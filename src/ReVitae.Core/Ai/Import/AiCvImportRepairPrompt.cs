using System.Text;
using System.Text.RegularExpressions;
using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Import;

/// <summary>Builds the prompt for one section's batch of repair targets (045 B.2).</summary>
public static class AiCvImportRepairPromptBuilder
{
	public const string SystemRole =
		"You correct OCR and parsing errors in CV field values using the source text. " +
		"For each numbered field return one line \"N: corrected value\". Keep the value's " +
		"original language and spelling. If a value is already correct, repeat it unchanged. " +
		"Do not add new fields, invent data, or change values the source does not support. " +
		"Plain text only — no JSON, no markdown.";

	public static AiCvPromptMessages Build(
		CvImportSectionId section,
		IReadOnlyList<AiImportFieldRepairTarget> targets,
		string sourceWindow,
		string uiCulture)
	{
		var language = AiCvPromptBuilder.ResolveLanguageInstruction(uiCulture);
		var system = $"{SystemRole} {language}";

		var sb = new StringBuilder();
		sb.AppendLine($"Section: {section}");
		sb.AppendLine("Fields to verify and correct:");
		for (var i = 0; i < targets.Count; i++)
		{
			sb.AppendLine($"{i + 1}: {targets[i].CurrentValue.Trim()}");
		}

		sb.AppendLine();
		sb.AppendLine("Source text (for reference):");
		sb.AppendLine(Truncate(sourceWindow, AiImportLimits.RepairSourceWindowChars));

		return new AiCvPromptMessages(system, sb.ToString());
	}

	private static string Truncate(string text, int max) =>
		string.IsNullOrEmpty(text) || text.Length <= max ? text : text[..max];
}

/// <summary>Parses "N: value" repair output into index → corrected value (045 B.2).</summary>
public static partial class AiCvImportRepairResponseParser
{
	[GeneratedRegex(@"^\s*(\d+)\s*[:).\-]\s*(.+?)\s*$", RegexOptions.CultureInvariant)]
	private static partial Regex LineRegex();

	public static IReadOnlyDictionary<int, string> Parse(string rawModelOutput)
	{
		var map = new Dictionary<int, string>();
		if (string.IsNullOrWhiteSpace(rawModelOutput))
		{
			return map;
		}

		foreach (var line in rawModelOutput.Split('\n'))
		{
			var match = LineRegex().Match(line);
			if (!match.Success)
			{
				continue;
			}

			if (int.TryParse(match.Groups[1].Value, out var index) && index >= 1)
			{
				map[index] = match.Groups[2].Value.Trim();
			}
		}

		return map;
	}
}
