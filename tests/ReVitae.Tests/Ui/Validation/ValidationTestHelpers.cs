using ReVitae.Core.Cv;
using ReVitae.Core.Cv.AdditionalInformation;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Core.Validation.Presentation;

namespace ReVitae.Tests.Ui.Validation;

internal static class ValidationTestHelpers
{
    public static AppLocalizer EnglishLocalizer { get; } = new(AppLocalizer.FallbackLanguageCode);

    public static string Localize(string messageKey) => EnglishLocalizer.Get(messageKey);

    public static FieldValidationResult ValidateFormLike(
        IReadOnlyDictionary<string, string?> personalValues,
        IReadOnlyList<WorkExperienceEntry> workEntries,
        IReadOnlyList<EducationEntry> educationEntries,
        IReadOnlyList<SkillsGroupEntry> skillsEntries,
        IReadOnlyList<LanguageEntry> languageEntries,
        IReadOnlyList<CertificateEntry> certificateEntries,
        IReadOnlyList<ProjectEntry> projectEntries,
        IReadOnlyList<LinkEntry> linkEntries,
        AdditionalInformationContent additionalInformation)
    {
        var personalResult = MainPersonalInformationSchema.CreateValidator().Validate(personalValues);
        var workResult = new WorkExperienceCollectionValidator().Validate(workEntries);
        var educationResult = new EducationCollectionValidator().Validate(educationEntries);
        var skillsResult = new SkillsCollectionValidator().Validate(skillsEntries);
        var languagesResult = new LanguagesCollectionValidator().Validate(languageEntries);
        var certificatesResult = new CertificatesCollectionValidator().Validate(certificateEntries);
        var projectsResult = new ProjectsCollectionValidator().Validate(projectEntries);
        var linksResult = new LinksCollectionValidator().Validate(linkEntries);
        var additionalResult = new AdditionalInformationValidator().Validate(additionalInformation);

        var combinedErrors = personalResult.Errors
            .Concat(workResult.Errors)
            .Concat(educationResult.Errors)
            .Concat(skillsResult.Errors)
            .Concat(languagesResult.Errors)
            .Concat(certificatesResult.Errors)
            .Concat(projectsResult.Errors)
            .Concat(linksResult.Errors)
            .Concat(additionalResult.Errors)
            .ToArray();

        return new FieldValidationResult(combinedErrors);
    }

    public static void AssertNoOrphanErrors(
        IReadOnlyList<FieldValidationError> errors,
        IReadOnlyCollection<string> registeredFieldKeys)
    {
        var orphans = ValidationOrphanChecker.FindOrphanErrors(errors, registeredFieldKeys);
        Assert.Empty(orphans);
    }

    public static void AssertEveryErrorKeyIsRegisteredAndMapped(
        IReadOnlyList<FieldValidationError> errors,
        IReadOnlyCollection<string> registeredFieldKeys)
    {
        AssertNoOrphanErrors(errors, registeredFieldKeys);

        var messageMap = ValidationFieldPresenter.BuildMessageMap(errors, Localize);
        foreach (var error in errors)
        {
            Assert.True(messageMap.ContainsKey(error.FieldKey), $"Missing message map entry for '{error.FieldKey}'.");
            Assert.NotEmpty(messageMap[error.FieldKey]);
            Assert.All(messageMap[error.FieldKey], message => Assert.False(string.IsNullOrWhiteSpace(message)));
        }
    }

    public static HashSet<string> BuildPersonalInfoRegistryKeys() =>
        MainPersonalInformationSchema.Fields.Select(field => field.Key).ToHashSet(StringComparer.Ordinal);

    public static HashSet<string> BuildWorkExperienceRegistryKeys(WorkExperienceEntry entry)
    {
        var keys = WorkExperienceSchema.CreateSchemasForEntry(entry.Id)
            .Select(schema => schema.Key)
            .ToHashSet(StringComparer.Ordinal);
        keys.Add(WorkExperienceFieldKeys.Build(entry.Id, WorkExperienceFieldKeys.DateRange));
        return keys;
    }

    public static HashSet<string> BuildEducationRegistryKeys(EducationEntry entry)
    {
        var keys = EducationSchema.CreateSchemasForEntry(entry.Id)
            .Select(schema => schema.Key)
            .ToHashSet(StringComparer.Ordinal);
        keys.Add(EducationFieldKeys.Build(entry.Id, EducationFieldKeys.DateRange));
        return keys;
    }

