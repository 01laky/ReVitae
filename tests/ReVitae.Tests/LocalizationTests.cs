using System.Globalization;
using ReVitae.Core.Localization;

namespace ReVitae.Tests;

public sealed class LocalizationTests
{
	[Fact]
	public void SupportedLanguages_ContainExpectedLanguageCodes()
	{
		var expectedCodes = new[]
		{
			"en",
			"es",
			"fr",
			"de",
			"pt",
			"it",
			"nl",
			"pl",
			"uk",
			"zh-Hans",
			"sk",
			"cs"
		};

		var actualCodes = AppLocalizer.SupportedLanguages
			.Select(language => language.Code)
			.ToArray();

		Assert.Equal(expectedCodes.Order(), actualCodes.Order());
		Assert.Equal(expectedCodes.Length, actualCodes.Distinct().Count());
	}

	[Theory]
	[InlineData("sk-SK", "sk")]
	[InlineData("cs-CZ", "cs")]
	[InlineData("en-US", "en")]
	[InlineData("es-MX", "es")]
	[InlineData("fr-CA", "fr")]
	[InlineData("zh-CN", "zh-Hans")]
	public void DetectSupportedLanguage_MapsSupportedCultures(string cultureName, string expectedLanguageCode)
	{
		var languageCode = AppLocalizer.DetectSupportedLanguage(new CultureInfo(cultureName));

		Assert.Equal(expectedLanguageCode, languageCode);
	}

	[Theory]
	[InlineData("ja-JP")]
	[InlineData("ko-KR")]
	[InlineData("ar-SA")]
	public void DetectSupportedLanguage_FallsBackToEnglishForUnsupportedCultures(string cultureName)
	{
		var languageCode = AppLocalizer.DetectSupportedLanguage(new CultureInfo(cultureName));

		Assert.Equal(AppLocalizer.FallbackLanguageCode, languageCode);
	}

	[Fact]
	public void GetTranslations_ContainsRequiredKeysForEverySupportedLanguage()
	{
		foreach (var language in AppLocalizer.SupportedLanguages)
		{
			var translations = AppLocalizer.GetTranslations(language.Code);

			foreach (var key in TranslationKeys.RequiredKeys)
			{
				Assert.True(
					translations.ContainsKey(key),
					$"Missing translation key '{key}' for language '{language.Code}'.");
				Assert.False(
					string.IsNullOrWhiteSpace(translations[key]),
					$"Empty translation value for key '{key}' in language '{language.Code}'.");
			}
		}
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("unknown-language")]
	public void DetectSupportedLanguage_FallsBackToEnglishForBlankOrUnknownValues(string? cultureName)
	{
		var languageCode = AppLocalizer.DetectSupportedLanguage(cultureName!);

		Assert.Equal(AppLocalizer.FallbackLanguageCode, languageCode);
	}

	[Theory]
	[InlineData("zh", "zh-Hans")]
	[InlineData("ZH-tw", "zh-Hans")]
	[InlineData("zh-Hans", "zh-Hans")]
	[InlineData("EN-us", "en")]
	[InlineData("SK-sk", "sk")]
	public void DetectSupportedLanguage_NormalizesSupportedCultureNames(string cultureName, string expectedLanguageCode)
	{
		var languageCode = AppLocalizer.DetectSupportedLanguage(cultureName);

		Assert.Equal(expectedLanguageCode, languageCode);
	}

	[Fact]
	public void DetectSupportedLanguage_AcceptsCultureInfoInstance()
	{
		var languageCode = AppLocalizer.DetectSupportedLanguage(new CultureInfo("de-DE"));

		Assert.Equal("de", languageCode);
	}

	[Fact]
	public void AppLocalizer_Constructor_NormalizesUnsupportedLanguageToEnglish()
	{
		var localizer = new AppLocalizer("ja-JP");

		Assert.Equal(AppLocalizer.FallbackLanguageCode, localizer.LanguageCode);
	}

	[Fact]
	public void AppLocalizer_Constructor_PreservesCanonicalSupportedLanguageCode()
	{
		var localizer = new AppLocalizer("sk-SK");

		Assert.Equal("sk", localizer.LanguageCode);
	}

	[Fact]
	public void Get_ReturnsTranslationForKnownKey()
	{
		var localizer = new AppLocalizer(AppLocalizer.FallbackLanguageCode);

		Assert.Equal("First name", localizer.Get(TranslationKeys.FirstName));
	}

	[Fact]
	public void Get_ReturnsKeyWhenTranslationIsMissing()
	{
		var localizer = new AppLocalizer(AppLocalizer.FallbackLanguageCode);

		Assert.Equal("missing.key", localizer.Get("missing.key"));
	}

	[Fact]
	public void Format_SubstitutesPlaceholdersUsingCurrentCulture()
	{
		var localizer = new AppLocalizer(AppLocalizer.FallbackLanguageCode);

		var formatted = localizer.Format(TranslationKeys.ExportedPdfTo, "/tmp/cv.pdf");

		Assert.Equal("Exported PDF to /tmp/cv.pdf.", formatted);
	}

	[Theory]
	[InlineData("sk", "Meno")]
	[InlineData("cs", "Jméno")]
	[InlineData("de", "Vorname")]
	public void GetTranslations_OverlayLanguagesOverrideSelectedUiStrings(string languageCode, string expectedFirstName)
	{
		var translations = AppLocalizer.GetTranslations(languageCode);

		Assert.Equal(expectedFirstName, translations[TranslationKeys.FirstName]);
	}

	[Fact]
	public void GetTranslations_OverlayLanguagesFallBackToEnglishForNonOverlayKeys()
	{
		var translations = AppLocalizer.GetTranslations("sk");

		Assert.Equal("Email", translations[TranslationKeys.Email]);
		Assert.Equal("LinkedIn URL", translations[TranslationKeys.LinkedInUrl]);
	}

	[Fact]
	public void GetTranslations_Sk_OverlaysOcrImportKeys()
	{
		var localizer = new AppLocalizer("sk");

		Assert.Contains("OCR", localizer.Get(TranslationKeys.IntroHelper), StringComparison.OrdinalIgnoreCase);
		Assert.Equal("Importovať ako sken (OCR)", localizer.Get(TranslationKeys.ImportForceOcr));
		Assert.Equal("Rozpoznávanie textu z obrázka…", localizer.Get(TranslationKeys.ImportRunningOcr));
		Assert.Equal("OCR nie je na tomto systéme k dispozícii.", localizer.Get(TranslationKeys.ImportErrorOcrUnavailable));
		Assert.Equal("Obrázky (JPEG, PNG, …)", localizer.Get(TranslationKeys.ImportRasterImageFileType));
	}

	[Fact]
	public void FromSystemCulture_ReturnsLocalizerWithDetectedLanguageCode()
	{
		var localizer = AppLocalizer.FromSystemCulture();

		Assert.Contains(
			AppLocalizer.SupportedLanguages,
			language => language.Code.Equals(localizer.LanguageCode, StringComparison.OrdinalIgnoreCase));
	}
}
