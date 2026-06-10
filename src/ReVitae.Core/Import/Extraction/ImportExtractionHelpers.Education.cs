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
	internal static IEnumerable<string> SplitEducationBlocks(string body)
	{
		body = MergeSplitExportMetaLines(body);
		var lines = body.Split('\n', StringSplitOptions.TrimEntries);
		if (!EducationBodyLooksInstitutionFirst(body) && EducationBodyLooksDegreeFirst(lines))
		{
			var lineBlocks = SplitLineBasedEntryBlocks(body, StartsReVitaeEducationEntryLine).ToList();
			if (lineBlocks.Count > 1)
			{
				return MergeEducationContinuationBlocks(lineBlocks);
			}
		}

		var rawBlocks = body
			.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.ToList();

		if (rawBlocks.Count <= 1)
		{
			return rawBlocks;
		}

		var merged = new List<string> { rawBlocks[0] };
		for (var index = 1; index < rawBlocks.Count; index++)
		{
			if (LooksLikeEducationContinuationBlock(rawBlocks[index], merged[^1]))
			{
				merged[^1] = merged[^1] + '\n' + rawBlocks[index];
			}
			else
			{
				merged.Add(rawBlocks[index]);
			}
		}

		return merged;
	}

	internal static IReadOnlyList<string> MergeEducationContinuationBlocks(IReadOnlyList<string> blocks)
	{
		if (blocks.Count <= 1)
		{
			return blocks;
		}

		var merged = new List<string> { blocks[0] };
		for (var index = 1; index < blocks.Count; index++)
		{
			if (LooksLikeEducationContinuationBlock(blocks[index], merged[^1]))
			{
				merged[^1] = merged[^1] + '\n' + blocks[index];
			}
			else
			{
				merged.Add(blocks[index]);
			}
		}

		return merged;
	}

	internal static bool StartsReVitaeEducationEntryLine(string[] lines, int index) =>
		index == 0 || LooksLikeEducationDegreeTitleLine(lines[index]);

	internal static bool LooksLikeInstitutionFirstEducationHeader(IReadOnlyList<string> headerLines) =>
		headerLines.Count >= 2
		&& LooksLikeInstitutionName(headerLines[0])
		&& (LooksLikeEducationDegreeTitleLine(headerLines[1]) || LooksLikeExplicitDegreeLine(headerLines[1]));

	internal static bool EducationBodyLooksInstitutionFirst(string body)
	{
		var rawBlocks = body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (rawBlocks.Length < 2)
		{
			return false;
		}

		var institutionFirstBlocks = 0;
		foreach (var block in rawBlocks)
		{
			var blockLines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (blockLines.Length >= 2
				&& LooksLikeInstitutionName(blockLines[0])
				&& (LooksLikeEducationDegreeTitleLine(blockLines[1]) || LooksLikeExplicitDegreeLine(blockLines[1])))
			{
				institutionFirstBlocks++;
			}
		}

		return institutionFirstBlocks >= Math.Max(2, rawBlocks.Length / 2);
	}

	internal static bool EducationBodyLooksDegreeFirst(string[] lines)
	{
		foreach (var line in lines)
		{
			if (LooksLikeEducationDegreeTitleLine(line))
			{
				return true;
			}

			if (DateRangeParser.TryParse(line, out _))
			{
				return false;
			}
		}

		return false;
	}

	internal static bool LooksLikeEducationDegreeTitleLine(string line)
	{
		if (string.IsNullOrWhiteSpace(line)
			|| line.Contains('·', StringComparison.Ordinal)
			|| line.Contains("Field of study:", StringComparison.OrdinalIgnoreCase)
			|| line.StartsWith("Grade:", StringComparison.OrdinalIgnoreCase)
			|| line.StartsWith("Institution URL:", StringComparison.OrdinalIgnoreCase)
			|| line.StartsWith("Thesis focus", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return EducationDegreeTitleLine.IsMatch(line.Trim());
	}

	internal static readonly Regex EducationDegreeTitleLine = new(
		@"^(MSc|BSc|PhD|MEng|MBA|Bachelor|Master|Doctor)\b",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	internal static bool LooksLikeEducationContinuationBlock(string block, string previousBlock)
	{
		var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (lines.Length == 0)
		{
			return false;
		}

		var firstLine = lines[0];
		if (DateRangeParser.TryParse(firstLine, out _))
		{
			return false;
		}

		if (LooksLikeExplicitDegreeLine(firstLine))
		{
			return false;
		}

		if (firstLine.StartsWith("and ", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		var lastPreviousLine = GetLastMeaningfulLine(previousBlock);
		if (LooksLikeIncompleteInstitutionLine(lastPreviousLine))
		{
			return true;
		}

		return lines.All(line => !DateRangeParser.TryParse(line, out _) && !LooksLikeLocationLine(line))
			&& lines.All(line => line.Length < 80)
			&& !lines.Any(LooksLikeExplicitDegreeLine)
			&& HasEducationEntryAnchor(previousBlock);
	}

	internal static bool HasEducationEntryAnchor(string block)
	{
		var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		return lines.Any(line => DateRangeParser.TryParse(line, out _))
			|| lines.Any(LooksLikeInstitutionName)
			|| lines.Any(LooksLikeIncompleteInstitutionLine);
	}

	internal static string GetLastMeaningfulLine(string block)
	{
		return block
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.LastOrDefault() ?? string.Empty;
	}

	internal static string InferDefaultDegreeLabel(string institutionOrTitle)
	{
		if (institutionOrTitle.Contains("high school", StringComparison.OrdinalIgnoreCase)
			|| institutionOrTitle.Contains("gymnaz", StringComparison.OrdinalIgnoreCase)
			|| institutionOrTitle.Contains("gymnáz", StringComparison.OrdinalIgnoreCase)
			|| institutionOrTitle.Contains("stredna skola", StringComparison.OrdinalIgnoreCase)
			|| institutionOrTitle.Contains("stredná škola", StringComparison.OrdinalIgnoreCase))
		{
			return "High School";
		}

		if (institutionOrTitle.Contains("university", StringComparison.OrdinalIgnoreCase)
			|| institutionOrTitle.Contains("college", StringComparison.OrdinalIgnoreCase))
		{
			return "Bachelor's degree";
		}

		return institutionOrTitle;
	}

	internal static void InferDegreeType(EducationEntry entry, string institutionOrTitle)
	{
		if (institutionOrTitle.Contains("high school", StringComparison.OrdinalIgnoreCase)
			|| institutionOrTitle.Contains("gymnaz", StringComparison.OrdinalIgnoreCase)
			|| institutionOrTitle.Contains("gymnáz", StringComparison.OrdinalIgnoreCase)
			|| institutionOrTitle.Contains("stredna skola", StringComparison.OrdinalIgnoreCase)
			|| institutionOrTitle.Contains("stredná škola", StringComparison.OrdinalIgnoreCase))
		{
			entry.DegreeType = DegreeType.HighSchool;
			return;
		}

		if (institutionOrTitle.Contains("master", StringComparison.OrdinalIgnoreCase)
			|| institutionOrTitle.Contains("mgr", StringComparison.OrdinalIgnoreCase))
		{
			entry.DegreeType = DegreeType.Master;
			return;
		}

		if (institutionOrTitle.Contains("phd", StringComparison.OrdinalIgnoreCase)
			|| institutionOrTitle.Contains("doctor", StringComparison.OrdinalIgnoreCase))
		{
			entry.DegreeType = DegreeType.Doctorate;
		}
	}

	internal static bool LooksLikeLocationLine(string line)
	{
		if (string.IsNullOrWhiteSpace(line) || LooksLikeWorkEntryHeader(line))
		{
			return false;
		}

		if (DateRangeParser.TryParse(line, out _))
		{
			return false;
		}

		if (line.StartsWith("- ", StringComparison.Ordinal)
			|| line.StartsWith("Technologies:", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (!line.Contains(','))
		{
			return false;
		}

		var parts = line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		return parts.Length == 2 && LooksLikeCityCountryPair(parts[0], parts[1]);
	}

	internal static bool LooksLikeTechnologyList(string line)
	{
		if (!line.Contains(','))
		{
			return false;
		}

		if (ContainsTechnologyListProseIndicators(line))
		{
			return false;
		}

		var parts = SplitCommaList(line).ToArray();
		if (parts.Length < 2 || parts.Any(part => part.Length is < 1 or > 40))
		{
			return false;
		}

		if (parts.Length == 2 && LooksLikeCityCountryPair(parts[0], parts[1]))
		{
			return false;
		}

		return true;
	}

	internal static bool ContainsTechnologyListProseIndicators(string line)
	{
		if (line.Length > 72)
		{
			return true;
		}

		if (line.Contains('.', StringComparison.Ordinal) && !line.Contains(".NET", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (char.IsLower(line[0]))
		{
			return true;
		}

		string[] proseTokens =
		[
			" using ",
			" for ",
			" with ",
			" the ",
			" to ",
			" in ",
			" on ",
			" of ",
			" a ",
			" an ",
			" and delivering ",
			" and reviewed ",
			" and maintained ",
			" and collaborated ",
			" and iterated ",
			" and technical ",
			" and user ",
			" and backend ",
			" and frontend ",
			" and code ",
			" and security",
			" and auditability"
		];

		var lowerLine = $" {line.ToLowerInvariant()} ";
		if (proseTokens.Any(token => lowerLine.Contains(token, StringComparison.Ordinal)))
		{
			return true;
		}

		return SplitCommaList(line).Any(part =>
			part.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length > 4
			|| Regex.IsMatch(part, @"\b(and|with|for|the|to|in|on|of|a|an)\b", RegexOptions.IgnoreCase));
	}

	internal static HashSet<string> CollectSidebarSkillTokens(string skillsBody)
	{
		var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(skillsBody))
		{
			return tokens;
		}

		foreach (var line in skillsBody.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			if (line.Contains(':', StringComparison.Ordinal))
			{
				var parts = line.Split(':', 2);
				if (parts.Length == 2)
				{
					foreach (var skillName in SplitCommaList(parts[1]))
					{
						tokens.Add(skillName);
					}
				}

				continue;
			}

			if (line.StartsWith("- ", StringComparison.Ordinal))
			{
				tokens.Add(line[2..].Trim());
				continue;
			}

			if (TryParseSkillPreviewLine(line, out var previewSkillName))
			{
				tokens.Add(previewSkillName);
				continue;
			}

			if (line.Contains(',', StringComparison.Ordinal))
			{
				foreach (var skillName in SplitCommaList(line))
				{
					if (IsPlausibleSkillName(skillName))
					{
						tokens.Add(skillName);
					}
				}
			}
		}

		return tokens;
	}

	internal static bool IsSidebarSkillLine(string line, IReadOnlySet<string> sidebarSkillTokens)
	{
		return sidebarSkillTokens.Contains(line.Trim());
	}

	internal static bool TrySkipSidebarSkillRun(
		string[] lines,
		ref int lineIndex,
		IReadOnlySet<string> sidebarSkillTokens)
	{
		if (!IsLikelySidebarSkillBlockLine(lines[lineIndex], sidebarSkillTokens))
		{
			return false;
		}

		var runLength = 1;
		while (lineIndex + runLength < lines.Length
			   && IsLikelySidebarSkillBlockLine(lines[lineIndex + runLength], sidebarSkillTokens))
		{
			runLength++;
		}

		if (runLength < 2)
		{
			return IsSidebarSkillLine(lines[lineIndex], sidebarSkillTokens)
				|| LooksLikeSidebarSkillToken(lines[lineIndex]);
		}

		lineIndex += runLength - 1;
		return true;
	}

	internal static bool IsLikelySidebarSkillBlockLine(string line, IReadOnlySet<string> sidebarSkillTokens)
	{
		if (line.StartsWith("- ", StringComparison.Ordinal)
			|| line.StartsWith("Technologies:", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (LooksLikeTechnologyList(line))
		{
			return false;
		}

		if (IsSidebarSkillLine(line, sidebarSkillTokens) || LooksLikeSidebarSkillToken(line))
		{
			return true;
		}

		if (line.Contains(" - ", StringComparison.Ordinal)
			|| line.Contains(" at ", StringComparison.OrdinalIgnoreCase)
			|| DateRangeParser.TryParse(line, out _)
			|| LooksLikeLocationLine(line)
			|| line.StartsWith("Project ", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return line.Length <= 40
			&& !line.Contains('.', StringComparison.Ordinal)
			&& line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length <= 4;
	}

	internal static bool TryParseContactLocationLine(string line, out string location)
	{
		location = string.Empty;
		if (string.IsNullOrWhiteSpace(line)
			|| CvImportPatterns.Email.IsMatch(line)
			|| CvImportPatterns.Phone.IsMatch(line)
			|| CvImportPatterns.Url.IsMatch(line)
			|| DateRangeParser.TryParse(line, out _))
		{
			return false;
		}

		if (line.Contains('/', StringComparison.Ordinal) && !line.Contains(',', StringComparison.Ordinal))
		{
			return false;
		}

		if (!line.Contains(',', StringComparison.Ordinal))
		{
			return false;
		}

		var parts = line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (parts.Length != 2 || parts[0].Length < 2 || parts[1].Length < 2)
		{
			return false;
		}

		if (!char.IsLetter(parts[0][0]) || !char.IsLetter(parts[1][0]))
		{
			return false;
		}

		location = $"{parts[0]}, {parts[1]}";
		return true;
	}

	internal static bool LooksLikeCityCountryPair(string city, string country)
	{
		return city.Length >= 2
			&& country.Length >= 3
			&& char.IsLetter(city[0])
			&& char.IsLetter(country[0])
			&& !city.Any(char.IsDigit)
			&& !country.Any(char.IsDigit)
			&& !city.Contains('.')
			&& !country.Contains('.')
			&& !country.Contains('#')
			&& !city.Contains('#');
	}

	internal static bool LooksLikeSidebarSkillToken(string line)
	{
		if (string.IsNullOrWhiteSpace(line)
			|| line.Contains(' ')
			|| line.Contains('.')
			|| line.Contains(',')
			|| line.Contains(':'))
		{
			return false;
		}

		return line.Length <= 30;
	}

	internal static bool IsRepeatedJobTitleLine(string line, string jobTitle)
	{
		return !string.IsNullOrWhiteSpace(jobTitle)
			&& line.Equals(jobTitle, StringComparison.OrdinalIgnoreCase);
	}

	internal static string MergeTechnologies(string existing, string additional)
	{
		if (string.IsNullOrWhiteSpace(existing))
		{
			return additional.Trim();
		}

		if (string.IsNullOrWhiteSpace(additional))
		{
			return existing.Trim();
		}

		return $"{existing.Trim()}, {additional.Trim()}";
	}

}
