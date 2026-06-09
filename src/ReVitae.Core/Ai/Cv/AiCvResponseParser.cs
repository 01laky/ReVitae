using ReVitae.Core.Cv;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Cv;

public sealed class AiCvResponseParseException : Exception
{
	public AiCvResponseParseException(string errorMessageKey)
		: base(errorMessageKey)
	{
		ErrorMessageKey = errorMessageKey;
	}

	public string ErrorMessageKey { get; }
}

public static class AiCvResponseParser
{
	public static string Parse(string rawModelOutput, AiCvTaskKind task)
	{
		var text = StripMarkdownFences(rawModelOutput).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new AiCvResponseParseException(TranslationKeys.AiCvEmptyResponse);
		}

		var maxLength = ResolveMaxLength(task);
		if (text.Length > maxLength)
		{
			throw new AiCvResponseParseException(TranslationKeys.AiCvResponseTooLong);
		}

		return text;
	}

	internal static int ResolveMaxLength(AiCvTaskKind task) =>
		task switch
		{
			AiCvTaskKind.ImproveProfessionalSummary or AiCvTaskKind.DraftProfessionalSummary => 800,
			AiCvTaskKind.ShortenProfessionalSummary => 600,
			AiCvTaskKind.ImproveWorkDescription or AiCvTaskKind.DraftWorkDescription => WorkExperienceSchema.DescriptionMaxLength,
			AiCvTaskKind.ImproveProjectDescription => ProjectsSchema.DescriptionMaxLength,
			_ => 2000,
		};

	/// <summary>
	/// Parses advisor / advice-list output (045 A.3 / C.5) into up to four items. Accepts
	/// "- ", "* ", or "1." style line prefixes; splits an optional rationale after " — "
	/// (em dash) or " - " (hyphen). Throws when no items can be parsed.
	/// </summary>
	public static IReadOnlyList<AiCvParsedAdvice> ParseAdviceList(string rawModelOutput)
	{
		const int maxItems = 4;
		var text = StripMarkdownFences(rawModelOutput ?? string.Empty);
		var items = new List<AiCvParsedAdvice>();

		foreach (var rawLine in text.Split('\n'))
		{
			var line = StripBullet(rawLine.Trim());
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			var (advice, rationale) = SplitRationale(line);
			if (string.IsNullOrWhiteSpace(advice))
			{
				continue;
			}

			items.Add(new AiCvParsedAdvice(advice.Trim(), rationale));
			if (items.Count >= maxItems)
			{
				break;
			}
		}

		if (items.Count == 0)
		{
			throw new AiCvResponseParseException(TranslationKeys.AiCvEmptyResponse);
		}

		return items;
	}

	internal static string StripBullet(string line)
	{
		if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
		{
			return line[2..].Trim();
		}

		// Numbered prefixes like "1. " / "2) ".
		var idx = 0;
		while (idx < line.Length && char.IsDigit(line[idx]))
		{
			idx++;
		}

		if (idx > 0 && idx < line.Length && (line[idx] == '.' || line[idx] == ')'))
		{
			return line[(idx + 1)..].Trim();
		}

		return line;
	}

	internal static (string Advice, string? Rationale) SplitRationale(string line)
	{
		var emDash = line.IndexOf(" — ", StringComparison.Ordinal);
		if (emDash >= 0)
		{
			return (line[..emDash], NullIfEmpty(line[(emDash + 3)..]));
		}

		var hyphen = line.IndexOf(" - ", StringComparison.Ordinal);
		if (hyphen >= 0)
		{
			return (line[..hyphen], NullIfEmpty(line[(hyphen + 3)..]));
		}

		return (line, null);
	}

	private static string? NullIfEmpty(string value) =>
		string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	internal static string StripMarkdownFences(string raw)
	{
		var trimmed = raw.Trim();
		if (!trimmed.StartsWith("```", StringComparison.Ordinal))
		{
			return trimmed;
		}

		var firstNewline = trimmed.IndexOf('\n');
		if (firstNewline < 0)
		{
			return trimmed.Trim('`', ' ');
		}

		var body = trimmed[(firstNewline + 1)..];
		var closingIndex = body.LastIndexOf("```", StringComparison.Ordinal);
		if (closingIndex >= 0)
		{
			body = body[..closingIndex];
		}

		return body.Trim();
	}
}
