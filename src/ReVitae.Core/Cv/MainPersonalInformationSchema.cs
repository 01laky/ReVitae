using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv;

public static class MainPersonalInformationSchema
{
	public static readonly IReadOnlyList<FieldSchema> Fields = Array.AsReadOnly(
		new[]
		{
			FieldSchemaFactory.RequiredText(MainPersonalInformationFieldKeys.FirstName, "First name", 80, TranslationKeys.ValidationFirstNameRequired, TranslationKeys.ValidationFirstNameMax),
			FieldSchemaFactory.RequiredText(MainPersonalInformationFieldKeys.LastName, "Last name", 80, TranslationKeys.ValidationLastNameRequired, TranslationKeys.ValidationLastNameMax),
			FieldSchemaFactory.OptionalText(MainPersonalInformationFieldKeys.ProfessionalTitle, "Professional title", 120, TranslationKeys.ValidationProfessionalTitleMax),
			FieldSchemaFactory.RequiredEmail(
				MainPersonalInformationFieldKeys.Email,
				"Email",
				160,
				TranslationKeys.ValidationEmailRequired,
				TranslationKeys.ValidationEmailMax,
				TranslationKeys.ValidationEmailFormat),
			FieldSchemaFactory.OptionalText(MainPersonalInformationFieldKeys.Phone, "Phone", 40, TranslationKeys.ValidationPhoneMax),
			FieldSchemaFactory.OptionalText(MainPersonalInformationFieldKeys.Location, "Location", 120, TranslationKeys.ValidationLocationMax),
			FieldSchemaFactory.OptionalUrl(MainPersonalInformationFieldKeys.LinkedInUrl, "LinkedIn URL", 240, TranslationKeys.ValidationLinkedInUrlMax, TranslationKeys.ValidationLinkedInUrlFormat),
			FieldSchemaFactory.OptionalUrl(MainPersonalInformationFieldKeys.PortfolioUrl, "Portfolio / website URL", 240, TranslationKeys.ValidationPortfolioUrlMax, TranslationKeys.ValidationPortfolioUrlFormat),
			FieldSchemaFactory.OptionalUrl(MainPersonalInformationFieldKeys.GitHubUrl, "GitHub URL", 240, TranslationKeys.ValidationGitHubUrlMax, TranslationKeys.ValidationGitHubUrlFormat),
			FieldSchemaFactory.OptionalText(MainPersonalInformationFieldKeys.ShortSummary, "Short summary", 800, TranslationKeys.ValidationShortSummaryMax)
		});

	public static FieldValidator CreateValidator()
	{
		return new FieldValidator(Fields);
	}
}
