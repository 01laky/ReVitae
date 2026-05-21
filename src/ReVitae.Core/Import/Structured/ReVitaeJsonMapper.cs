using System.Linq;
using System.Text.Json;
using ReVitae.Core.Cv;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Structured;

public static class ReVitaeJsonMapper
{
    private const int MinSupportedRevision = 1;
    private const int MaxSupportedRevision = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static CvImportResult Map(string jsonText)
    {
        RevitaeFile? dto;
        try
        {
            dto = JsonSerializer.Deserialize<RevitaeFile>(jsonText, JsonOptions);
        }
        catch (JsonException)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);
        }

        if (dto is null || dto.RevitaeVersion < MinSupportedRevision || dto.RevitaeVersion > MaxSupportedRevision)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnsupportedStructuredFormat);
        }

        var skillGroups = new List<SkillsGroupEntry>();
        if (dto.Skills != null)
        {
            foreach (var snapshot in dto.Skills)
            {
                var group = new SkillsGroupEntry();
                group.Category = string.IsNullOrWhiteSpace(snapshot.Category) ? "General" : snapshot.Category!;
                if (snapshot.Skills != null)
                {
                    foreach (var item in snapshot.Skills.Where(item => item is { Name.Length: > 0 }))
                    {
                        group.Skills.Add(new SkillItem { Name = item.Name! });
                    }
                }

                if (group.HasUserInput())
                {
                    skillGroups.Add(group);
                }
            }
        }

        var links = dto.Links ?? [];
        var projects = dto.Projects ?? [];
        var certs = dto.Certificates ?? [];
        var languages = dto.Languages ?? [];
        var education = dto.Education ?? [];
        var experience = dto.WorkExperience ?? [];
        var personal = MapPersonalInformation(dto.PersonalInformation);

        string additionalInformation = string.Empty;
        if (!string.IsNullOrWhiteSpace(dto.AdditionalInformation?.Content))
        {
            additionalInformation = CvTextNormalizer.Normalize(dto.AdditionalInformation.Content).Trim();
        }

        if (!CvStructuredImportMapper.HasImportableCvData(
                personal,
                experience,
                education,
                skillGroups,
                languages,
                certs,
                projects,
                links,
                additionalInformation))
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorNoStructuredData);
        }

        var confidences = new List<ImportedFieldConfidence>();
        AppendPersonalConfidences(personal, confidences);

        return new CvImportResult
        {
            Success = true,
            Personal = personal,
            WorkExperienceEntries = experience,
            EducationEntries = education,
            SkillsGroups = skillGroups,
            LanguageEntries = languages,
            CertificateEntries = certs,
            ProjectEntries = projects,
            LinkEntries = links,
            AdditionalInformationContent = additionalInformation,
            SectionHasData = CvStructuredImportMapper.SectionHasData(personal, experience, education, skillGroups, languages, certs, projects, links,
                additionalInformation),
            Warnings = [],
            FieldConfidences = confidences
        };
    }

    internal static PersonalInformationImport MapPersonalInformation(PersonalInformationFileDto? dto)
    {
        if (dto is null)
        {
            return new PersonalInformationImport();
        }

        var personal = new PersonalInformationImport
        {
            FirstName = dto.FirstName ?? string.Empty,
            LastName = dto.LastName ?? string.Empty,
            ProfessionalTitle = dto.ProfessionalTitle ?? string.Empty,
            Email = dto.Email ?? string.Empty,
            Phone = dto.Phone ?? string.Empty,
            Location = dto.Location ?? string.Empty,
            LinkedInUrl = dto.LinkedInUrl ?? string.Empty,
            PortfolioUrl = dto.PortfolioUrl ?? string.Empty,
            GitHubUrl = dto.GitHubUrl ?? string.Empty,
            ShortSummary = dto.ShortSummary ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(dto.ProfilePhotoBase64))
        {
            try
            {
                var bytes = Convert.FromBase64String(dto.ProfilePhotoBase64);
                var contentType = string.IsNullOrWhiteSpace(dto.ProfilePhotoContentType)
                    ? "image/jpeg"
                    : dto.ProfilePhotoContentType;
                var storage = new ProfilePhotoStorage();
                var saveResult = storage.TrySaveBytes(bytes, contentType);
                if (saveResult.Success && !string.IsNullOrWhiteSpace(saveResult.StoredPath))
                {
                    personal.ProfilePhotoPath = saveResult.StoredPath!;
                }
            }
            catch (FormatException)
            {
                // Invalid base64 — skip photo, keep text import.
            }
        }

        return personal;
    }

    private static void AppendPersonalConfidences(PersonalInformationImport personal, ICollection<ImportedFieldConfidence> sink)
    {
        void Tag(string path, string? raw)
        {
            if (!string.IsNullOrWhiteSpace(raw))
            {
                sink.Add(CvStructuredImportMapper.Field(path, CvImportConfidence.High));
            }
        }

        Tag(MainPersonalInformationFieldKeys.FirstName, personal.FirstName);
        Tag(MainPersonalInformationFieldKeys.LastName, personal.LastName);
        Tag(MainPersonalInformationFieldKeys.ProfessionalTitle, personal.ProfessionalTitle);
        Tag(MainPersonalInformationFieldKeys.Email, personal.Email);
        Tag(MainPersonalInformationFieldKeys.Phone, personal.Phone);
        Tag(MainPersonalInformationFieldKeys.Location, personal.Location);
        Tag(MainPersonalInformationFieldKeys.LinkedInUrl, personal.LinkedInUrl);
        Tag(MainPersonalInformationFieldKeys.PortfolioUrl, personal.PortfolioUrl);
        Tag(MainPersonalInformationFieldKeys.GitHubUrl, personal.GitHubUrl);
        Tag(MainPersonalInformationFieldKeys.ShortSummary, personal.ShortSummary);
        if (ProfilePhotoStorage.FileExists(personal.ProfilePhotoPath))
        {
            Tag(MainPersonalInformationFieldKeys.ProfilePhotoPath, personal.ProfilePhotoPath);
        }
    }

    private sealed class RevitaeFile
    {
        public int RevitaeVersion { get; init; }

        public PersonalInformationFileDto? PersonalInformation { get; init; }

        public List<WorkExperienceEntry>? WorkExperience { get; init; }

        public List<EducationEntry>? Education { get; init; }

        public List<SkillsGroupDraft>? Skills { get; init; }

        public List<LanguageEntry>? Languages { get; init; }

        public List<CertificateEntry>? Certificates { get; init; }

        public List<ProjectEntry>? Projects { get; init; }

        public List<LinkEntry>? Links { get; init; }

        public AdditionalInformationDraft? AdditionalInformation { get; init; }
    }

    internal sealed class PersonalInformationFileDto
    {
        public string? FirstName { get; init; }

        public string? LastName { get; init; }

        public string? ProfessionalTitle { get; init; }

        public string? Email { get; init; }

        public string? Phone { get; init; }

        public string? Location { get; init; }

        public string? LinkedInUrl { get; init; }

        public string? PortfolioUrl { get; init; }

        public string? GitHubUrl { get; init; }

        public string? ShortSummary { get; init; }

        public string? ProfilePhotoBase64 { get; init; }

        public string? ProfilePhotoContentType { get; init; }
    }

    private sealed class SkillsGroupDraft
    {
        public string? Category { get; init; }

        public List<SkillItemDraft>? Skills { get; init; }
    }

    private sealed class SkillItemDraft
    {
        public string? Name { get; init; }
    }

    private sealed class AdditionalInformationDraft
    {
        public string? Content { get; init; }
    }
}
