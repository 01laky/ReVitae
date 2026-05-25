using System.Text;
using System.Text.RegularExpressions;

namespace ReVitae.Core.Import;

public static class CvTextNormalizer
{
	private static readonly Regex RepeatedBlankLines = new("\n{3,}", RegexOptions.Compiled);

	public static string Normalize(string? rawText)
	{
		if (string.IsNullOrWhiteSpace(rawText))
		{
			return string.Empty;
		}

		var normalized = rawText.Replace("\r\n", "\n", StringComparison.Ordinal);
		normalized = normalized.Replace('\r', '\n');
		normalized = normalized.Replace('\u2013', '-');
		normalized = normalized.Replace('\u2014', '-');
		normalized = normalized.Replace('\u2212', '-');

		var lines = normalized.Split('\n');
		for (var index = 0; index < lines.Length; index++)
		{
			lines[index] = CollapseInlineSpaces(lines[index].TrimEnd());
			lines[index] = NormalizeBulletPrefix(lines[index]);
		}

		normalized = string.Join('\n', lines);
		normalized = RepeatedBlankLines.Replace(normalized, "\n\n");
		return normalized.Trim();
	}

	private static string CollapseInlineSpaces(string line)
	{
		if (string.IsNullOrEmpty(line))
		{
			return line;
		}

		var builder = new StringBuilder(line.Length);
		var previousWasSpace = false;
		foreach (var character in line)
		{
			if (char.IsWhiteSpace(character))
			{
				if (!previousWasSpace)
				{
					builder.Append(' ');
					previousWasSpace = true;
				}
			}
			else
			{
				builder.Append(character);
				previousWasSpace = false;
			}
		}

		return builder.ToString().Trim();
	}

	private static string NormalizeBulletPrefix(string line)
	{
		if (line.StartsWith("• ", StringComparison.Ordinal)
			|| line.StartsWith("◦ ", StringComparison.Ordinal)
			|| line.StartsWith("▪ ", StringComparison.Ordinal)
			|| line.StartsWith("* ", StringComparison.Ordinal)
			|| line.StartsWith("- ", StringComparison.Ordinal))
		{
			return "- " + line[2..].TrimStart();
		}

		return line;
	}
}
