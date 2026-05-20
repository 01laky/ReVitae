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
}
