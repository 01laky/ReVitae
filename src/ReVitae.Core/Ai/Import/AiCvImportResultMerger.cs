using System.Text.Json.Nodes;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import;
using ReVitae.Core.Import.Structured;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Import;

public static class AiCvImportResultMerger
{
    public static JsonObject CreateEmptyDocument() => new() { ["revitaeVersion"] = 1 };

    public static void MergeFragment(JsonObject accumulated, JsonObject fragment)
    {
        foreach (var property in fragment)
        {
            if (property.Value is null)
            {
                continue;
            }

            if (property.Key is "personalInformation" && property.Value is JsonObject personal)
            {
                MergePersonal(accumulated, personal);
                continue;
            }

            if (property.Value is JsonArray incomingArray)
            {
                MergeArray(accumulated, property.Key, incomingArray);
                continue;
            }

            if (property.Key is "additionalInformation" && property.Value is JsonObject additional)
            {
                accumulated["additionalInformation"] = additional.DeepClone();
                continue;
            }

            accumulated[property.Key] = property.Value.DeepClone();
        }
    }

    public static CvImportResult BuildFinalResult(
        JsonObject accumulated,
        CvImportResult? deterministicBaseline,
        AiCvImportMergeMode mergeMode,
        IReadOnlyList<CvImportWarning> acquisitionWarnings,
        int batchesFailed,
        string? existingProfilePhotoPath)
    {
        var warnings = new List<CvImportWarning>(acquisitionWarnings)
        {
            new(TranslationKeys.ImportWarningAiAssisted),
        };

        if (batchesFailed > 0)
        {
            warnings.Add(new CvImportWarning(TranslationKeys.ImportWarningAiPartial));
        }

        var aiResult = AiCvImportResponseParser.MapAccumulated(accumulated, warnings);
        return MergeForApply(aiResult, deterministicBaseline, mergeMode, existingProfilePhotoPath);
    }

    public static CvImportResult MergeForApply(
        CvImportResult aiFull,
        CvImportResult? deterministicBaseline,
        AiCvImportMergeMode mergeMode,
        string? existingProfilePhotoPath)
    {
        if (mergeMode == AiCvImportMergeMode.ReplaceAll ||
            deterministicBaseline is null ||
            !deterministicBaseline.Success)
        {
            return PreservePhotoPath(aiFull, existingProfilePhotoPath);
        }

        return PreservePhotoPath(
            MergeFillEmptyOnly(deterministicBaseline, aiFull),
            existingProfilePhotoPath);
    }

    private static CvImportResult PreservePhotoPath(CvImportResult result, string? existingProfilePhotoPath)
    {
        if (string.IsNullOrWhiteSpace(existingProfilePhotoPath))
        {
            return result;
        }

        var personal = result.Personal;
        personal.ProfilePhotoPath = existingProfilePhotoPath;
        return new CvImportResult
        {
            Success = result.Success,
            ErrorMessageKey = result.ErrorMessageKey,
            Personal = personal,
            WorkExperienceEntries = result.WorkExperienceEntries,
            EducationEntries = result.EducationEntries,
            SkillsGroups = result.SkillsGroups,
            LanguageEntries = result.LanguageEntries,
            CertificateEntries = result.CertificateEntries,
            ProjectEntries = result.ProjectEntries,
            LinkEntries = result.LinkEntries,
            AdditionalInformationContent = result.AdditionalInformationContent,
            SectionHasData = result.SectionHasData,
            Warnings = result.Warnings,
            FieldConfidences = result.FieldConfidences,
        };
    }

