namespace ReVitae.Core.Ai.Import;

public static class AiImportLimits
{
	public const int MinSourceCharsForAi = 80;
	public const int MaxSourceChars = 120_000;

	public static string TruncateSourceText(string normalizedText)
	{
		if (string.IsNullOrEmpty(normalizedText) || normalizedText.Length <= MaxSourceChars)
		{
			return normalizedText;
		}

		return normalizedText[..MaxSourceChars];
	}
}
