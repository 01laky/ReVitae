namespace ReVitae.Core.Localization;

public sealed record SupportedLanguage(string Code, string EnglishName, string NativeName, string FlagEmoji)
{
	public string DisplayName => $"{FlagEmoji} {NativeName}";
}
