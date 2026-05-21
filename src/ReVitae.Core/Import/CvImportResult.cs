using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;

namespace ReVitae.Core.Import;

public sealed class CvImportResult
{
    public static CvImportResult Failed(string errorMessageKey)
    {
        return new CvImportResult
        {
            Success = false,
            ErrorMessageKey = errorMessageKey
        };
    }

    public bool Success { get; init; }

    public string? ErrorMessageKey { get; init; }

    public PersonalInformationImport Personal { get; init; } = new();

    public IReadOnlyList<WorkExperienceEntry> WorkExperienceEntries { get; init; } = [];

    public IReadOnlyList<EducationEntry> EducationEntries { get; init; } = [];

    public IReadOnlyList<SkillsGroupEntry> SkillsGroups { get; init; } = [];

    public IReadOnlyList<LanguageEntry> LanguageEntries { get; init; } = [];

    public IReadOnlyList<CertificateEntry> CertificateEntries { get; init; } = [];

    public IReadOnlyList<ProjectEntry> ProjectEntries { get; init; } = [];

    public IReadOnlyList<LinkEntry> LinkEntries { get; init; } = [];

    public string AdditionalInformationContent { get; init; } = string.Empty;

    public IReadOnlyDictionary<CvImportSectionId, bool> SectionHasData { get; init; }
        = new Dictionary<CvImportSectionId, bool>();

    public IReadOnlyList<CvImportWarning> Warnings { get; init; } = [];

    public IReadOnlyList<ImportedFieldConfidence> FieldConfidences { get; init; } = [];
}
