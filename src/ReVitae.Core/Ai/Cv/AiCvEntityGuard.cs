using System.Text.RegularExpressions;

namespace ReVitae.Core.Ai.Cv;

/// <summary>
/// Deterministic anti-hallucination post-check for rewrite tasks (045 C.3). Compares model
/// output against the source text and reports concrete entities — numbers, years,
/// percentages, currency amounts, emails, and multi-word capitalized names — that appear in
/// the output but not in the source. No model call; pure heuristics, non-blocking warning.
/// </summary>
public static partial class AiCvEntityGuard
{
	[GeneratedRegex(@"\d+(?:[.,]\d+)*\s?%?", RegexOptions.CultureInvariant)]
	private static partial Regex NumberRegex();

	[GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant)]
	private static partial Regex EmailRegex();

	[GeneratedRegex(@"[A-Z][\p{L}&]+(?:\s+[A-Z][\p{L}&]+)+", RegexOptions.CultureInvariant)]
	private static partial Regex ProperNameRegex();

	public static AiCvEntityGuardResult Inspect(string sourceText, string modelOutput)
	{
		var source = sourceText ?? string.Empty;
		var output = modelOutput ?? string.Empty;
		if (string.IsNullOrWhiteSpace(output))
		{
			return AiCvEntityGuardResult.Clean;
		}

		var sourceDigits = ExtractDigitRuns(source);
		var sourceLower = source.ToLowerInvariant();

		var unsupported = new List<string>();
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (Match m in NumberRegex().Matches(output))
		{
			var token = m.Value.Trim();
			var digits = new string(token.Where(char.IsDigit).ToArray());
			if (digits.Length < 2)
			{
				continue; // single digits are too noisy to flag
			}

			if (!sourceDigits.Contains(digits) && seen.Add(token))
			{
				unsupported.Add(token);
			}
		}

		foreach (Match m in EmailRegex().Matches(output))
		{
			var token = m.Value.Trim();
			if (!sourceLower.Contains(token.ToLowerInvariant(), StringComparison.Ordinal) && seen.Add(token))
			{
				unsupported.Add(token);
			}
		}

		foreach (Match m in ProperNameRegex().Matches(output))
		{
			var token = m.Value.Trim();
			if (sourceLower.Contains(token.ToLowerInvariant(), StringComparison.Ordinal))
			{
				continue;
			}

			if (seen.Add(token))
			{
				unsupported.Add(token);
			}
		}

		if (unsupported.Count == 0)
		{
			return AiCvEntityGuardResult.Clean;
		}

		// Cap surfaced tokens so the warning stays readable.
		const int maxTokens = 6;
		var capped = unsupported.Count > maxTokens
			? unsupported.Take(maxTokens).ToList()
			: unsupported;
		return new AiCvEntityGuardResult(true, capped);
	}

	private static HashSet<string> ExtractDigitRuns(string text)
	{
		var runs = new HashSet<string>(StringComparer.Ordinal);
		foreach (Match m in NumberRegex().Matches(text))
		{
			var digits = new string(m.Value.Where(char.IsDigit).ToArray());
			if (digits.Length > 0)
			{
				runs.Add(digits);
			}
		}

		return runs;
	}
}

public sealed record AiCvEntityGuardResult(
	bool HasUnsupportedEntities,
	IReadOnlyList<string> UnsupportedEntities)
{
	public static AiCvEntityGuardResult Clean { get; } = new(false, []);
}
