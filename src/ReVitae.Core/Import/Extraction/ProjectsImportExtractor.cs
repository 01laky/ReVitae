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
	internal static IReadOnlyList<ProjectEntry> ExtractProjects(string body)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return [];
		}

		var entries = new List<ProjectEntry>();
		foreach (var block in SplitProjectBlocks(body))
		{
			var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (lines.Length == 0)
			{
				continue;
			}

			var entry = new ProjectEntry();
			var lineIndex = 0;
			ParsedDateRange? dateRange = null;

			if (DateRangeParser.TryParse(lines[0], out var leadingDate))
			{
				dateRange = leadingDate;
				lineIndex = 1;
			}

			if (lineIndex < lines.Length && string.IsNullOrWhiteSpace(entry.Name))
			{
				entry.Name = lines[lineIndex++];
			}

			if (lineIndex < lines.Length
				&& dateRange is null
				&& DateRangeParser.TryParse(lines[lineIndex], out var inlineDate))
			{
				dateRange = inlineDate;
				lineIndex++;
			}

			if (dateRange is not null)
			{
				ApplyProjectDateRange(entry, dateRange);
			}

			var descriptionLines = new List<string>();
			for (; lineIndex < lines.Length; lineIndex++)
			{
				var line = lines[lineIndex];
				if (dateRange is null && TryParseLabeledDateRangeLine(line, out var labeledRange))
				{
					dateRange = labeledRange;
					ApplyProjectDateRange(entry, dateRange);
					continue;
				}

				var urlMatch = CvImportPatterns.Url.Match(line);
				if (urlMatch.Success && string.IsNullOrWhiteSpace(entry.ProjectUrl))
				{
					entry.ProjectUrl = urlMatch.Value;
					continue;
				}

				if (line.StartsWith("Tech:", StringComparison.OrdinalIgnoreCase)
					|| line.StartsWith("Stack:", StringComparison.OrdinalIgnoreCase))
				{
					var techPart = line[(line.IndexOf(':') + 1)..];
					foreach (var tech in SplitCommaList(techPart))
					{
						entry.Technologies.Add(new ProjectTechnologyItem { Name = tech });
					}

					continue;
				}

				descriptionLines.Add(line);
			}

			entry.Description = string.Join('\n', descriptionLines).Trim();
			if (entry.HasUserInput())
			{
				entries.Add(entry);
			}
		}

		return entries;
	}

}
