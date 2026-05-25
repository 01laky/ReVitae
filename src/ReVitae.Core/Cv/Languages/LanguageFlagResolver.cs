namespace ReVitae.Core.Cv.Languages;

public static class LanguageFlagResolver
{
	private static readonly IReadOnlyDictionary<string, string> FlagsByLanguage =
		new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["English"] = "🇬🇧",
			["Slovak"] = "🇸🇰",
			["Czech"] = "🇨🇿",
			["German"] = "🇩🇪",
			["French"] = "🇫🇷",
			["Spanish"] = "🇪🇸",
			["Italian"] = "🇮🇹",
			["Portuguese"] = "🇵🇹",
			["Polish"] = "🇵🇱",
			["Ukrainian"] = "🇺🇦",
			["Hungarian"] = "🇭🇺",
			["Russian"] = "🇷🇺",
			["Dutch"] = "🇳🇱",
			["Swedish"] = "🇸🇪",
			["Norwegian"] = "🇳🇴",
			["Danish"] = "🇩🇰",
			["Finnish"] = "🇫🇮",
			["Romanian"] = "🇷🇴",
			["Greek"] = "🇬🇷",
			["Turkish"] = "🇹🇷",
			["Arabic"] = "🇸🇦",
			["Chinese"] = "🇨🇳",
			["Japanese"] = "🇯🇵",
			["Korean"] = "🇰🇷",
			["Hindi"] = "🇮🇳"
		};

	public static string ResolveFlagEmoji(string? languageName)
	{
		if (string.IsNullOrWhiteSpace(languageName))
		{
			return string.Empty;
		}

		return FlagsByLanguage.TryGetValue(languageName.Trim(), out var flag) ? flag : "🌐";
	}
}
