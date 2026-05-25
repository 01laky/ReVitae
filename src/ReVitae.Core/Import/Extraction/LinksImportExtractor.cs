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
	internal static IReadOnlyList<LinkEntry> ExtractLinks(
		string body,
		PersonalInformationImport personal,
		ImportSectionExtractionContext context)
	{
		var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			personal.LinkedInUrl,
			personal.GitHubUrl,
			personal.PortfolioUrl
		};

		var entries = new List<LinkEntry>();
		var source = body;
		if (string.IsNullOrWhiteSpace(source))
		{
			return entries;
		}

		foreach (Match match in CvImportPatterns.Url.Matches(source))
		{
			var url = match.Value.Trim();
			if (excluded.Contains(url))
			{
				context.Warnings.Add(new CvImportWarning(TranslationKeys.ImportWarningPersonalLinksDuplicatedSkipped));
				continue;
			}

			var entry = new LinkEntry
			{
				Url = url,
				Label = InferLabelFromUrl(url)
			};
			entries.Add(entry);
			context.AddConfidence(Cv.Links.LinksFieldKeys.Build(entry.Id, Cv.Links.LinksFieldKeys.Url), CvImportConfidence.Medium);
			context.AddConfidence(Cv.Links.LinksFieldKeys.Build(entry.Id, Cv.Links.LinksFieldKeys.Label), CvImportConfidence.Low);
		}

		return entries;
	}

}
