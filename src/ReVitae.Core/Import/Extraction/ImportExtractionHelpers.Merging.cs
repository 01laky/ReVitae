using System.Text.RegularExpressions;
using ReVitae.Core.Cv;
using ReVitae.Core.Cv.AdditionalInformation;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import.Patterns;
using ReVitae.Core.Import.Pdf;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Extraction;

internal static partial class ImportFieldExtractionCore
{
	internal static IEnumerable<string> SplitBlocks(string body)
	{
		return body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	internal static IEnumerable<string> SplitCertificateBlocks(string body)
	{
		var lineBlocks = SplitLineBasedEntryBlocks(body, StartsCertificateEntryLine).ToList();
		return lineBlocks.Count > 1 ? lineBlocks : SplitBlocks(body);
	}

	internal static IEnumerable<string> SplitProjectBlocks(string body)
	{
		var lineBlocks = SplitLineBasedEntryBlocks(body, StartsProjectEntryLine).ToList();
		return lineBlocks.Count > 1 ? lineBlocks : SplitBlocks(body);
	}

	internal static bool StartsCertificateEntryLine(string[] lines, int index)
	{
		var line = lines[index];
		return index == 0
			|| line.StartsWith("Professional Certification #", StringComparison.OrdinalIgnoreCase)
			|| CertificateEntryHeader.IsMatch(line);
	}

	internal static bool StartsProjectEntryLine(string[] lines, int index)
	{
		var line = lines[index];
		return index == 0
			|| ProjectEntryHeader.IsMatch(line);
	}

	internal static IEnumerable<string> SplitLineBasedEntryBlocks(
		string body,
		Func<string[], int, bool> startsEntry)
	{
		var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var blocks = new List<List<string>>();
		List<string>? current = null;

		for (var index = 0; index < lines.Length; index++)
		{
			if (startsEntry(lines, index))
			{
				if (current is { Count: > 0 })
				{
					blocks.Add(current);
				}

				current = [lines[index]];
				continue;
			}

			current ??= [];
			current.Add(lines[index]);
		}

		if (current is { Count: > 0 })
		{
			blocks.Add(current);
		}

		return blocks
			.Where(block => block.Count > 0)
			.Select(block => string.Join('\n', block));
	}

	internal static string MergeSplitExportMetaLines(string body)
	{
		var lines = body.Split('\n', StringSplitOptions.TrimEntries);
		if (lines.Length == 0)
		{
			return body;
		}

		var merged = new List<string>();
		for (var index = 0; index < lines.Length; index++)
		{
			var line = lines[index];
			while (index + 1 < lines.Length && ShouldMergeExportMetaContinuation(line, lines[index + 1]))
			{
				line = line.TrimEnd() + " " + lines[++index].TrimStart();
			}

			merged.Add(line);
		}

		return string.Join('\n', merged);
	}

	internal static bool ShouldMergeExportMetaContinuation(string current, string next)
	{
		if (string.IsNullOrWhiteSpace(next))
		{
			return false;
		}

		var trimmedNext = next.Trim();
		if (current.Contains('·', StringComparison.Ordinal)
			&& !DateRangeParser.TryParseTrailingDateRange(current, out var trailingRange, out _)
			&& (DateRangeParser.TryParse(trimmedNext, out _)
				|| DateRangeParser.TryParseTrailingDateRange(trimmedNext, out _, out _)))
		{
			return true;
		}

		if (current.TrimEnd().EndsWith('·')
			&& (trimmedNext.Contains('/', StringComparison.Ordinal) || DateRangeParser.TryParse(trimmedNext, out _)))
		{
			return true;
		}

		if (current.Contains('/', StringComparison.Ordinal)
			&& (!DateRangeParser.TryParse(current, out var parsedRange) || parsedRange.EndYear is null)
			&& Regex.IsMatch(trimmedNext, @"^\d{4}\b"))
		{
			return true;
		}

		return false;
	}

	internal static string MergeSplitUrlLines(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return text;
		}

		var lines = text.Split('\n');
		var merged = new List<string>();
		for (var index = 0; index < lines.Length; index++)
		{
			var line = lines[index];
			while (index + 1 < lines.Length && ShouldMergeUrlContinuation(line, lines[index + 1]))
			{
				line += lines[++index].TrimStart();
			}

			merged.Add(line);
		}

		return string.Join('\n', merged);
	}

	internal static string MergeSplitPersonalFieldLines(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return text;
		}

		var lines = text.Split('\n');
		var merged = new List<string>();
		for (var index = 0; index < lines.Length; index++)
		{
			var line = lines[index];
			while (index + 1 < lines.Length && ShouldMergeLabeledFieldContinuation(line, lines[index + 1]))
			{
				line += " " + lines[++index].TrimStart();
			}

			merged.Add(line);
		}

