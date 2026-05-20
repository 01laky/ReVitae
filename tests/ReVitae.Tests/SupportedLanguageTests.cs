using ReVitae.Core.Localization;

namespace ReVitae.Tests;

public sealed class SupportedLanguageTests
{
    [Fact]
    public void SupportedLanguages_AllCodesAreUnique()
    {
        var codes = AppLocalizer.SupportedLanguages
            .Select(language => language.Code)
            .ToArray();

        Assert.Equal(codes.Length, codes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Theory]
    [InlineData("en", "English", "English", "🇬🇧")]
    [InlineData("sk", "Slovak", "Slovenčina", "🇸🇰")]
    [InlineData("zh-Hans", "Chinese Simplified", "简体中文", "🇨🇳")]
    public void SupportedLanguage_StoresMetadata(string code, string englishName, string nativeName, string flagEmoji)
    {
        var language = AppLocalizer.SupportedLanguages.Single(item => item.Code == code);

        Assert.Equal(englishName, language.EnglishName);
        Assert.Equal(nativeName, language.NativeName);
        Assert.Equal(flagEmoji, language.FlagEmoji);
    }

    [Theory]
    [InlineData("en", "🇬🇧 English")]
    [InlineData("sk", "🇸🇰 Slovenčina")]
    [InlineData("cs", "🇨🇿 Čeština")]
    public void DisplayName_IncludesFlagEmojiAndNativeName(string code, string expectedDisplayName)
    {
        var language = AppLocalizer.SupportedLanguages.Single(item => item.Code == code);

        Assert.Equal(expectedDisplayName, language.DisplayName);
    }

    [Fact]
    public void SupportedLanguages_AllEntriesHaveNonEmptyMetadata()
    {
        foreach (var language in AppLocalizer.SupportedLanguages)
        {
            Assert.False(string.IsNullOrWhiteSpace(language.Code));
            Assert.False(string.IsNullOrWhiteSpace(language.EnglishName));
            Assert.False(string.IsNullOrWhiteSpace(language.NativeName));
            Assert.False(string.IsNullOrWhiteSpace(language.FlagEmoji));
            Assert.False(string.IsNullOrWhiteSpace(language.DisplayName));
        }
    }
}
