using ReVitae.Core.Localization;

namespace ReVitae.Core.Cv.Languages;

public static class LanguageProficiencyExtensions
{
	public static string ToTranslationKey(this LanguageProficiency proficiency)
	{
		return proficiency switch
		{
			LanguageProficiency.Elementary => TranslationKeys.LanguageProficiencyElementary,
			LanguageProficiency.Intermediate => TranslationKeys.LanguageProficiencyIntermediate,
			LanguageProficiency.Advanced => TranslationKeys.LanguageProficiencyAdvanced,
			LanguageProficiency.Fluent => TranslationKeys.LanguageProficiencyFluent,
			LanguageProficiency.Native => TranslationKeys.LanguageProficiencyNative,
			_ => TranslationKeys.LanguageProficiencyIntermediate
		};
	}

	public static IReadOnlyList<LanguageProficiency> SupportedValues { get; } =
	[
		LanguageProficiency.Elementary,
		LanguageProficiency.Intermediate,
		LanguageProficiency.Advanced,
		LanguageProficiency.Fluent,
		LanguageProficiency.Native
	];
}
