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
	internal static IReadOnlyList<WorkExperienceEntry> ExtractWorkExperience(
		string body,
		IReadOnlySet<string> sidebarSkillTokens,
		Queue<string> orphanWorkDateFragments)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return [];
		}

		var blocks = SplitWorkExperienceBlocks(body);
		var entries = new List<WorkExperienceEntry>();

		foreach (var block in blocks)
		{
			var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (lines.Length == 0)
			{
				continue;
			}

			var entry = new WorkExperienceEntry();
			var titleLine = lines[0];
			ParsedDateRange? dateRange = null;
			var lineIndex = 1;
			var titleEmbedsRoleCompany = LooksLikeWorkEntryHeader(titleLine);
			var nextLineIsExportMeta = lineIndex < lines.Length && lines[lineIndex].Contains('·');

			if (DateRangeParser.TryParse(titleLine, out var titleDates))
			{
				dateRange = titleDates;
				titleLine = lines.Length > 1 ? lines[1] : string.Empty;
				lineIndex = 2;
				titleEmbedsRoleCompany = LooksLikeWorkEntryHeader(titleLine);
				nextLineIsExportMeta = lineIndex < lines.Length && lines[lineIndex].Contains('·');
			}
			else if (!titleEmbedsRoleCompany || !nextLineIsExportMeta)
			{
				TryParseLeadingWorkExperienceMetadata(lines, ref lineIndex, entry, out dateRange, orphanWorkDateFragments);
			}

			if (!string.IsNullOrWhiteSpace(titleLine))
			{
				SplitTitleCompany(titleLine, out var jobTitle, out var company);
				entry.JobTitle = jobTitle;
				if (string.IsNullOrWhiteSpace(entry.Company))
				{
					entry.Company = company;
				}
			}

			if (lineIndex < lines.Length
				&& TryParseExportDelimitedMetaLine(lines[lineIndex], orphanWorkDateFragments, out var metaParts, out var metaDates))
			{
				if (titleEmbedsRoleCompany && nextLineIsExportMeta)
				{
					if (metaParts.Count >= 1 && string.IsNullOrWhiteSpace(entry.Location))
					{
						entry.Location = metaParts[0];
					}
				}
				else
				{
					if (metaParts.Count >= 1 && string.IsNullOrWhiteSpace(entry.Company))
					{
						entry.Company = metaParts[0];
					}

					if (metaParts.Count >= 2)
					{
						entry.Location = metaParts[1];
					}
				}

				dateRange ??= metaDates;
				lineIndex++;
			}

			if (dateRange is not null)
			{
				entry.StartMonth = dateRange.StartMonth;
				entry.StartYear = dateRange.StartYear;
				entry.EndMonth = dateRange.EndMonth;
				entry.EndYear = dateRange.EndYear;
				entry.IsCurrentlyWorking = dateRange.IsPresent;
			}

			if (!entry.IsCurrentlyWorking)
			{
				TryApplyPresentWorkDatesFromHeaderLines(lines, entry, ref dateRange);
			}

			if (entry.StartYear.HasValue && !entry.EndYear.HasValue && !entry.IsCurrentlyWorking)
			{
				foreach (var line in lines)
				{
					if (!CvImportPatterns.IsPresentToken(line) && !line.Contains("Present", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					entry.IsCurrentlyWorking = true;
					break;
				}
			}

			var descriptionLines = new List<string>();
			var achievementLines = new List<string>();
			for (; lineIndex < lines.Length; lineIndex++)
			{
				var line = lines[lineIndex];
				if (TrySkipSidebarSkillRun(lines, ref lineIndex, sidebarSkillTokens))
				{
					continue;
				}

				if (line.StartsWith("- ", StringComparison.Ordinal))
				{
					achievementLines.Add(line[2..]);
					continue;
				}

				if (line.StartsWith("Technologies:", StringComparison.OrdinalIgnoreCase))
				{
					entry.Technologies = MergeTechnologies(entry.Technologies, line["Technologies:".Length..].Trim());
					continue;
				}

				if (LooksLikeTechnologyList(line))
				{
					entry.Technologies = MergeTechnologies(entry.Technologies, line);
					continue;
				}

				if (IsSidebarSkillLine(line, sidebarSkillTokens)
					|| LooksLikeSidebarSkillToken(line)
					|| IsRepeatedJobTitleLine(line, entry.JobTitle))
				{
					continue;
				}

				descriptionLines.Add(line);
			}

			entry.Description = string.Join('\n', descriptionLines).Trim();
			entry.Achievements = string.Join('\n', achievementLines).Trim();
			NormalizeWorkExperienceDates(entry);

			if (entry.HasUserInput())
			{
				entries.Add(entry);
			}
		}

		return entries;
	}

}
