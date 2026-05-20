using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv;

public static class MainPersonalInformationSchema
{
    public static readonly IReadOnlyList<FieldSchema> Fields = Array.AsReadOnly(
        new[]
        {
        RequiredText(MainPersonalInformationFieldKeys.FirstName, "First name", 80, TranslationKeys.ValidationFirstNameRequired, TranslationKeys.ValidationFirstNameMax),
        RequiredText(MainPersonalInformationFieldKeys.LastName, "Last name", 80, TranslationKeys.ValidationLastNameRequired, TranslationKeys.ValidationLastNameMax),
        OptionalText(MainPersonalInformationFieldKeys.ProfessionalTitle, "Professional title", 120, TranslationKeys.ValidationProfessionalTitleMax),
            new FieldSchema(
                MainPersonalInformationFieldKeys.Email,
                "Email",
                IsRequired: true,
                MaximumLength: 160,
                Format: FieldFormat.Email,
            RequiredMessage: TranslationKeys.ValidationEmailRequired,
            MaximumLengthMessage: TranslationKeys.ValidationEmailMax,
            FormatMessage: TranslationKeys.ValidationEmailFormat),
        OptionalText(MainPersonalInformationFieldKeys.Phone, "Phone", 40, TranslationKeys.ValidationPhoneMax),
        OptionalText(MainPersonalInformationFieldKeys.Location, "Location", 120, TranslationKeys.ValidationLocationMax),
        OptionalUrl(MainPersonalInformationFieldKeys.LinkedInUrl, "LinkedIn URL", 240, TranslationKeys.ValidationLinkedInUrlMax, TranslationKeys.ValidationLinkedInUrlFormat),
        OptionalUrl(MainPersonalInformationFieldKeys.PortfolioUrl, "Portfolio / website URL", 240, TranslationKeys.ValidationPortfolioUrlMax, TranslationKeys.ValidationPortfolioUrlFormat),
        OptionalUrl(MainPersonalInformationFieldKeys.GitHubUrl, "GitHub URL", 240, TranslationKeys.ValidationGitHubUrlMax, TranslationKeys.ValidationGitHubUrlFormat),
        OptionalText(MainPersonalInformationFieldKeys.ShortSummary, "Short summary", 800, TranslationKeys.ValidationShortSummaryMax)
        });

    public static FieldValidator CreateValidator()
    {
        return new FieldValidator(Fields);
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
}
