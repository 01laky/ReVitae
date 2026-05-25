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
			FieldSchemaFactory.RequiredText(
				LanguagesFieldKeys.Language,
				LanguagesFieldKeys.Language,
				LanguageMaxLength,
				TranslationKeys.ValidationLanguagesLanguageRequired,
				TranslationKeys.ValidationLanguagesLanguageMax),
			FieldSchemaFactory.RequiredLanguageProficiency(
				LanguagesFieldKeys.Proficiency,
				TranslationKeys.ValidationLanguagesProficiencyRequired,
				TranslationKeys.ValidationLanguagesProficiencyInvalid),
			FieldSchemaFactory.OptionalCefrLevel(
				LanguagesFieldKeys.CefrLevel,
				TranslationKeys.ValidationLanguagesCefrInvalid),
			FieldSchemaFactory.OptionalText(
				LanguagesFieldKeys.Certificate,
				LanguagesFieldKeys.Certificate,
				CertificateMaxLength,
				TranslationKeys.ValidationLanguagesCertificateMax),
			FieldSchemaFactory.OptionalLanguageProficiency(
				LanguagesFieldKeys.Reading,
				TranslationKeys.ValidationLanguagesProficiencyInvalid),
			FieldSchemaFactory.OptionalLanguageProficiency(
				LanguagesFieldKeys.Writing,
				TranslationKeys.ValidationLanguagesProficiencyInvalid),
			FieldSchemaFactory.OptionalLanguageProficiency(
				LanguagesFieldKeys.Speaking,
				TranslationKeys.ValidationLanguagesProficiencyInvalid),
			FieldSchemaFactory.OptionalLanguageProficiency(
				LanguagesFieldKeys.Listening,
				TranslationKeys.ValidationLanguagesProficiencyInvalid)
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
}
