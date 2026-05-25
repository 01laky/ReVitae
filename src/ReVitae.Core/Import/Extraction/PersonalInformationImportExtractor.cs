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
	internal static PersonalInformationImport ExtractPersonalInformation(
		CvSegmentationResult segmentation,
		ImportSectionExtractionContext context,
		IReadOnlyList<string>? hyperlinkUrls = null,
		ReVitaePdfExportHints? reVitaeHints = null)
	{
		var contactBody = GetBody(segmentation, CvImportSectionId.Contact);
		var supplementalContact = CollectSupplementalPersonalContactText(segmentation);
		var personalSource = string.Join(
			"\n",
			new[] { segmentation.HeaderBlock, contactBody, supplementalContact }
				.Where(part => !string.IsNullOrWhiteSpace(part)));
		var headerLines = personalSource.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var nameHeaderLines = segmentation.HeaderBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var personal = new PersonalInformationImport();
		var assignedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		var combinedHeader = MergeSplitPersonalFieldLines(MergeSplitUrlLines(personalSource));
		if (hyperlinkUrls is { Count: > 0 })
		{
			combinedHeader += "\n" + string.Join('\n', hyperlinkUrls);
		}
		var emailMatch = CvImportPatterns.Email.Match(combinedHeader);
		if (emailMatch.Success)
		{
			personal.Email = emailMatch.Value;
			context.AddConfidence(MainPersonalInformationFieldKeys.Email, CvImportConfidence.High);
		}

		var phoneMatch = CvImportPatterns.Phone.Match(combinedHeader);
		if (phoneMatch.Success)
		{
			personal.Phone = phoneMatch.Value.Trim();
			context.AddConfidence(MainPersonalInformationFieldKeys.Phone, CvImportConfidence.High);
		}

		foreach (Match urlMatch in CvImportPatterns.Url.Matches(combinedHeader))
		{
			var url = urlMatch.Value.Trim();
			if (url.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(personal.LinkedInUrl))
			{
				personal.LinkedInUrl = url;
				assignedUrls.Add(url);
				context.AddConfidence(MainPersonalInformationFieldKeys.LinkedInUrl, CvImportConfidence.High);
			}
			else if (url.Contains("github.com", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(personal.GitHubUrl))
			{
				personal.GitHubUrl = url;
				assignedUrls.Add(url);
				context.AddConfidence(MainPersonalInformationFieldKeys.GitHubUrl, CvImportConfidence.High);
			}
			else if (string.IsNullOrWhiteSpace(personal.PortfolioUrl) && !assignedUrls.Contains(url))
			{
				personal.PortfolioUrl = url;
				assignedUrls.Add(url);
				context.AddConfidence(MainPersonalInformationFieldKeys.PortfolioUrl, CvImportConfidence.Medium);
			}
		}

		if (reVitaeHints?.IsLikelyReVitaeExport == true)
		{
			ApplyReVitaeExportHyperlinks(personal, hyperlinkUrls, assignedUrls, context);
		}

		foreach (var line in headerLines)
		{
			var labeled = CvImportPatterns.LabeledValue.Match(line);
			if (!labeled.Success)
			{
				continue;
			}

			var label = labeled.Groups["label"].Value.Trim();
			var value = labeled.Groups["value"].Value.Trim();
			if (label.Equals("location", StringComparison.OrdinalIgnoreCase) || label.Equals("lokalita", StringComparison.OrdinalIgnoreCase))
			{
				personal.Location = value;
				context.AddConfidence(MainPersonalInformationFieldKeys.Location, CvImportConfidence.Medium);
			}
			else if (label.Contains("linkedin", StringComparison.OrdinalIgnoreCase)
					 && string.IsNullOrWhiteSpace(personal.LinkedInUrl))
			{
				personal.LinkedInUrl = NormalizeImportedUrl(value);
				assignedUrls.Add(personal.LinkedInUrl);
				context.AddConfidence(MainPersonalInformationFieldKeys.LinkedInUrl, CvImportConfidence.High);
			}
			else if (label.Contains("github", StringComparison.OrdinalIgnoreCase)
					 && string.IsNullOrWhiteSpace(personal.GitHubUrl))
			{
				personal.GitHubUrl = NormalizeImportedUrl(value);
				assignedUrls.Add(personal.GitHubUrl);
				context.AddConfidence(MainPersonalInformationFieldKeys.GitHubUrl, CvImportConfidence.High);
			}
			else if ((label.Contains("portfolio", StringComparison.OrdinalIgnoreCase)
					  || label.Contains("website", StringComparison.OrdinalIgnoreCase))
					 && string.IsNullOrWhiteSpace(personal.PortfolioUrl))
			{
				personal.PortfolioUrl = NormalizeImportedUrl(value);
				assignedUrls.Add(personal.PortfolioUrl);
				context.AddConfidence(MainPersonalInformationFieldKeys.PortfolioUrl, CvImportConfidence.Medium);
			}
			else if (label.Contains("professional title", StringComparison.OrdinalIgnoreCase)
					 && string.IsNullOrWhiteSpace(personal.ProfessionalTitle))
			{
				personal.ProfessionalTitle = value;
				context.AddConfidence(MainPersonalInformationFieldKeys.ProfessionalTitle, CvImportConfidence.Medium);
			}
		}

		if (string.IsNullOrWhiteSpace(personal.Location))
		{
			foreach (var line in headerLines)
			{
				if (TryParseContactLocationLine(line, out var location))
				{
					personal.Location = location;
					context.AddConfidence(MainPersonalInformationFieldKeys.Location, CvImportConfidence.Medium);
					break;
				}
			}
		}

		var headerLooksLikeSummary = HeaderLooksLikeSummaryProse(nameHeaderLines);

		if (headerLooksLikeSummary)
		{
			TryAssignNameFromSkillsSection(segmentation, personal, context);
		}

		if (string.IsNullOrWhiteSpace(personal.FirstName))
		{
			TryAssignBestPersonNameFromLines(nameHeaderLines, personal, context, CvImportConfidence.Medium);
		}

		if (!headerLooksLikeSummary && string.IsNullOrWhiteSpace(personal.FirstName))
		{
			TryAssignNameFromSkillsSection(segmentation, personal, context);
		}

		if (!headerLooksLikeSummary && string.IsNullOrWhiteSpace(personal.FirstName))
		{
			TryAssignSplitPersonNameFromLines(nameHeaderLines, personal, context, CvImportConfidence.Medium);
		}

		if (string.IsNullOrWhiteSpace(personal.FirstName))
		{
			var allPersonalLines = personalSource.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			TryAssignSplitPersonNameFromLines(allPersonalLines, personal, context, CvImportConfidence.Medium);
		}

		if (string.IsNullOrWhiteSpace(personal.FirstName))
		{
			foreach (var line in nameHeaderLines)
			{
				if (IsLikelyNameToken(line)
					&& !CvImportPatterns.Email.IsMatch(line)
					&& !CvImportPatterns.Url.IsMatch(line))
				{
					personal.FirstName = line;
					context.AddConfidence(MainPersonalInformationFieldKeys.FirstName, CvImportConfidence.Low);
					context.Warnings.Add(new CvImportWarning(TranslationKeys.ImportWarningNameUncertain));
					break;
				}
			}
		}

		if (string.IsNullOrWhiteSpace(personal.FirstName))
		{
			TryAssignNameFromOtherSections(segmentation, personal, context);
		}

		if (string.IsNullOrWhiteSpace(personal.FirstName))
		{
			var segmentedLines = CollectSegmentedPersonalLines(segmentation);
			TryAssignReVitaeSidebarNameBeforeEmail(segmentedLines, personal, context);
			TryAssignSplitPersonNameFromLines(segmentedLines, personal, context, CvImportConfidence.Medium);
		}

		if (nameHeaderLines.Length > 1 && string.IsNullOrWhiteSpace(personal.ProfessionalTitle))
		{
			var titleLineIndex = !string.IsNullOrWhiteSpace(personal.LastName)
				&& nameHeaderLines.Length > 2
				&& nameHeaderLines[1].Equals(personal.LastName, StringComparison.Ordinal)
					? 2
					: 1;
			if (titleLineIndex < nameHeaderLines.Length)
			{
				var titleLine = nameHeaderLines[titleLineIndex];
				if (!CvImportPatterns.Email.IsMatch(titleLine) && !CvImportPatterns.Url.IsMatch(titleLine))
				{
					personal.ProfessionalTitle = titleLine;
					context.AddConfidence(MainPersonalInformationFieldKeys.ProfessionalTitle, CvImportConfidence.Medium);
				}
			}
		}

		if (segmentation.SectionBodies.TryGetValue(CvImportSectionId.Summary, out var summaryBody)
			&& !string.IsNullOrWhiteSpace(summaryBody))
		{
			personal.ShortSummary = NormalizeImportedBoundedText(
				CollapseRepeatedImportParagraphs(summaryBody),
				maxLength: 800);
			context.AddConfidence(MainPersonalInformationFieldKeys.ShortSummary, CvImportConfidence.Medium);
		}

		return personal;
	}

}
