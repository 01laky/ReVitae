namespace ReVitae.Core.Ai.Import;

public static class AiImportLimits
{
	public const int MinSourceCharsForAi = 80;
	public const int MaxSourceChars = 120_000;

	// 045 C.9 — cap on targeted field-repair payload; lowest-confidence fields are kept first.
	public const int MaxRepairFields = 25;

	// Per-field source window sent with each repair target.
	public const int RepairSourceWindowChars = 600;

	public static string TruncateSourceText(string normalizedText)
	{
		if (string.IsNullOrEmpty(normalizedText) || normalizedText.Length <= MaxSourceChars)
		{
			return normalizedText;
		}

		return normalizedText[..MaxSourceChars];
	}
}
