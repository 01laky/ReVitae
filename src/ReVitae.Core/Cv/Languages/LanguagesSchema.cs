using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.Languages;

public static class LanguagesSchema
{
    public const int LanguageMaxLength = 80;
    public const int CertificateMaxLength = 120;

    public static readonly IReadOnlyList<FieldSchema> EntryFields = Array.AsReadOnly(
        new[]
        {
            RequiredText(LanguagesFieldKeys.Language, TranslationKeys.ValidationLanguagesLanguageRequired, TranslationKeys.ValidationLanguagesLanguageMax),
            RequiredLanguageProficiency(),
            OptionalCefrLevel(),
            OptionalText(LanguagesFieldKeys.Certificate, TranslationKeys.ValidationLanguagesCertificateMax),
            OptionalLanguageProficiency(LanguagesFieldKeys.Reading),
            OptionalLanguageProficiency(LanguagesFieldKeys.Writing),
            OptionalLanguageProficiency(LanguagesFieldKeys.Speaking),
            OptionalLanguageProficiency(LanguagesFieldKeys.Listening)
        });

    public static FieldValidator CreateEntryValidator()
    {
        return new FieldValidator(EntryFields);
    }

    public static IReadOnlyList<FieldSchema> CreateSchemasForEntry(string entryId)
    {
        return EntryFields
            .Select(field => field with { Key = LanguagesFieldKeys.Build(entryId, field.Key) })
            .ToArray();
    }

    private static FieldSchema RequiredText(string key, string requiredMessageKey, string maximumLengthMessageKey)
    {
        return new FieldSchema(
            key,
            key,
            IsRequired: true,
            LanguageMaxLength,
            FieldFormat.Text,
            RequiredMessage: requiredMessageKey,
            MaximumLengthMessage: maximumLengthMessageKey);
    }

    private static FieldSchema OptionalText(string key, string maximumLengthMessageKey)
    {
        return new FieldSchema(
            key,
            key,
            IsRequired: false,
            CertificateMaxLength,
            FieldFormat.Text,
            RequiredMessage: string.Empty,
            MaximumLengthMessage: maximumLengthMessageKey);
    }

    private static FieldSchema RequiredLanguageProficiency()
    {
        return new FieldSchema(
            LanguagesFieldKeys.Proficiency,
            LanguagesFieldKeys.Proficiency,
            IsRequired: true,
            MaximumLength: 32,
            Format: FieldFormat.LanguageProficiency,
            RequiredMessage: TranslationKeys.ValidationLanguagesProficiencyRequired,
            MaximumLengthMessage: TranslationKeys.ValidationLanguagesProficiencyInvalid,
            FormatMessage: TranslationKeys.ValidationLanguagesProficiencyInvalid);
    }

    private static FieldSchema OptionalLanguageProficiency(string key)
    {
        return new FieldSchema(
            key,
            key,
            IsRequired: false,
            MaximumLength: 32,
            Format: FieldFormat.LanguageProficiency,
            RequiredMessage: string.Empty,
            MaximumLengthMessage: TranslationKeys.ValidationLanguagesProficiencyInvalid,
            FormatMessage: TranslationKeys.ValidationLanguagesProficiencyInvalid);
    }

    private static FieldSchema OptionalCefrLevel()
    {
        return new FieldSchema(
            LanguagesFieldKeys.CefrLevel,
            LanguagesFieldKeys.CefrLevel,
            IsRequired: false,
            MaximumLength: 4,
            Format: FieldFormat.CefrLevel,
            RequiredMessage: string.Empty,
            MaximumLengthMessage: TranslationKeys.ValidationLanguagesCefrInvalid,
            FormatMessage: TranslationKeys.ValidationLanguagesCefrInvalid);
    }
}
