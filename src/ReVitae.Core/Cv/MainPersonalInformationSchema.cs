using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv;

public static class MainPersonalInformationSchema
{
    public static readonly IReadOnlyList<FieldSchema> Fields = Array.AsReadOnly(
        new[]
        {
            RequiredText(MainPersonalInformationFieldKeys.FirstName, "First name", 80),
            RequiredText(MainPersonalInformationFieldKeys.LastName, "Last name", 80),
            OptionalText(MainPersonalInformationFieldKeys.ProfessionalTitle, "Professional title", 120),
            new FieldSchema(
                MainPersonalInformationFieldKeys.Email,
                "Email",
                IsRequired: true,
                MaximumLength: 160,
                Format: FieldFormat.Email,
                RequiredMessage: "Email is required.",
                MaximumLengthMessage: "Email must be 160 characters or fewer.",
                FormatMessage: "Email must be a valid email address."),
            OptionalText(MainPersonalInformationFieldKeys.Phone, "Phone", 40),
            OptionalText(MainPersonalInformationFieldKeys.Location, "Location", 120),
            OptionalUrl(MainPersonalInformationFieldKeys.LinkedInUrl, "LinkedIn URL", 240),
            OptionalUrl(MainPersonalInformationFieldKeys.PortfolioUrl, "Portfolio / website URL", 240),
            OptionalUrl(MainPersonalInformationFieldKeys.GitHubUrl, "GitHub URL", 240),
            OptionalText(MainPersonalInformationFieldKeys.ShortSummary, "Short summary", 800)
        });

    public static FieldValidator CreateValidator()
    {
        return new FieldValidator(Fields);
    }

    private static FieldSchema RequiredText(string key, string label, int maximumLength)
    {
        return new FieldSchema(
            key,
            label,
            IsRequired: true,
            maximumLength,
            FieldFormat.Text,
            RequiredMessage: $"{label} is required.",
            MaximumLengthMessage: $"{label} must be {maximumLength} characters or fewer.");
    }

    private static FieldSchema OptionalText(string key, string label, int maximumLength)
    {
        return new FieldSchema(
            key,
            label,
            IsRequired: false,
            maximumLength,
            FieldFormat.Text,
            RequiredMessage: string.Empty,
            MaximumLengthMessage: $"{label} must be {maximumLength} characters or fewer.");
    }

    private static FieldSchema OptionalUrl(string key, string label, int maximumLength)
    {
        return new FieldSchema(
            key,
            label,
            IsRequired: false,
            maximumLength,
            FieldFormat.Url,
            RequiredMessage: string.Empty,
            MaximumLengthMessage: $"{label} must be {maximumLength} characters or fewer.",
            FormatMessage: $"{label} must be a valid http or https URL.");
    }
}