		return string.Join('\n', merged);
	}

	internal static bool ShouldMergeLabeledFieldContinuation(string current, string next)
	{
		var trimmedNext = next.Trim();
		if (string.IsNullOrWhiteSpace(trimmedNext))
		{
			return false;
		}

		if (!CvImportPatterns.LabeledValue.IsMatch(current))
		{
			return false;
		}

		if (CvImportPatterns.LabeledValue.IsMatch(trimmedNext))
		{
			return false;
		}

		return !trimmedNext.Contains(':', StringComparison.Ordinal);
	}

	internal static bool ShouldMergeUrlContinuation(string current, string next)
	{
		var trimmedNext = next.Trim();
		if (string.IsNullOrWhiteSpace(trimmedNext))
		{
			return false;
		}

		if (ContainsPartialUrl(current))
		{
			if (trimmedNext.Contains(':', StringComparison.Ordinal) && !trimmedNext.Contains("://", StringComparison.Ordinal))
			{
				return false;
			}

			return trimmedNext.All(static character => !char.IsWhiteSpace(character));
		}

		if (current.Contains("URL:", StringComparison.OrdinalIgnoreCase)
			&& trimmedNext.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (IsReVitaeUrlLabelLine(current)
			&& !trimmedNext.Contains(':', StringComparison.Ordinal))
		{
			return trimmedNext.All(static character => !char.IsWhiteSpace(character));
		}

		return false;
	}

	internal static bool IsReVitaeUrlLabelLine(string line) =>
		line.Contains("LinkedIn URL:", StringComparison.OrdinalIgnoreCase)
		|| line.Contains("GitHub URL:", StringComparison.OrdinalIgnoreCase)
		|| line.Contains("Portfolio URL:", StringComparison.OrdinalIgnoreCase);

	internal static string[] CollectSegmentedPersonalLines(CvSegmentationResult segmentation)
	{
		var builder = new List<string>();
		if (!string.IsNullOrWhiteSpace(segmentation.HeaderBlock))
		{
			builder.Add(segmentation.HeaderBlock);
		}

		foreach (var body in segmentation.SectionBodies.Values)
		{
			if (!string.IsNullOrWhiteSpace(body))
			{
				builder.Add(body);
			}
		}

		return string.Join('\n', builder)
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	internal static bool TryAssignReVitaeSidebarNameBeforeEmail(
		IReadOnlyList<string> lines,
		PersonalInformationImport personal,
		ImportSectionExtractionContext context)
	{
		for (var index = 0; index < lines.Count; index++)
		{
			if (!CvImportPatterns.Email.IsMatch(lines[index]))
			{
				continue;
			}

			var nameParts = new List<string>();
			for (var scan = index - 1; scan >= 0 && nameParts.Count < 2; scan--)
			{
				var line = lines[scan].Trim();
				if (string.IsNullOrWhiteSpace(line)
					|| line.Contains(':', StringComparison.Ordinal)
					|| IsPersonalContactLabel(line))
				{
					continue;
				}

				if (!IsLikelyNamePart(line))
				{
					continue;
				}

				nameParts.Insert(0, line);
			}

			if (nameParts.Count < 2)
			{
				continue;
			}

			personal.FirstName = nameParts[^2];
			personal.LastName = nameParts[^1];
			context.AddConfidence(MainPersonalInformationFieldKeys.FirstName, CvImportConfidence.High);
			context.AddConfidence(MainPersonalInformationFieldKeys.LastName, CvImportConfidence.High);
			return true;
		}

		return false;
	}

	internal static void ApplyReVitaeExportHyperlinks(
		PersonalInformationImport personal,
		IReadOnlyList<string>? hyperlinkUrls,
		HashSet<string> assignedUrls,
		ImportSectionExtractionContext context)
	{
		if (hyperlinkUrls is not { Count: > 0 })
		{
			return;
		}

		foreach (var url in hyperlinkUrls)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				continue;
			}

			var normalized = NormalizeImportedUrl(url.Trim());
			if (normalized.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase)
				&& string.IsNullOrWhiteSpace(personal.LinkedInUrl))
			{
				personal.LinkedInUrl = normalized;
				assignedUrls.Add(normalized);
				context.AddConfidence(MainPersonalInformationFieldKeys.LinkedInUrl, CvImportConfidence.High);
			}
			else if (normalized.Contains("github.com", StringComparison.OrdinalIgnoreCase)
					 && string.IsNullOrWhiteSpace(personal.GitHubUrl))
			{
				personal.GitHubUrl = normalized;
				assignedUrls.Add(normalized);
				context.AddConfidence(MainPersonalInformationFieldKeys.GitHubUrl, CvImportConfidence.High);
			}
			else if (string.IsNullOrWhiteSpace(personal.PortfolioUrl)
					 && !assignedUrls.Contains(normalized)
					 && !normalized.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase)
					 && !normalized.Contains("github.com", StringComparison.OrdinalIgnoreCase))
			{
				personal.PortfolioUrl = normalized;
				assignedUrls.Add(normalized);
				context.AddConfidence(MainPersonalInformationFieldKeys.PortfolioUrl, CvImportConfidence.Medium);
			}
		}
	}

	internal static bool ContainsPartialUrl(string line)
	{
		if (!line.Contains("http://", StringComparison.OrdinalIgnoreCase)
			&& !line.Contains("https://", StringComparison.OrdinalIgnoreCase)
			&& !line.Contains("www.", StringComparison.OrdinalIgnoreCase)
			&& !line.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase)
			&& !line.Contains("github.com", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (CvImportPatterns.Url.IsMatch(line) && !line.TrimEnd().EndsWith('-'))
		{
			return false;
		}

		return true;
	}

	internal static string CollectSupplementalPersonalContactText(CvSegmentationResult segmentation)
	{
		var collected = new List<string>();
		foreach (var body in segmentation.SectionBodies.Values)
		{
			CollectLabeledPersonalContactLines(body, collected);
		}

		if (segmentation.SectionBodies.TryGetValue(CvImportSectionId.AdditionalInformation, out var additional))
		{
			collected.AddRange(ExtractInlineLabeledBlock(additional, "Digital"));
		}

		return string.Join('\n', collected.Distinct(StringComparer.OrdinalIgnoreCase));
	}

	internal static void CollectLabeledPersonalContactLines(string body, ICollection<string> collected)
	{
		foreach (var line in body.Split('\n', StringSplitOptions.TrimEntries))
		{
			var labeled = CvImportPatterns.LabeledValue.Match(line);
			if (!labeled.Success || !IsPersonalContactLabel(labeled.Groups["label"].Value))
			{
				continue;
			}

			collected.Add(line);
		}
	}

	internal static IEnumerable<string> ExtractInlineLabeledBlock(string body, string header)
	{
		var lines = body.Split('\n', StringSplitOptions.TrimEntries);
		var capturing = false;
		foreach (var line in lines)
		{
			if (line.Equals(header, StringComparison.OrdinalIgnoreCase))
			{
				capturing = true;
				continue;
			}

			if (!capturing)
			{
				continue;
			}

			if (!line.Contains(':', StringComparison.Ordinal))
			{
				if (line.Length > 0)
				{
					break;
				}

				continue;
			}

			yield return line;
		}
	}

	internal static bool IsPersonalContactLabel(string label) =>
		label.Contains("location", StringComparison.OrdinalIgnoreCase)
		|| label.Contains("lokalita", StringComparison.OrdinalIgnoreCase)
		|| label.Contains("linkedin", StringComparison.OrdinalIgnoreCase)
		|| label.Contains("github", StringComparison.OrdinalIgnoreCase)
		|| label.Contains("portfolio", StringComparison.OrdinalIgnoreCase)
		|| label.Contains("website", StringComparison.OrdinalIgnoreCase)
		|| label.Contains("professional title", StringComparison.OrdinalIgnoreCase);

	internal static string NormalizeImportedUrl(string value)
	{
		var trimmed = value.Trim();
		var match = CvImportPatterns.Url.Match(trimmed);
		return match.Success ? match.Value.Trim() : trimmed;
	}

	internal static bool TryParseLabeledIssueDateLine(string line, out ParsedDateRange issueDate)
	{
		issueDate = new ParsedDateRange(null, null, null, null, false);
		var labeled = CvImportPatterns.LabeledValue.Match(line);
		if (!labeled.Success)
		{
			return false;
		}

		var label = labeled.Groups["label"].Value;
		if (!label.Contains("issued", StringComparison.OrdinalIgnoreCase)
			&& !label.Contains("issue date", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return DateRangeParser.TryParse(labeled.Groups["value"].Value.Trim(), out issueDate);
	}

	internal static bool TryParseLabeledExpirationDateLine(string line, out ParsedDateRange expirationDate)
	{
		expirationDate = new ParsedDateRange(null, null, null, null, false);
		var labeled = CvImportPatterns.LabeledValue.Match(line);
		if (!labeled.Success)
		{
			return false;
		}

		var label = labeled.Groups["label"].Value;
		if (!label.Contains("valid through", StringComparison.OrdinalIgnoreCase)
			&& !label.Contains("valid until", StringComparison.OrdinalIgnoreCase)
			&& !label.Contains("expires", StringComparison.OrdinalIgnoreCase)
			&& !label.Contains("expiration", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return DateRangeParser.TryParse(labeled.Groups["value"].Value.Trim(), out expirationDate);
	}

	internal static bool TryParseLabeledDateRangeLine(string line, out ParsedDateRange dateRange)
	{
		dateRange = new ParsedDateRange(null, null, null, null, false);
		var labeled = CvImportPatterns.LabeledValue.Match(line);
		if (!labeled.Success
			|| !labeled.Groups["label"].Value.Contains("date range", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return DateRangeParser.TryParse(labeled.Groups["value"].Value.Trim(), out dateRange);
	}

	internal static readonly Regex CertificateEntryHeader = new(
		@"^Professional Certification\s+#\d+",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	internal static readonly Regex ProjectEntryHeader = new(
		@"^Project\s+.+\s+[—-]\s+",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

}
