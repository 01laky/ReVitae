using ReVitae.Core.Cv;
using ReVitae.Core.Cv.AdditionalInformation;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Quality;

public static class CvQualityAnalyzer
{
    public static CvQualityReport Analyze(
        CvExportSourceData snapshot,
        CvQualityAnalysisOptions? options = null)
    {
        options ??= CvQualityAnalysisOptions.Default;
        var hints = new List<CvQualityHint>();

        AnalyzePersonal(snapshot, hints);
        AnalyzeWorkExperience(snapshot, hints);
        AnalyzeEducation(snapshot, hints);
        AnalyzeSkills(snapshot, hints);
        AnalyzeLanguages(snapshot, hints);
        AnalyzeLinks(snapshot, hints);
        AnalyzeCertificates(snapshot, hints);
        AnalyzeProjects(snapshot, hints);
        AnalyzeImportConfidence(snapshot, hints, options.ImportConfidences);

        var filtered = FilterDismissed(Deduplicate(hints), options.DismissedHintKeys);
        return new CvQualityReport(filtered);
    }

    public static string BuildDismissKey(CvQualityHint hint) =>
        $"{hint.Id}|{hint.EntryId ?? string.Empty}|{hint.FieldKey ?? string.Empty}";

    private static void AnalyzePersonal(CvExportSourceData snapshot, ICollection<CvQualityHint> hints)
    {
        var summaryLength = CvQualityTextHelper.CountNonWhitespace(snapshot.Personal.ShortSummary);
        if (summaryLength is > 0 and < 80)
        {
            hints.Add(new CvQualityHint(
                CvQualityHintIds.PersonalSummaryTooShort,
                TranslationKeys.QualityHintPersonalSummaryTooShort,
                CvQualityHintSeverity.Suggestion,
                CvImportSectionId.PersonalInformation,
                MainPersonalInformationFieldKeys.ShortSummary));
        }

        if (summaryLength > 600)
        {
            hints.Add(new CvQualityHint(
                CvQualityHintIds.PersonalSummaryTooLong,
                TranslationKeys.QualityHintPersonalSummaryTooLong,
                CvQualityHintSeverity.Info,
                CvImportSectionId.PersonalInformation,
                MainPersonalInformationFieldKeys.ShortSummary));
        }

        if (CvQualityGate.HasStartedCv(snapshot)
            && summaryLength == 0
            && (snapshot.WorkExperience.Count > 0 || snapshot.Education.Count > 0))
        {
            hints.Add(new CvQualityHint(
                CvQualityHintIds.PersonalSummaryMissing,
                TranslationKeys.QualityHintPersonalSummaryMissing,
                CvQualityHintSeverity.Suggestion,
                CvImportSectionId.PersonalInformation,
                MainPersonalInformationFieldKeys.ShortSummary));
        }

        if (CvQualityGate.HasStartedCv(snapshot)
            && !CvQualityTextHelper.HasText(snapshot.Personal.ProfessionalTitle))
        {
            hints.Add(new CvQualityHint(
                CvQualityHintIds.PersonalMissingTitle,
                TranslationKeys.QualityHintPersonalMissingTitle,
                CvQualityHintSeverity.Suggestion,
                CvImportSectionId.PersonalInformation,
                MainPersonalInformationFieldKeys.ProfessionalTitle));
        }
    }

    private static void AnalyzeWorkExperience(CvExportSourceData snapshot, ICollection<CvQualityHint> hints)
    {
        if (CvQualityGate.HasStartedCv(snapshot)
            && snapshot.WorkExperience.Count == 0
            && CvQualityGate.HasOtherSectionData(snapshot, CvImportSectionId.WorkExperience))
        {
            hints.Add(new CvQualityHint(
                CvQualityHintIds.WorkSectionEmpty,
                TranslationKeys.QualityHintWorkSectionEmpty,
                CvQualityHintSeverity.Suggestion,
                CvImportSectionId.WorkExperience));
        }

        foreach (var entry in snapshot.WorkExperience)
        {
            var descriptionKey = WorkExperienceFieldKeys.Build(entry.Id, WorkExperienceFieldKeys.Description);
            if (!CvQualityTextHelper.HasText(entry.Description))
            {
                hints.Add(new CvQualityHint(
                    CvQualityHintIds.WorkEntryMissingDescription,
                    TranslationKeys.QualityHintWorkMissingDescription,
                    CvQualityHintSeverity.Suggestion,
                    CvImportSectionId.WorkExperience,
                    descriptionKey,
                    entry.Id));
            }
            else if (GenericWorkDescriptionHeuristic.IsGeneric(entry.Description))
            {
                hints.Add(new CvQualityHint(
                    CvQualityHintIds.WorkGenericDescription,
                    TranslationKeys.QualityHintWorkGenericDescription,
                    CvQualityHintSeverity.Suggestion,
                    CvImportSectionId.WorkExperience,
                    descriptionKey,
                    entry.Id));
            }
        }
    }