    public static HashSet<string> BuildSkillsRegistryKeys(SkillsGroupEntry group)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal)
        {
            SkillsFieldKeys.BuildGroup(group.Id, SkillsFieldKeys.Category),
            SkillsFieldKeys.BuildGroup(group.Id, SkillsFieldKeys.SkillsCollection)
        };

        foreach (var skill in group.Skills)
        {
            keys.Add(SkillsFieldKeys.BuildSkill(group.Id, skill.Id, SkillsFieldKeys.SkillName));
            keys.Add(SkillsFieldKeys.BuildSkill(group.Id, skill.Id, SkillsFieldKeys.SkillProficiency));
            keys.Add(SkillsFieldKeys.BuildSkill(group.Id, skill.Id, SkillsFieldKeys.SkillYearsOfExperience));
        }

        return keys;
    }

    public static HashSet<string> BuildLanguagesRegistryKeys(LanguageEntry entry) =>
        LanguagesSchema.CreateSchemasForEntry(entry.Id)
            .Select(schema => schema.Key)
            .ToHashSet(StringComparer.Ordinal);

    public static HashSet<string> BuildCertificatesRegistryKeys(CertificateEntry entry)
    {
        var keys = CertificatesSchema.CreateSchemasForEntry(entry.Id)
            .Select(schema => schema.Key)
            .ToHashSet(StringComparer.Ordinal);
        keys.Add(CertificatesFieldKeys.Build(entry.Id, CertificatesFieldKeys.DateRange));
        return keys;
    }

    public static HashSet<string> BuildProjectsRegistryKeys(ProjectEntry entry)
    {
        var keys = ProjectsSchema.CreateSchemasForEntry(entry.Id)
            .Select(schema => schema.Key)
            .ToHashSet(StringComparer.Ordinal);
        keys.Add(ProjectsFieldKeys.Build(entry.Id, ProjectsFieldKeys.DateRange));
        keys.Add(ProjectsFieldKeys.Build(entry.Id, ProjectsFieldKeys.BulkTechnologies));
        keys.Add(ProjectsFieldKeys.Build(entry.Id, ProjectsFieldKeys.TechnologiesCollection));

        foreach (var technology in entry.Technologies)
        {
            keys.Add(ProjectsFieldKeys.BuildTechnology(
                entry.Id,
                technology.Id,
                ProjectsFieldKeys.TechnologyName));
        }

        return keys;
    }

    public static HashSet<string> BuildLinksRegistryKeys(LinkEntry entry) =>
        LinksSchema.CreateSchemasForEntry(entry.Id)
            .Select(schema => schema.Key)
            .ToHashSet(StringComparer.Ordinal);

    public static HashSet<string> BuildAdditionalInformationRegistryKeys() =>
        new([AdditionalInformationFieldKeys.Content], StringComparer.Ordinal);

    public static HashSet<string> BuildCombinedRegistryKeys(
        IReadOnlyList<WorkExperienceEntry> workEntries,
        IReadOnlyList<EducationEntry> educationEntries,
        IReadOnlyList<SkillsGroupEntry> skillsEntries,
        IReadOnlyList<LanguageEntry> languageEntries,
        IReadOnlyList<CertificateEntry> certificateEntries,
        IReadOnlyList<ProjectEntry> projectEntries,
        IReadOnlyList<LinkEntry> linkEntries)
    {
        var keys = BuildPersonalInfoRegistryKeys();

        foreach (var entry in workEntries)
        {
            keys.UnionWith(BuildWorkExperienceRegistryKeys(entry));
        }

        foreach (var entry in educationEntries)
        {
            keys.UnionWith(BuildEducationRegistryKeys(entry));
        }

        foreach (var group in skillsEntries)
        {
            keys.UnionWith(BuildSkillsRegistryKeys(group));
        }

        foreach (var entry in languageEntries)
        {
            keys.UnionWith(BuildLanguagesRegistryKeys(entry));
        }

        foreach (var entry in certificateEntries)
        {
            keys.UnionWith(BuildCertificatesRegistryKeys(entry));
        }

        foreach (var entry in projectEntries)
        {
            keys.UnionWith(BuildProjectsRegistryKeys(entry));
        }

        foreach (var entry in linkEntries)
        {
            keys.UnionWith(BuildLinksRegistryKeys(entry));
        }

        keys.UnionWith(BuildAdditionalInformationRegistryKeys());
        return keys;
    }

    public static IReadOnlyDictionary<string, string?> BuildInvalidPersonalValues() =>
        new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [MainPersonalInformationFieldKeys.FirstName] = string.Empty,
            [MainPersonalInformationFieldKeys.LastName] = string.Empty,
            [MainPersonalInformationFieldKeys.ProfessionalTitle] = new string('a', 121),
            [MainPersonalInformationFieldKeys.Email] = "not-an-email",
            [MainPersonalInformationFieldKeys.Phone] = string.Empty,
            [MainPersonalInformationFieldKeys.Location] = string.Empty,
            [MainPersonalInformationFieldKeys.LinkedInUrl] = "bad-url",
            [MainPersonalInformationFieldKeys.PortfolioUrl] = string.Empty,
            [MainPersonalInformationFieldKeys.GitHubUrl] = string.Empty,
            [MainPersonalInformationFieldKeys.ShortSummary] = new string('a', 801)
        };
}
