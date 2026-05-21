using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;

namespace ReVitae.Core.Import.Structured;

/// <summary>Shared helpers for structured imports (<see cref="CvImportConfidence"/>, section routing).</summary>
public static class CvStructuredImportMapper
{
    public static ImportedFieldConfidence Field(string fieldPath, CvImportConfidence confidence)
    {
        return new ImportedFieldConfidence(fieldPath, confidence);
    }

    public static CvImportConfidence ConfidenceForLikelyGuess(bool directMatch, bool inferred)
    {
        return directMatch ? CvImportConfidence.High :
            inferred ? CvImportConfidence.Medium : CvImportConfidence.Low;
    }

    public static IReadOnlyDictionary<CvImportSectionId, bool> SectionHasData(
        PersonalInformationImport personal,
        IReadOnlyList<WorkExperienceEntry> workExperience,
        IReadOnlyList<EducationEntry> education,
        IReadOnlyList<SkillsGroupEntry> skills,
        IReadOnlyList<LanguageEntry> languages,
        IReadOnlyList<CertificateEntry> certificates,
        IReadOnlyList<ProjectEntry> projects,
        IReadOnlyList<LinkEntry> links,
        string additionalInformation)
    {
        return new Dictionary<CvImportSectionId, bool>
        {
            [CvImportSectionId.PersonalInformation] = personal.HasAnyData(),
            [CvImportSectionId.WorkExperience] = workExperience.Count > 0,
            [CvImportSectionId.Education] = education.Count > 0,
            [CvImportSectionId.Skills] = skills.Count > 0,
            [CvImportSectionId.Languages] = languages.Count > 0,
            [CvImportSectionId.Certificates] = certificates.Count > 0,
            [CvImportSectionId.Projects] = projects.Count > 0,
            [CvImportSectionId.Links] = links.Count > 0,
            [CvImportSectionId.AdditionalInformation] = !string.IsNullOrWhiteSpace(additionalInformation)
        };
    }

    public static CvImportWarning Warning(string translationKey)
    {
        return new CvImportWarning(translationKey);
    }

    public static void AddUniqueHref(ICollection<string> store, HashSet<string> seenInsensitive, string? href)
    {
        var trimmed = href?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (seenInsensitive.Add(trimmed))
        {
            store.Add(trimmed);
        }
    }

    public static bool HasImportableCvData(
        PersonalInformationImport personal,
        IReadOnlyList<WorkExperienceEntry> workExperience,
        IReadOnlyList<EducationEntry> education,
        IReadOnlyList<SkillsGroupEntry> skills,
        IReadOnlyList<LanguageEntry> languages,
        IReadOnlyList<CertificateEntry> certificates,
        IReadOnlyList<ProjectEntry> projects,
        IReadOnlyList<LinkEntry> links,
        string additionalInformation)
    {
        return personal.HasAnyData()
            || workExperience.Count > 0
            || education.Count > 0
            || skills.Count > 0
            || languages.Count > 0
            || certificates.Count > 0
            || projects.Count > 0
            || links.Count > 0
            || !string.IsNullOrWhiteSpace(additionalInformation);
    }
}