    private static void AnalyzeEducation(CvExportSourceData snapshot, ICollection<CvQualityHint> hints)
    {
        if (CvQualityGate.HasStartedCv(snapshot)
            && snapshot.Education.Count == 0
            && CvQualityGate.HasOtherSectionData(snapshot, CvImportSectionId.Education))
        {
            hints.Add(new CvQualityHint(
                CvQualityHintIds.EducationSectionEmpty,
                TranslationKeys.QualityHintEducationSectionEmpty,
                CvQualityHintSeverity.Suggestion,
                CvImportSectionId.Education));
        }
    }

    private static void AnalyzeSkills(CvExportSourceData snapshot, ICollection<CvQualityHint> hints)
    {
        if (CvQualityGate.HasStartedCv(snapshot) && snapshot.Skills.Count == 0)
        {
            hints.Add(new CvQualityHint(
                CvQualityHintIds.SkillsSectionEmpty,
                TranslationKeys.QualityHintSkillsSectionEmpty,
                CvQualityHintSeverity.Suggestion,
                CvImportSectionId.Skills));
        }

        foreach (var group in snapshot.Skills)
        {
            var activeSkillCount = group.Skills.Count(skill => skill.HasUserInput());
            if (activeSkillCount > 15)
            {
                hints.Add(new CvQualityHint(
                    CvQualityHintIds.SkillsSingleLargeGroup,
                    TranslationKeys.QualityHintSkillsSingleLargeGroup,
                    CvQualityHintSeverity.Info,
                    CvImportSectionId.Skills,
                    EntryId: group.Id));
            }
        }
    }

    private static void AnalyzeLanguages(CvExportSourceData snapshot, ICollection<CvQualityHint> hints)
    {
        if (snapshot.WorkExperience.Count > 0 && snapshot.Languages.Count == 0)
        {
            hints.Add(new CvQualityHint(
                CvQualityHintIds.LanguagesSectionEmpty,
                TranslationKeys.QualityHintLanguagesSectionEmpty,
                CvQualityHintSeverity.Suggestion,
                CvImportSectionId.Languages));
        }
    }

    private static void AnalyzeLinks(CvExportSourceData snapshot, ICollection<CvQualityHint> hints)
    {
        var personalUrls = new[]
        {
            snapshot.Personal.LinkedInUrl,
            snapshot.Personal.GitHubUrl,
            snapshot.Personal.PortfolioUrl
        };

        foreach (var link in snapshot.Links)
        {
            if (!CvQualityTextHelper.HasText(link.Url))
            {
                continue;
            }

            foreach (var personalUrl in personalUrls)
            {
                if (CvUrlNormalizer.AreEquivalent(link.Url, personalUrl))
                {
                    hints.Add(new CvQualityHint(
                        CvQualityHintIds.LinksDuplicatePersonalUrl,
                        TranslationKeys.QualityHintLinksDuplicatePersonalUrl,
                        CvQualityHintSeverity.Info,
                        CvImportSectionId.Links,
                        LinksFieldKeys.Build(link.Id, LinksFieldKeys.Url),
                        link.Id));
                    break;
                }
            }
        }
    }

    private static void AnalyzeCertificates(CvExportSourceData snapshot, ICollection<CvQualityHint> hints)
    {
        if (CvQualityGate.HasStartedCv(snapshot)
            && snapshot.Certificates.Count == 0
            && CvQualityGate.HasOtherSectionData(snapshot, CvImportSectionId.Certificates))
        {
            hints.Add(new CvQualityHint(
                CvQualityHintIds.CertificatesSectionEmpty,
                TranslationKeys.QualityHintCertificatesSectionEmpty,
                CvQualityHintSeverity.Suggestion,
                CvImportSectionId.Certificates));
        }
    }

    private static void AnalyzeProjects(CvExportSourceData snapshot, ICollection<CvQualityHint> hints)
    {
        if (CvQualityGate.HasStartedCv(snapshot)
            && snapshot.Projects.Count == 0
            && CvQualityGate.HasOtherSectionData(snapshot, CvImportSectionId.Projects))
        {
            hints.Add(new CvQualityHint(
                CvQualityHintIds.ProjectsSectionEmpty,
                TranslationKeys.QualityHintProjectsSectionEmpty,
                CvQualityHintSeverity.Suggestion,
                CvImportSectionId.Projects));
        }

        foreach (var entry in snapshot.Projects)
        {
            if (!CvQualityTextHelper.HasText(entry.Description)
                && !CvQualityTextHelper.HasText(entry.Highlights))
            {
                hints.Add(new CvQualityHint(
                    CvQualityHintIds.ProjectsEntryMissingDescription,
                    TranslationKeys.QualityHintProjectsMissingDescription,
                    CvQualityHintSeverity.Suggestion,
                    CvImportSectionId.Projects,
                    ProjectsFieldKeys.Build(entry.Id, ProjectsFieldKeys.Description),
                    entry.Id));
            }
        }
    }

