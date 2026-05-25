using System.Text.Json.Nodes;

namespace ReVitae.Core.Ai.Import;

public static class AiImportCarryForwardSummaryBuilder
{
	public static string Build(JsonObject accumulated, int maxChars)
	{
		if (maxChars <= 0)
		{
			return string.Empty;
		}

		var parts = new List<string>();
		if (accumulated["personalInformation"] is JsonObject personal)
		{
			var name = $"{personal["firstName"]} {personal["lastName"]}".Trim();
			if (!string.IsNullOrWhiteSpace(name))
			{
				parts.Add($"Name: {name}");
			}

			if (personal["email"]?.GetValue<string>() is { Length: > 0 } email)
			{
				parts.Add($"Email: {email}");
			}
		}

		AppendArrayCount(parts, accumulated, "workExperience", "Work");
		AppendArrayCount(parts, accumulated, "education", "Education");
		AppendArrayCount(parts, accumulated, "skills", "Skills");
		AppendArrayCount(parts, accumulated, "languages", "Languages");

		var summary = parts.Count == 0 ? "none" : string.Join("; ", parts);
		return summary.Length <= maxChars ? summary : summary[..maxChars];
	}

	private static void AppendArrayCount(List<string> parts, JsonObject root, string key, string label)
	{
		if (root[key] is JsonArray array && array.Count > 0)
		{
			parts.Add($"{label}: {array.Count} entries");
		}
	}
}
