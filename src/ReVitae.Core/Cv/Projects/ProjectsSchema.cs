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
            FieldSchemaFactory.RequiredText(
                ProjectsFieldKeys.Name,
                "Project name",
                NameMaxLength,
                TranslationKeys.ValidationProjectsNameRequired,
                TranslationKeys.ValidationProjectsNameMax),
            FieldSchemaFactory.OptionalText(
                ProjectsFieldKeys.Role,
                "Role",
                RoleMaxLength,
                TranslationKeys.ValidationProjectsRoleMax),
            FieldSchemaFactory.OptionalText(
                ProjectsFieldKeys.Organization,
                "Organization or context",
                OrganizationMaxLength,
                TranslationKeys.ValidationProjectsOrganizationMax),
            FieldSchemaFactory.OptionalMonth(
                ProjectsFieldKeys.StartMonth,
                TranslationKeys.ValidationProjectsStartMonthInvalid),
            FieldSchemaFactory.OptionalYear(
                ProjectsFieldKeys.StartYear,
                TranslationKeys.ValidationProjectsStartYearInvalid),
            FieldSchemaFactory.OptionalMonth(
                ProjectsFieldKeys.EndMonth,
                TranslationKeys.ValidationProjectsEndMonthInvalid),
            FieldSchemaFactory.OptionalYear(
                ProjectsFieldKeys.EndYear,
                TranslationKeys.ValidationProjectsEndYearInvalid),
            FieldSchemaFactory.OptionalUrl(
                ProjectsFieldKeys.ProjectUrl,
                "Project URL",
                ProjectUrlMaxLength,
                TranslationKeys.ValidationProjectsProjectUrlMax,
                TranslationKeys.ValidationProjectsProjectUrlFormat),
            FieldSchemaFactory.OptionalText(
                ProjectsFieldKeys.Highlights,
                "Highlights",
                HighlightsMaxLength,
                TranslationKeys.ValidationProjectsHighlightsMax),
            FieldSchemaFactory.OptionalText(
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
}