    private static void AnalyzeImportConfidence(
        CvExportSourceData snapshot,
        ICollection<CvQualityHint> hints,
        IReadOnlyList<ImportedFieldConfidence>? importConfidences)
    {
        if (importConfidences is null || importConfidences.Count == 0)
        {
            return;
        }

        var lowConfidenceBySection = importConfidences
            .Where(item => item.Confidence == CvImportConfidence.Low)
            .GroupBy(item => ResolveSectionForFieldKey(item.FieldKey))
            .Where(group => group.Key.HasValue)
            .ToDictionary(group => group.Key!.Value, group => group.ToArray());

        foreach (var (section, fields) in lowConfidenceBySection)
        {
            if (fields.Length >= 2)
            {
                hints.Add(new CvQualityHint(
                    CvQualityHintIds.ImportReviewSection,
                    TranslationKeys.QualityHintImportReviewSection,
                    CvQualityHintSeverity.Info,
                    section));
            }
        }

        var contentHintsByField = hints
            .Where(hint => !string.IsNullOrEmpty(hint.FieldKey))
            .Select(hint => hint.FieldKey!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var field in importConfidences.Where(item => item.Confidence == CvImportConfidence.Low))
        {
            if (!contentHintsByField.Contains(field.FieldKey))
            {
                continue;
            }

            var section = ResolveSectionForFieldKey(field.FieldKey);
            if (!section.HasValue)
            {
                continue;
            }

            hints.Add(new CvQualityHint(
                CvQualityHintIds.ImportReviewField,
                TranslationKeys.QualityHintImportReviewField,
                CvQualityHintSeverity.Info,
                section,
                field.FieldKey));
        }
    }

    private static CvImportSectionId? ResolveSectionForFieldKey(string fieldKey)
    {
        if (fieldKey.StartsWith(WorkExperienceFieldKeys.Prefix + ".", StringComparison.Ordinal))
        {
            return CvImportSectionId.WorkExperience;
        }

        if (fieldKey.StartsWith(EducationFieldKeys.Prefix + ".", StringComparison.Ordinal))
        {
            return CvImportSectionId.Education;
        }

        if (fieldKey.StartsWith(SkillsFieldKeys.Prefix + ".", StringComparison.Ordinal))
        {
            return CvImportSectionId.Skills;
        }

        if (fieldKey.StartsWith(LanguagesFieldKeys.Prefix + ".", StringComparison.Ordinal))
        {
            return CvImportSectionId.Languages;
        }

        if (fieldKey.StartsWith(CertificatesFieldKeys.Prefix + ".", StringComparison.Ordinal))
        {
            return CvImportSectionId.Certificates;
        }

        if (fieldKey.StartsWith(ProjectsFieldKeys.Prefix + ".", StringComparison.Ordinal))
        {
            return CvImportSectionId.Projects;
        }

        if (fieldKey.StartsWith(LinksFieldKeys.Prefix + ".", StringComparison.Ordinal))
        {
            return CvImportSectionId.Links;
        }

        if (string.Equals(fieldKey, AdditionalInformationFieldKeys.Content, StringComparison.Ordinal))
        {
            return CvImportSectionId.AdditionalInformation;
        }

        if (IsPersonalFieldKey(fieldKey))
        {
            return CvImportSectionId.PersonalInformation;
        }

        return null;
    }

    private static bool IsPersonalFieldKey(string fieldKey) =>
        fieldKey is MainPersonalInformationFieldKeys.FirstName
            or MainPersonalInformationFieldKeys.LastName
            or MainPersonalInformationFieldKeys.ProfessionalTitle
            or MainPersonalInformationFieldKeys.Email
            or MainPersonalInformationFieldKeys.Phone
            or MainPersonalInformationFieldKeys.Location
            or MainPersonalInformationFieldKeys.LinkedInUrl
            or MainPersonalInformationFieldKeys.PortfolioUrl
            or MainPersonalInformationFieldKeys.GitHubUrl
            or MainPersonalInformationFieldKeys.ShortSummary
            or MainPersonalInformationFieldKeys.ProfilePhotoPath;

    private static IReadOnlyList<CvQualityHint> Deduplicate(IReadOnlyList<CvQualityHint> hints)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<CvQualityHint>();

        foreach (var hint in hints)
        {
            var key = BuildDismissKey(hint);
            if (seen.Add(key))
            {
                result.Add(hint);
            }
        }

        return result;
    }

    private static IReadOnlyList<CvQualityHint> FilterDismissed(
        IReadOnlyList<CvQualityHint> hints,
        IReadOnlySet<string>? dismissedKeys)
    {
        if (dismissedKeys is null || dismissedKeys.Count == 0)
        {
            return hints;
        }

        return hints
            .Where(hint => !dismissedKeys.Contains(BuildDismissKey(hint)))
            .ToArray();
    }
}
