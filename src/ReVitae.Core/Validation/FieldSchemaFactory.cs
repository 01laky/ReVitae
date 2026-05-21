using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Validation;

public static class FieldSchemaFactory
{
    public static FieldSchema RequiredText(
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

    public static FieldSchema OptionalText(string key, string label, int maximumLength, string maximumLengthMessageKey)
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

    public static FieldSchema RequiredEmail(
        string key,
        string label,
        int maximumLength,
        string requiredMessageKey,
        string maximumLengthMessageKey,
        string formatMessageKey)
    {
        return new FieldSchema(
            key,
            label,
            IsRequired: true,
            maximumLength,
            FieldFormat.Email,
            RequiredMessage: requiredMessageKey,
            MaximumLengthMessage: maximumLengthMessageKey,
            FormatMessage: formatMessageKey);
    }

    public static FieldSchema OptionalUrl(
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

    public static FieldSchema RequiredUrl(
        string key,
        string label,
        int maximumLength,
        string requiredMessageKey,
        string maximumLengthMessageKey,
        string formatMessageKey)
    {
        return new FieldSchema(
            key,
            label,
            IsRequired: true,
            maximumLength,
            FieldFormat.Url,
            RequiredMessage: requiredMessageKey,
            MaximumLengthMessage: maximumLengthMessageKey,
            FormatMessage: formatMessageKey);
    }

    public static FieldSchema RequiredEmploymentType()
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

    public static FieldSchema RequiredDegreeType()
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

    public static FieldSchema RequiredLanguageProficiency(string key, string requiredMessageKey, string invalidMessageKey)
    {
        return new FieldSchema(
            key,
            "Proficiency",
            IsRequired: true,
            MaximumLength: 32,
            Format: FieldFormat.LanguageProficiency,
            RequiredMessage: requiredMessageKey,
            MaximumLengthMessage: invalidMessageKey,
            FormatMessage: invalidMessageKey);
    }

    public static FieldSchema OptionalCefrLevel(string key, string invalidMessageKey)
    {
        return new FieldSchema(
            key,
            "CEFR level",
            IsRequired: false,
            MaximumLength: 8,
            Format: FieldFormat.CefrLevel,
            RequiredMessage: string.Empty,
            MaximumLengthMessage: invalidMessageKey,
            FormatMessage: invalidMessageKey);
    }

    public static FieldSchema OptionalLanguageProficiency(string key, string invalidMessageKey)
    {
        return new FieldSchema(
            key,
            "Proficiency",
            IsRequired: false,
            MaximumLength: 32,
            Format: FieldFormat.LanguageProficiency,
            RequiredMessage: string.Empty,
            MaximumLengthMessage: invalidMessageKey,
            FormatMessage: invalidMessageKey);
    }

    public static FieldSchema RequiredProficiencyLevel(string key, string requiredMessageKey, string invalidMessageKey)
    {
        return new FieldSchema(
            key,
            "Proficiency",
            IsRequired: true,
            MaximumLength: 32,
            Format: FieldFormat.ProficiencyLevel,
            RequiredMessage: requiredMessageKey,
            MaximumLengthMessage: invalidMessageKey,
            FormatMessage: invalidMessageKey);
    }

    public static FieldSchema RequiredMonth(string key, string requiredMessageKey, string invalidMessageKey)
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

    public static FieldSchema RequiredYear(string key, string requiredMessageKey, string invalidMessageKey)
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

    public static FieldSchema OptionalMonth(string key, string invalidMessageKey)
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

    public static FieldSchema OptionalYear(string key, string invalidMessageKey)
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
