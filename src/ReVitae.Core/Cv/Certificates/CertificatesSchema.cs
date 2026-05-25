using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.Certificates;

public static class CertificatesSchema
{
	public const int NameMaxLength = 160;
	public const int IssuerMaxLength = 160;
	public const int CredentialIdMaxLength = 80;
	public const int CredentialUrlMaxLength = 240;
	public const int DescriptionMaxLength = 500;

	public static readonly IReadOnlyList<FieldSchema> EntryFields = Array.AsReadOnly(
		new[]
		{
			FieldSchemaFactory.RequiredText(
				CertificatesFieldKeys.Name,
				"Certificate name",
				NameMaxLength,
				TranslationKeys.ValidationCertificatesNameRequired,
				TranslationKeys.ValidationCertificatesNameMax),
			FieldSchemaFactory.RequiredText(
				CertificatesFieldKeys.Issuer,
				"Issuing organization",
				IssuerMaxLength,
				TranslationKeys.ValidationCertificatesIssuerRequired,
				TranslationKeys.ValidationCertificatesIssuerMax),
			FieldSchemaFactory.RequiredMonth(
				CertificatesFieldKeys.IssueMonth,
				TranslationKeys.ValidationCertificatesIssueMonthRequired,
				TranslationKeys.ValidationCertificatesIssueMonthInvalid),
			FieldSchemaFactory.RequiredYear(
				CertificatesFieldKeys.IssueYear,
				TranslationKeys.ValidationCertificatesIssueYearRequired,
				TranslationKeys.ValidationCertificatesIssueYearInvalid),
			FieldSchemaFactory.OptionalMonth(
				CertificatesFieldKeys.ExpirationMonth,
				TranslationKeys.ValidationCertificatesExpirationMonthInvalid),
			FieldSchemaFactory.OptionalYear(
				CertificatesFieldKeys.ExpirationYear,
				TranslationKeys.ValidationCertificatesExpirationYearInvalid),
			FieldSchemaFactory.OptionalText(
				CertificatesFieldKeys.CredentialId,
				"Credential ID",
				CredentialIdMaxLength,
				TranslationKeys.ValidationCertificatesCredentialIdMax),
			FieldSchemaFactory.OptionalUrl(
				CertificatesFieldKeys.CredentialUrl,
				"Credential URL",
				CredentialUrlMaxLength,
				TranslationKeys.ValidationCertificatesCredentialUrlMax,
				TranslationKeys.ValidationCertificatesCredentialUrlFormat),
			FieldSchemaFactory.OptionalText(
				CertificatesFieldKeys.Description,
				"Description or note",
				DescriptionMaxLength,
				TranslationKeys.ValidationCertificatesDescriptionMax)
		});

	public static FieldValidator CreateEntryValidator()
	{
		return new FieldValidator(EntryFields);
	}

	public static IReadOnlyList<FieldSchema> CreateSchemasForEntry(string entryId)
	{
		return EntryFields
			.Select(field => field with { Key = CertificatesFieldKeys.Build(entryId, field.Key) })
			.ToArray();
	}
}
