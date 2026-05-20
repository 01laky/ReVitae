using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.Education;

public static class EducationSchema
{
    public const int InstitutionMaxLength = 160;
    public const int DegreeMaxLength = 160;
    public const int FieldOfStudyMaxLength = 160;
    public const int LocationMaxLength = 120;
    public const int GradeMaxLength = 80;
    public const int DescriptionMaxLength = 2000;
    public const int InstitutionUrlMaxLength = 240;

    public static readonly IReadOnlyList<FieldSchema> EntryFields = Array.AsReadOnly(
        new[]
        {
            RequiredText(EducationFieldKeys.Institution, "Institution", InstitutionMaxLength, TranslationKeys.ValidationEducationInstitutionRequired, TranslationKeys.ValidationEducationInstitutionMax),
            RequiredText(EducationFieldKeys.Degree, "Degree", DegreeMaxLength, TranslationKeys.ValidationEducationDegreeRequired, TranslationKeys.ValidationEducationDegreeMax),
            OptionalText(EducationFieldKeys.FieldOfStudy, "Field of study", FieldOfStudyMaxLength, TranslationKeys.ValidationEducationFieldOfStudyMax),
            OptionalText(EducationFieldKeys.Location, "Location", LocationMaxLength, TranslationKeys.ValidationEducationLocationMax),
            RequiredDegreeType(),
            RequiredMonth(EducationFieldKeys.StartMonth, TranslationKeys.ValidationEducationStartMonthRequired, TranslationKeys.ValidationEducationStartMonthInvalid),
            RequiredYear(EducationFieldKeys.StartYear, TranslationKeys.ValidationEducationStartYearRequired, TranslationKeys.ValidationEducationStartYearInvalid),
            OptionalMonth(EducationFieldKeys.EndMonth, TranslationKeys.ValidationEducationEndMonthInvalid),
            OptionalYear(EducationFieldKeys.EndYear, TranslationKeys.ValidationEducationEndYearInvalid),
            OptionalText(EducationFieldKeys.Grade, "Grade", GradeMaxLength, TranslationKeys.ValidationEducationGradeMax),
            OptionalText(EducationFieldKeys.Description, "Description", DescriptionMaxLength, TranslationKeys.ValidationEducationDescriptionMax),
            OptionalUrl(EducationFieldKeys.InstitutionUrl, "Institution URL", InstitutionUrlMaxLength, TranslationKeys.ValidationEducationInstitutionUrlMax, TranslationKeys.ValidationEducationInstitutionUrlFormat)
        });

    public static FieldValidator CreateEntryValidator()
    {
        return new FieldValidator(EntryFields);
    }

    public static IReadOnlyList<FieldSchema> CreateSchemasForEntry(string entryId)
    {
        return EntryFields
            .Select(field => field with { Key = EducationFieldKeys.Build(entryId, field.Key) })
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

    private static FieldSchema RequiredDegreeType()
    {
        return new FieldSchema(
            EducationFieldKeys.DegreeType,
            "Degree type",
            IsRequired: true,
            MaximumLength: 32,
            Format: FieldFormat.DegreeType,
            RequiredMessage: TranslationKeys.ValidationEducationDegreeTypeRequired,
            MaximumLengthMessage: TranslationKeys.ValidationEducationDegreeTypeInvalid,
            FormatMessage: TranslationKeys.ValidationEducationDegreeTypeInvalid);
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
