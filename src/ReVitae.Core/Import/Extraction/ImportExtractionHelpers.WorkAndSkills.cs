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
	internal static IEnumerable<string> SplitWorkExperienceBlocks(string body)
	{
		var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var blocks = new List<List<string>>();
		List<string>? current = null;

		for (var index = 0; index < lines.Length; index++)
		{
			var line = lines[index];
			var startsEntry = StartsWorkExperienceEntry(lines, index);

			if (startsEntry)
			{
				if (current is { Count: > 0 })
				{
					blocks.Add(current);
				}

				current = [line];
				continue;
			}

			current ??= [];
			current.Add(line);
		}

		if (current is { Count: > 0 })
		{
			blocks.Add(current);
		}

		return blocks
			.Where(block => block.Count > 0)
			.Select(block => string.Join('\n', block));
	}

	internal static bool StartsWorkExperienceEntry(string[] lines, int index)
	{
		if (LooksLikeReVitaeExportWorkEntry(lines, index))
		{
			return true;
		}

		var line = lines[index];
		if (line.Contains('·'))
		{
			return false;
		}

		return LooksLikeWorkEntryHeader(line)
			&& lines.Skip(index + 1).Take(3).Any(candidate => DateRangeParser.TryParse(candidate, out _));
	}

	internal static bool LooksLikeReVitaeExportWorkEntry(string[] lines, int index)
	{
		if (index + 1 >= lines.Length)
		{
			return false;
		}

		var titleLine = lines[index];
		var metaLine = lines[index + 1];
		if (string.IsNullOrWhiteSpace(titleLine)
			|| titleLine.Contains('·')
			|| DateRangeParser.TryParse(titleLine, out _))
		{
			return false;
		}

		return metaLine.Contains('·')
			&& (DateRangeParser.TryParseTrailingDateRange(metaLine, out _, out _)
				|| metaLine.Contains("Full-time", StringComparison.OrdinalIgnoreCase)
				|| metaLine.Contains("Part-time", StringComparison.OrdinalIgnoreCase)
				|| metaLine.Contains(" s.r.o.", StringComparison.OrdinalIgnoreCase)
				|| metaLine.Contains(" a.s.", StringComparison.OrdinalIgnoreCase));
	}

	internal static bool TryParseExportDelimitedMetaLine(
		string line,
		Queue<string> orphanWorkDateFragments,
		out IReadOnlyList<string> parts,
		out ParsedDateRange dateRange)
	{
		parts = [];
		dateRange = new ParsedDateRange(null, null, null, null, false);

		var candidate = line;
		if (!DateRangeParser.TryParseTrailingDateRange(candidate, out dateRange, out var prefix)
			&& TryCompletePartialWorkMetaLine(line, orphanWorkDateFragments, out candidate)
			&& !DateRangeParser.TryParseTrailingDateRange(candidate, out dateRange, out prefix))
		{
			return false;
		}

		if (!DateRangeParser.TryParseTrailingDateRange(candidate, out dateRange, out prefix))
		{
			return false;
		}

		parts = prefix.Split('·', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		return parts.Count > 0;
	}

	internal static bool LooksLikeWorkEntryHeader(string line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return false;
		}

		return line.Contains(" - ", StringComparison.Ordinal)
			|| line.Contains(" at ", StringComparison.OrdinalIgnoreCase)
			|| line.Contains(" s.r.o.", StringComparison.OrdinalIgnoreCase)
			|| line.Contains(" a.s.", StringComparison.OrdinalIgnoreCase);
	}

	internal static bool IsLikelyNameToken(string line)
	{
		if (string.IsNullOrWhiteSpace(line)
			|| CvImportPatterns.Email.IsMatch(line)
			|| CvImportPatterns.Url.IsMatch(line)
			|| CvImportPatterns.Phone.IsMatch(line))
		{
			return false;
		}

		return line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length == 1
			&& IsLikelyNamePart(line);
	}

	internal static bool HeaderLooksLikeSummaryProse(IReadOnlyList<string> headerLines)
	{
		if (headerLines.Count == 0)
		{
			return false;
		}

		var joined = string.Join(' ', headerLines);
		return headerLines[0].Length > 60
			|| joined.Contains("years of experience", StringComparison.OrdinalIgnoreCase)
			|| joined.Contains("Strong in", StringComparison.OrdinalIgnoreCase)
			|| joined.Contains("Experienced in", StringComparison.OrdinalIgnoreCase);
	}

	internal static string TrimReVitaeSkillBodyPrefix(string body)
	{
		var lines = body.Split('\n', StringSplitOptions.TrimEntries);
		var reVitaeSkillFormat = lines.Any(line => TryParseSkillPreviewLine(line, out _));
		var startIndex = 0;
		for (var index = 0; index < lines.Length; index++)
		{
			var line = lines[index];
			if (TryParseSkillPreviewLine(line, out _)
				|| line.Equals("General", StringComparison.OrdinalIgnoreCase)
				|| IsReVitaeExportSkillCategory(line, reVitaeSkillFormat, lines, index))
			{
				startIndex = index;
				break;
			}
		}

		return startIndex > 0
			? string.Join('\n', lines.Skip(startIndex))
			: body;
	}

	internal static string MergeSplitReVitaeSkillLines(string body)
	{
		var lines = body.Split('\n', StringSplitOptions.TrimEntries);
		if (lines.Length == 0)
		{
			return body;
		}

		var proficiencyQueue = new Queue<string>();
		var filtered = new List<string>();
		foreach (var line in lines)
		{
			if (TryDequeueStandaloneSkillProficiency(line, out var proficiency))
			{
				proficiencyQueue.Enqueue(proficiency);
				continue;
			}

			filtered.Add(line);
		}

		var merged = new List<string>();
		foreach (var line in filtered)
		{
			var normalized = line;
			if (normalized.Contains('·', StringComparison.Ordinal))
			{
				var trimmed = normalized.TrimEnd();
				if (trimmed.EndsWith('·') && proficiencyQueue.Count > 0)
				{
					normalized = trimmed + " " + proficiencyQueue.Dequeue();
				}
			}
			else if (IsBareReVitaeSkillNameLine(normalized) && proficiencyQueue.Count > 0)
			{
				normalized = normalized + " · " + proficiencyQueue.Dequeue();
			}

			merged.Add(normalized);
		}

		return string.Join('\n', merged);
	}

	internal static bool TryDequeueStandaloneSkillProficiency(string line, out string proficiency)
	{
		proficiency = string.Empty;
		var token = line.Trim().TrimStart('·').Trim();
		if (string.IsNullOrWhiteSpace(token))
		{
			return false;
		}

		if (token.Equals("Intermediate", StringComparison.OrdinalIgnoreCase)
			|| token.Equals("Advanced", StringComparison.OrdinalIgnoreCase)
			|| token.Equals("Beginner", StringComparison.OrdinalIgnoreCase)
			|| token.Equals("Expert", StringComparison.OrdinalIgnoreCase)
			|| token.Equals("Fluent", StringComparison.OrdinalIgnoreCase)
			|| Regex.IsMatch(token, @"^\d+\s+years?$", RegexOptions.IgnoreCase))
		{
			proficiency = token;
			return true;
		}

		return false;
	}

	internal static bool IsBareReVitaeSkillNameLine(string line)
	{
		if (string.IsNullOrWhiteSpace(line)
			|| line.Contains('·', StringComparison.Ordinal)
			|| line.Contains(':', StringComparison.Ordinal)
			|| IsExportSubheadingLine(line)
			|| IsLikelyPersonNameLine(line)
			|| IsKnownSkillCategoryLabel(line)
			|| IsPlausibleSkillCategory(line))
		{
			return false;
		}

		return IsPlausibleSkillName(line);
	}

	internal static Queue<string> CollectOrphanWorkDateFragments(string headerBlock)
	{
		var queue = new Queue<string>();
		if (string.IsNullOrWhiteSpace(headerBlock))
		{
			return queue;
		}

		foreach (var line in headerBlock.Split('\n', StringSplitOptions.TrimEntries))
		{
			if (OrphanWorkDateFragment.IsMatch(line))
			{
				queue.Enqueue(line);
			}
		}

		return queue;
	}

	internal static bool TryCompletePartialWorkMetaLine(
		string metaLine,
		Queue<string> orphanWorkDateFragments,
		out string completedLine)
	{
		completedLine = metaLine;
		if (!metaLine.Contains('·', StringComparison.Ordinal) || orphanWorkDateFragments.Count == 0)
		{
			return false;
		}

		var lastSeparator = metaLine.LastIndexOf('·');
		var trailing = metaLine[(lastSeparator + 1)..].Trim();
		if (!int.TryParse(trailing, out var startMonth) || startMonth is < 1 or > 12)
		{
			return false;
		}

		var fragment = orphanWorkDateFragments.Dequeue();
		var match = OrphanWorkDateFragment.Match(fragment);
		if (!match.Success)
		{
			return false;
		}

		completedLine = metaLine[..lastSeparator].TrimEnd()
			+ " · "
			+ $"{startMonth:D2} / {match.Groups["startYear"].Value} – {match.Groups["endMonth"].Value} / {match.Groups["endYear"].Value}";
		return true;
	}

	internal static readonly Regex OrphanWorkDateFragment = new(
		@"^/\s*(?<startYear>\d{4})\s*[–-]\s*(?<endMonth>\d{1,2})\s*/\s*(?<endYear>\d{4})\s*$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	internal static bool IsLikelyNamePart(string token)
	{
		token = token.Trim();
		if (token.Length is < 2 or > 32)
		{
			return false;
		}

		if (!Regex.IsMatch(token, @"^[\p{L}][\p{L}\p{M}'-]*$"))
		{
			return false;
		}

		if (token.Length <= 3 && token.All(static c => !char.IsUpper(c)))
		{
			return false;
		}

		return !IsLikelyTechNameToken(token);
	}

	internal static bool IsLikelyTechNameToken(string token) =>
		TechNameTokens.Contains(token);

	internal static IEnumerable<string> SplitCommaList(string value)
	{
		return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	internal static void SplitTitleCompany(string line, out string jobTitle, out string company)
	{
		var atIndex = line.IndexOf(" at ", StringComparison.OrdinalIgnoreCase);
		if (atIndex > 0)
		{
			jobTitle = line[..atIndex].Trim();
			company = line[(atIndex + 4)..].Trim();
			return;
		}

		var dashIndex = line.IndexOf(" - ", StringComparison.Ordinal);
		if (dashIndex > 0)
		{
			company = line[..dashIndex].Trim();
			jobTitle = line[(dashIndex + 3)..].Trim();
			return;
		}

		var pipeIndex = line.IndexOf('|');
		if (pipeIndex > 0)
		{
			jobTitle = line[..pipeIndex].Trim();
			company = line[(pipeIndex + 1)..].Trim();
			return;
		}

		jobTitle = line.Trim();
		company = string.Empty;
	}

}
