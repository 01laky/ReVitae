namespace ReVitae.Core.Cv.Languages;

public static class LanguageSuggestions
{
	public static IReadOnlyList<string> All { get; } =
	[
		"English",
		"Slovak",
		"Czech",
		"German",
		"French",
		"Spanish",
		"Italian",
		"Portuguese",
		"Polish",
		"Ukrainian",
		"Hungarian",
		"Russian",
		"Dutch",
		"Swedish",
		"Norwegian",
		"Danish",
		"Finnish",
		"Romanian",
		"Greek",
		"Turkish",
		"Arabic",
		"Chinese",
		"Japanese",
		"Korean",
		"Hindi"
	];

	public static IReadOnlyList<string> Filter(string? query, int maxResults = 12)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return All.Take(maxResults).ToArray();
		}

		return All
			.Where(language => language.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
			.Take(maxResults)
			.ToArray();
	}
}
