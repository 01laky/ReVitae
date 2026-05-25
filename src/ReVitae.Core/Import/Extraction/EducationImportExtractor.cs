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
	internal static IReadOnlyList<EducationEntry> ExtractEducation(string body, ImportSectionExtractionContext context)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return [];
		}

		var entries = new List<EducationEntry>();
		foreach (var block in SplitEducationBlocks(body))
		{
			var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (lines.Length == 0)
			{
				continue;
			}

			var entry = new EducationEntry();
			var lineIndex = 0;
			ParsedDateRange? dateRange = null;

			if (lineIndex < lines.Length && DateRangeParser.TryParse(lines[lineIndex], out var leadingDate))
			{
				dateRange = leadingDate;
				lineIndex++;
				if (lineIndex < lines.Length && LooksLikeLocationLine(lines[lineIndex]))
				{
					entry.Location = lines[lineIndex];
					lineIndex++;
				}
			}

			var headerLines = new List<string>();
			var exportMetaApplied = false;
			for (; lineIndex < lines.Length; lineIndex++)
			{
				var line = lines[lineIndex];

				if (TryParseExportDelimitedMetaLine(line, new Queue<string>(), out var metaParts, out var metaDates))
				{
					if (headerLines.Count == 1)
					{
						entry.Degree = headerLines[0];
						entry.Institution = metaParts[0];
						if (metaParts.Count >= 2)
						{
							entry.Location = metaParts[1];
						}

						if (metaParts.Count >= 3)
						{
							InferDegreeType(entry, metaParts[2]);
						}

						dateRange ??= metaDates;
						exportMetaApplied = true;
						lineIndex++;
						break;
					}

					if (LooksLikeInstitutionFirstEducationHeader(headerLines))
					{
						entry.Institution = headerLines[0];
						entry.Degree = headerLines[1];
						if (metaParts.Count >= 1)
						{
							entry.Location = metaParts[0];
						}

						if (metaParts.Count >= 2)
						{
							InferDegreeType(entry, metaParts[1]);
						}

						dateRange ??= metaDates;
						exportMetaApplied = true;
						lineIndex++;
						break;
					}
				}

				if (DateRangeParser.TryParse(line, out var inlineDate))
				{
					dateRange ??= inlineDate;
					lineIndex++;
					break;
				}

				if (string.IsNullOrWhiteSpace(entry.Location) && LooksLikeLocationLine(line) && headerLines.Count == 0)
				{
					entry.Location = line;
					continue;
				}

				if (headerLines.Count > 0 && LooksLikeEducationDescriptionLine(line))
				{
					break;
				}

				headerLines.Add(line);
			}

			if (!exportMetaApplied)
			{
				AssignEducationHeader(entry, headerLines);
			}

			if (lineIndex < lines.Length
				&& DateRangeParser.TryParse(lines[lineIndex], out var trailingDate))
			{
				dateRange ??= trailingDate;
				lineIndex++;
			}

			if (dateRange is not null)
			{
				ApplyEducationDateRange(entry, dateRange);
				if (InferMissingEducationStartDate(entry))
				{
					context.AddConfidence(
						EducationFieldKeys.Build(entry.Id, EducationFieldKeys.StartMonth),
						CvImportConfidence.Low);
					context.AddConfidence(
						EducationFieldKeys.Build(entry.Id, EducationFieldKeys.StartYear),
						CvImportConfidence.Low);
				}
			}

			if (lineIndex < lines.Length)
			{
				entry.Description = string.Join('\n', lines.Skip(lineIndex)).Trim();
			}

			if (entry.HasUserInput() && !LooksLikeGarbageEducationEntry(entry))
			{
				entries.Add(entry);
			}
		}

		return entries;
	}

}
