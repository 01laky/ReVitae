using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.AdditionalInformation;

public static class AdditionalInformationSchema
{
	public const int ContentMaxLength = 3000;

	public static readonly IReadOnlyList<FieldSchema> Fields = Array.AsReadOnly(
		new[]
		{
			OptionalText(
				AdditionalInformationFieldKeys.Content,
				"Content",
				ContentMaxLength,
				TranslationKeys.ValidationAdditionalInformationContentMax)
		});

	public static FieldValidator CreateValidator()
	{
		return new FieldValidator(Fields);
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
}
