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
            RequiredText(
                CertificatesFieldKeys.Name,
                "Certificate name",
                NameMaxLength,
                TranslationKeys.ValidationCertificatesNameRequired,
                TranslationKeys.ValidationCertificatesNameMax),
            RequiredText(
                CertificatesFieldKeys.Issuer,
                "Issuing organization",
                IssuerMaxLength,
                TranslationKeys.ValidationCertificatesIssuerRequired,
                TranslationKeys.ValidationCertificatesIssuerMax),
            RequiredMonth(
                CertificatesFieldKeys.IssueMonth,
                TranslationKeys.ValidationCertificatesIssueMonthRequired,
                TranslationKeys.ValidationCertificatesIssueMonthInvalid),
            RequiredYear(
                CertificatesFieldKeys.IssueYear,
                TranslationKeys.ValidationCertificatesIssueYearRequired,
                TranslationKeys.ValidationCertificatesIssueYearInvalid),
            OptionalMonth(
                CertificatesFieldKeys.ExpirationMonth,
                TranslationKeys.ValidationCertificatesExpirationMonthInvalid),
            OptionalYear(
                CertificatesFieldKeys.ExpirationYear,
                TranslationKeys.ValidationCertificatesExpirationYearInvalid),
            OptionalText(
                CertificatesFieldKeys.CredentialId,
                "Credential ID",
                CredentialIdMaxLength,
                TranslationKeys.ValidationCertificatesCredentialIdMax),
            OptionalUrl(
                CertificatesFieldKeys.CredentialUrl,
                "Credential URL",
                CredentialUrlMaxLength,
                TranslationKeys.ValidationCertificatesCredentialUrlMax,
                TranslationKeys.ValidationCertificatesCredentialUrlFormat),
            OptionalText(
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
