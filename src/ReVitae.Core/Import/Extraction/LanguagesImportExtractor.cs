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
	internal static IReadOnlyList<LanguageEntry> ExtractLanguages(string body, ImportSectionExtractionContext context)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return [];
		}

		var entries = new List<LanguageEntry>();
		var seenLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var line in body.Split('\n', StringSplitOptions.TrimEntries))
		{
			if (string.IsNullOrWhiteSpace(line) || IsLanguageProficiencySubline(line))
			{
				continue;
			}

			var entry = new LanguageEntry();
			var separatorIndex = line.IndexOf('-');
			if (separatorIndex < 0)
			{
				separatorIndex = line.IndexOf('—');
			}

			if (separatorIndex > 0)
			{
				entry.Language = line[..separatorIndex].Trim();
				MapLanguageProficiency(line[(separatorIndex + 1)..].Trim(), entry, context);
			}
			else
			{
				entry.Language = line.Trim();
				context.AddConfidence($"languages.{entry.Id}.language", CvImportConfidence.Medium);
			}

			if (!entry.HasUserInput())
			{
				continue;
			}

			var languageKey = entry.Language.Trim();
			if (!seenLanguages.Add(languageKey))
			{
				continue;
			}

			entries.Add(entry);
		}

		return entries;
	}

}
