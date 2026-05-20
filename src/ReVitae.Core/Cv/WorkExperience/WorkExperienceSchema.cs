using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.WorkExperience;

public static class WorkExperienceSchema
{
    public const int JobTitleMaxLength = 120;
    public const int CompanyMaxLength = 160;
    public const int LocationMaxLength = 120;
    public const int DescriptionMaxLength = 2000;
    public const int AchievementsMaxLength = 2000;
    public const int TechnologiesMaxLength = 500;
    public const int CompanyUrlMaxLength = 240;

    public static readonly IReadOnlyList<FieldSchema> EntryFields = Array.AsReadOnly(
        new[]
        {
            RequiredText(WorkExperienceFieldKeys.JobTitle, "Job title", JobTitleMaxLength, TranslationKeys.ValidationWorkExperienceJobTitleRequired, TranslationKeys.ValidationWorkExperienceJobTitleMax),
            RequiredText(WorkExperienceFieldKeys.Company, "Company", CompanyMaxLength, TranslationKeys.ValidationWorkExperienceCompanyRequired, TranslationKeys.ValidationWorkExperienceCompanyMax),
            OptionalText(WorkExperienceFieldKeys.Location, "Location", LocationMaxLength, TranslationKeys.ValidationWorkExperienceLocationMax),
            RequiredEmploymentType(),
            RequiredMonth(WorkExperienceFieldKeys.StartMonth, TranslationKeys.ValidationWorkExperienceStartMonthRequired, TranslationKeys.ValidationWorkExperienceStartMonthInvalid),
            RequiredYear(WorkExperienceFieldKeys.StartYear, TranslationKeys.ValidationWorkExperienceStartYearRequired, TranslationKeys.ValidationWorkExperienceStartYearInvalid),
            OptionalMonth(WorkExperienceFieldKeys.EndMonth, TranslationKeys.ValidationWorkExperienceEndMonthInvalid),
            OptionalYear(WorkExperienceFieldKeys.EndYear, TranslationKeys.ValidationWorkExperienceEndYearInvalid),
            OptionalText(WorkExperienceFieldKeys.Description, "Description", DescriptionMaxLength, TranslationKeys.ValidationWorkExperienceDescriptionMax),
            OptionalText(WorkExperienceFieldKeys.Achievements, "Achievements", AchievementsMaxLength, TranslationKeys.ValidationWorkExperienceAchievementsMax),
            OptionalText(WorkExperienceFieldKeys.Technologies, "Technologies", TechnologiesMaxLength, TranslationKeys.ValidationWorkExperienceTechnologiesMax),
            OptionalUrl(WorkExperienceFieldKeys.CompanyUrl, "Company URL", CompanyUrlMaxLength, TranslationKeys.ValidationWorkExperienceCompanyUrlMax, TranslationKeys.ValidationWorkExperienceCompanyUrlFormat)
        });

    public static FieldValidator CreateEntryValidator()
    {
        return new FieldValidator(EntryFields);
    }

    public static IReadOnlyList<FieldSchema> CreateSchemasForEntry(string entryId)
    {
        return EntryFields
            .Select(field => field with { Key = WorkExperienceFieldKeys.Build(entryId, field.Key) })
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

    private static FieldSchema RequiredEmploymentType()
    {
        return new FieldSchema(
            WorkExperienceFieldKeys.EmploymentType,
            "Employment type",
            IsRequired: true,
            MaximumLength: 32,
            Format: FieldFormat.EmploymentType,
            RequiredMessage: TranslationKeys.ValidationWorkExperienceEmploymentTypeRequired,
            MaximumLengthMessage: TranslationKeys.ValidationWorkExperienceEmploymentTypeInvalid,
            FormatMessage: TranslationKeys.ValidationWorkExperienceEmploymentTypeInvalid);
    }

    private static FieldSchema RequiredMonth(string key, string requiredMessageKey, string invalidMessageKey)
    {
        return new FieldSchema(
            key,
            "Month",
            IsRequired: true,
            MaximumLength: 2,
            Format: FieldFormat.Month,
            RequiredMessage: requiredMessageKey,
            MaximumLengthMessage: invalidMessageKey,
            FormatMessage: invalidMessageKey);
    }

    private static FieldSchema RequiredYear(string key, string requiredMessageKey, string invalidMessageKey)
    {
        return new FieldSchema(
            key,
            "Year",
            IsRequired: true,
            MaximumLength: 4,
            Format: FieldFormat.Year,
            RequiredMessage: requiredMessageKey,
            MaximumLengthMessage: invalidMessageKey,
            FormatMessage: invalidMessageKey);
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