    private static CvImportResult MergeFillEmptyOnly(CvImportResult baseline, CvImportResult ai)
    {
        var personal = baseline.Personal;
        if (string.IsNullOrWhiteSpace(personal.FirstName) && !string.IsNullOrWhiteSpace(ai.Personal.FirstName))
        {
            personal.FirstName = ai.Personal.FirstName;
        }

        if (string.IsNullOrWhiteSpace(personal.LastName) && !string.IsNullOrWhiteSpace(ai.Personal.LastName))
        {
            personal.LastName = ai.Personal.LastName;
        }

        if (string.IsNullOrWhiteSpace(personal.Email) && !string.IsNullOrWhiteSpace(ai.Personal.Email))
        {
            personal.Email = ai.Personal.Email;
        }

        if (string.IsNullOrWhiteSpace(personal.Phone) && !string.IsNullOrWhiteSpace(ai.Personal.Phone))
        {
            personal.Phone = ai.Personal.Phone;
        }

        if (string.IsNullOrWhiteSpace(personal.ProfessionalTitle) && !string.IsNullOrWhiteSpace(ai.Personal.ProfessionalTitle))
        {
            personal.ProfessionalTitle = ai.Personal.ProfessionalTitle;
        }

        if (string.IsNullOrWhiteSpace(personal.ShortSummary) && !string.IsNullOrWhiteSpace(ai.Personal.ShortSummary))
        {
            personal.ShortSummary = ai.Personal.ShortSummary;
        }

        if (string.IsNullOrWhiteSpace(personal.Location) && !string.IsNullOrWhiteSpace(ai.Personal.Location))
        {
            personal.Location = ai.Personal.Location;
        }

        var work = MergeWork(baseline.WorkExperienceEntries, ai.WorkExperienceEntries);
        var education = MergeEducation(baseline.EducationEntries, ai.EducationEntries);
        var skills = baseline.SkillsGroups.Count > 0 ? baseline.SkillsGroups : ai.SkillsGroups;
        var languages = baseline.LanguageEntries.Count > 0 ? baseline.LanguageEntries : ai.LanguageEntries;
        var certificates = MergeCertificates(baseline.CertificateEntries, ai.CertificateEntries);
        var projects = MergeProjects(baseline.ProjectEntries, ai.ProjectEntries);
        var links = baseline.LinkEntries.Count > 0 ? baseline.LinkEntries : ai.LinkEntries;
        var additional = string.IsNullOrWhiteSpace(baseline.AdditionalInformationContent)
            ? ai.AdditionalInformationContent
            : baseline.AdditionalInformationContent;

        var warnings = baseline.Warnings.Concat(ai.Warnings).DistinctBy(w => w.MessageKey).ToList();
        return new CvImportResult
        {
            Success = true,
            Personal = personal,
            WorkExperienceEntries = work,
            EducationEntries = education,
            SkillsGroups = skills,
            LanguageEntries = languages,
            CertificateEntries = certificates,
            ProjectEntries = projects,
            LinkEntries = links,
            AdditionalInformationContent = additional,
            SectionHasData = CvStructuredImportMapper.SectionHasData(
                personal,
                work,
                education,
                skills,
                languages,
                certificates,
                projects,
                links,
                additional),
            Warnings = warnings,
            FieldConfidences = ai.FieldConfidences,
        };
    }

    private static void MergePersonal(JsonObject accumulated, JsonObject personal)
    {
        if (accumulated["personalInformation"] is not JsonObject existing)
        {
            accumulated["personalInformation"] = personal.DeepClone();
            return;
        }

        foreach (var property in personal)
        {
            if (property.Value is null)
            {
                continue;
            }

            existing[property.Key] = property.Value.DeepClone();
        }
    }

    private static void MergeArray(JsonObject accumulated, string key, JsonArray incoming)
    {
        if (accumulated[key] is not JsonArray existing)
        {
            accumulated[key] = incoming.DeepClone();
            return;
        }

        foreach (var node in incoming)
        {
            if (node is not null)
            {
                existing.Add(node.DeepClone());
            }
        }
    }

    private static IReadOnlyList<WorkExperienceEntry> MergeWork(
        IReadOnlyList<WorkExperienceEntry> baseline,
        IReadOnlyList<WorkExperienceEntry> ai)
    {
        var result = baseline.ToList();
        foreach (var entry in ai)
        {
            if (result.Any(existing =>
                    string.Equals(existing.Company, entry.Company, StringComparison.OrdinalIgnoreCase) &&
                    existing.StartYear == entry.StartYear &&
                    existing.StartMonth == entry.StartMonth))
            {
                continue;
            }

            result.Add(entry);
        }

        return result;
    }

    private static IReadOnlyList<EducationEntry> MergeEducation(
        IReadOnlyList<EducationEntry> baseline,
        IReadOnlyList<EducationEntry> ai)
    {
        var result = baseline.ToList();
        foreach (var entry in ai)
        {
            if (result.Any(existing =>
                    string.Equals(existing.Institution, entry.Institution, StringComparison.OrdinalIgnoreCase) &&
                    existing.StartYear == entry.StartYear))
            {
                continue;
            }

            result.Add(entry);
        }

        return result;
    }

    private static IReadOnlyList<CertificateEntry> MergeCertificates(
        IReadOnlyList<CertificateEntry> baseline,
        IReadOnlyList<CertificateEntry> ai)
    {
        var result = baseline.ToList();
        foreach (var entry in ai)
        {
            if (result.Any(existing => string.Equals(existing.Name, entry.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            result.Add(entry);
        }

        return result;
    }

    private static IReadOnlyList<ProjectEntry> MergeProjects(
        IReadOnlyList<ProjectEntry> baseline,
        IReadOnlyList<ProjectEntry> ai)
    {
        var result = baseline.ToList();
        foreach (var entry in ai)
        {
            if (result.Any(existing => string.Equals(existing.Name, entry.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            result.Add(entry);
        }

        return result;
    }
}
