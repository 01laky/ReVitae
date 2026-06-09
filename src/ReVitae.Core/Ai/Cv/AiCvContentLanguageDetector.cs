namespace ReVitae.Core.Ai.Cv;

/// <summary>
/// Lightweight heuristic detector for the language of CV <em>content</em> (045 C.4), so a
/// rewrite stays in the CV's own language even when the UI culture differs. Detection is
/// diacritic / signal based for the languages whose rewrite instruction ReVitae can author;
/// when no strong signal is present it returns the supplied fallback culture.
/// </summary>
public static class AiCvContentLanguageDetector
{
	// Characters that strongly signal Slovak (and shared Slovak/Czech) content.
	private const string SlovakSignal = "áäčďéíĺľňóôŕšťúýžÁÄČĎÉÍĹĽŇÓÔŔŠŤÚÝŽ";

	// Characters specific to Czech that Slovak does not use.
	private const string CzechSignal = "ěřůĚŘŮ";

	private const int MinSignalChars = 2;

	public static string Detect(string? text, string fallbackCulture)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return fallbackCulture;
		}

		var czech = 0;
		var slovak = 0;
		foreach (var c in text)
		{
			if (CzechSignal.Contains(c, StringComparison.Ordinal))
			{
				czech++;
			}
			else if (SlovakSignal.Contains(c, StringComparison.Ordinal))
			{
				slovak++;
			}
		}

		var total = czech + slovak;

		// Any Czech-specific letters (ě/ř/ů) are a strong signal: Slovak does not use them.
		if (czech >= 1 && total >= MinSignalChars)
		{
			return "cs";
		}

		if (total >= MinSignalChars)
		{
			return "sk";
		}

		return fallbackCulture;
	}
}
