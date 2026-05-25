namespace ReVitae.Core.Ai;

public static class AiModelInstallationMatcher
{
	public static bool IsTagForModel(string installedTag, string catalogTag) =>
		string.Equals(installedTag, catalogTag, StringComparison.OrdinalIgnoreCase) ||
		installedTag.StartsWith($"{catalogTag}:", StringComparison.OrdinalIgnoreCase);

	public static IReadOnlyList<string> GetMatchingTags(
		IEnumerable<string> installedTags,
		string catalogTag) =>
		installedTags
			.Where(tag => IsTagForModel(tag, catalogTag))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

	public static bool IsModelInstalled(IEnumerable<string> installedTags, string catalogTag) =>
		GetMatchingTags(installedTags, catalogTag).Count > 0;
}
