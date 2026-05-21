using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.Projects;

public static class ProjectsSchema
{
    public const int NameMaxLength = 160;
    public const int RoleMaxLength = 120;
    public const int OrganizationMaxLength = 160;
    public const int ProjectUrlMaxLength = 240;
    public const int HighlightsMaxLength = 2000;
    public const int DescriptionMaxLength = 2000;
    public const int TechnologyNameMaxLength = 80;
    public const int BulkTechnologiesMaxLength = 1000;

    public static readonly IReadOnlyList<FieldSchema> EntryFields = Array.AsReadOnly(
        new[]
        {
            RequiredText(
                ProjectsFieldKeys.Name,
                "Project name",
                NameMaxLength,
                TranslationKeys.ValidationProjectsNameRequired,
                TranslationKeys.ValidationProjectsNameMax),
            OptionalText(
                ProjectsFieldKeys.Role,
                "Role",
                RoleMaxLength,
                TranslationKeys.ValidationProjectsRoleMax),
            OptionalText(
                ProjectsFieldKeys.Organization,
                "Organization or context",
                OrganizationMaxLength,
                TranslationKeys.ValidationProjectsOrganizationMax),
            OptionalMonth(
                ProjectsFieldKeys.StartMonth,
                TranslationKeys.ValidationProjectsStartMonthInvalid),
            OptionalYear(
                ProjectsFieldKeys.StartYear,
                TranslationKeys.ValidationProjectsStartYearInvalid),
            OptionalMonth(
                ProjectsFieldKeys.EndMonth,
                TranslationKeys.ValidationProjectsEndMonthInvalid),
            OptionalYear(
                ProjectsFieldKeys.EndYear,
                TranslationKeys.ValidationProjectsEndYearInvalid),
            OptionalUrl(
                ProjectsFieldKeys.ProjectUrl,
                "Project URL",
                ProjectUrlMaxLength,
                TranslationKeys.ValidationProjectsProjectUrlMax,
                TranslationKeys.ValidationProjectsProjectUrlFormat),
            OptionalText(
                ProjectsFieldKeys.Highlights,
                "Highlights",
                HighlightsMaxLength,
                TranslationKeys.ValidationProjectsHighlightsMax),
            OptionalText(
                ProjectsFieldKeys.Description,
                "Description",
                DescriptionMaxLength,
                TranslationKeys.ValidationProjectsDescriptionMax)
        });

    public static readonly FieldSchema TechnologyNameField = new(
        ProjectsFieldKeys.TechnologyName,
        "Technology",
        IsRequired: true,
        TechnologyNameMaxLength,
        FieldFormat.Text,
        RequiredMessage: TranslationKeys.ValidationProjectsTechnologyNameRequired,
        MaximumLengthMessage: TranslationKeys.ValidationProjectsTechnologyNameMax);

    public static FieldValidator CreateEntryValidator()
    {
        return new FieldValidator(EntryFields);
    }

    public static FieldValidator CreateTechnologyValidator()
    {
        return new FieldValidator([TechnologyNameField]);
    }

    public static IReadOnlyList<FieldSchema> CreateSchemasForEntry(string entryId)
    {
        return EntryFields
            .Select(field => field with { Key = ProjectsFieldKeys.Build(entryId, field.Key) })
            .ToArray();
    }

    private static FieldSchema RequiredText(
        string key,
        string label,
        int maximumLength,
        string requiredMessageKey,
        string maximumLengthMessageKey)
    {
        return new FieldSchema(
            key,
            label,
            IsRequired: true,
            maximumLength,
            FieldFormat.Text,
            RequiredMessage: requiredMessageKey,
            MaximumLengthMessage: maximumLengthMessageKey);
    }

    private static FieldSchema OptionalText(string key, string label, int maximumLength, string maximumLengthMessageKey)
    {
        return new FieldSchema(
            key,
            label,
            IsRequired: false,
            maximumLength,
            FieldFormat.Text,
            RequiredMessage: string.Empty,
            MaximumLengthMessage: maximumLengthMessageKey);
    }

    private static FieldSchema OptionalUrl(
        string key,
        string label,
        int maximumLength,
        string maximumLengthMessageKey,
        string formatMessageKey)
    {
        return new FieldSchema(
            key,
            label,
            IsRequired: false,
            maximumLength,
            FieldFormat.Url,
            RequiredMessage: string.Empty,
            MaximumLengthMessage: maximumLengthMessageKey,
            FormatMessage: formatMessageKey);
    }

    private static FieldSchema OptionalMonth(string key, string invalidMessageKey)
    {
        return new FieldSchema(
            key,
            "Month",
            IsRequired: false,
            MaximumLength: 2,
            Format: FieldFormat.Month,
            RequiredMessage: string.Empty,
            MaximumLengthMessage: invalidMessageKey,
            FormatMessage: invalidMessageKey);
    }

    private static FieldSchema OptionalYear(string key, string invalidMessageKey)
    {
        return new FieldSchema(
            key,
            "Year",
            IsRequired: false,
            MaximumLength: 4,
            Format: FieldFormat.Year,
            RequiredMessage: string.Empty,
            MaximumLengthMessage: invalidMessageKey,
            FormatMessage: invalidMessageKey);
    }
}
