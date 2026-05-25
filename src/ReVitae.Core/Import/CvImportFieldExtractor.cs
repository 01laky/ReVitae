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

namespace ReVitae.Core.Import;

internal sealed class CvImportBuildContext
{
	public List<CvImportWarning> Warnings { get; } = [];

	public List<ImportedFieldConfidence> FieldConfidences { get; } = [];

	public void AddConfidence(string fieldKey, CvImportConfidence confidence)
	{
		FieldConfidences.Add(new ImportedFieldConfidence(fieldKey, confidence));
	}
}

public static class CvImportFieldExtractor
{
	public static CvImportResult Extract(
		CvSegmentationResult segmentation,
		IReadOnlyList<string>? hyperlinkUrls = null,
		ReVitaePdfExportHints? reVitaeHints = null)
	{
		var context = new CvImportBuildContext();
		var personal = ExtractPersonalInformation(segmentation, context, hyperlinkUrls, reVitaeHints);
		var sidebarSkillTokens = CollectSidebarSkillTokens(GetBody(segmentation, CvImportSectionId.Skills));
		var orphanWorkDateFragments = CollectOrphanWorkDateFragments(segmentation.HeaderBlock);
		var workExperience = ExtractWorkExperience(
			GetBody(segmentation, CvImportSectionId.WorkExperience),
			sidebarSkillTokens,
			orphanWorkDateFragments);
		var education = ExtractEducation(GetBody(segmentation, CvImportSectionId.Education), context);
		var skills = ExtractSkills(GetBody(segmentation, CvImportSectionId.Skills), context);
		var languages = ExtractLanguages(GetBody(segmentation, CvImportSectionId.Languages), context);
		var certificates = ExtractCertificates(GetBody(segmentation, CvImportSectionId.Certificates));
		var projects = ExtractProjects(GetBody(segmentation, CvImportSectionId.Projects));
		var links = ExtractLinks(GetBody(segmentation, CvImportSectionId.Links), personal, context);
		var additional = BuildAdditionalInformation(segmentation, context);

		AddEntryConfidences(context, workExperience, education, languages, certificates, projects, links);

		var sectionHasData = BuildSectionHasData(
			personal,
			workExperience,
			education,
			skills,
			languages,
			certificates,
			projects,
			links,
			additional);

		return new CvImportResult
		{
			Success = HasStructuredData(personal, workExperience, education, skills, languages, certificates, projects, links, additional),
			ErrorMessageKey = null,
			Personal = personal,
			WorkExperienceEntries = workExperience,
			EducationEntries = education,
			SkillsGroups = skills,
			LanguageEntries = languages,
			CertificateEntries = certificates,
			ProjectEntries = projects,
			LinkEntries = links,
			AdditionalInformationContent = additional,
			SectionHasData = sectionHasData,
			Warnings = context.Warnings,
			FieldConfidences = context.FieldConfidences
		};
	}

	private static PersonalInformationImport ExtractPersonalInformation(
		CvSegmentationResult segmentation,
		CvImportBuildContext context,
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

	private static IReadOnlyList<WorkExperienceEntry> ExtractWorkExperience(
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

	private static IReadOnlyList<EducationEntry> ExtractEducation(string body, CvImportBuildContext context)
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

	private static IReadOnlyList<SkillsGroupEntry> ExtractSkills(string body, CvImportBuildContext context)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return [];
		}

		body = MergeSplitReVitaeSkillLines(body);
		body = TrimReVitaeSkillBodyPrefix(body);

		var groups = new List<SkillsGroupEntry>();
		SkillsGroupEntry? currentGroup = null;
		var skillLines = body.Split('\n', StringSplitOptions.TrimEntries);
		var reVitaeSkillFormat = skillLines.Any(line => TryParseSkillPreviewLine(line, out _));

		for (var lineIndex = 0; lineIndex < skillLines.Length; lineIndex++)
		{
			var line = skillLines[lineIndex];
			if (string.IsNullOrWhiteSpace(line) || IsExportSubheadingLine(line) || LooksLikeWorkBleedLine(line) || IsLikelyPersonNameLine(line))
			{
				continue;
			}

			if (TryParseSkillPreviewLine(line, out var previewSkillName))
			{
				currentGroup ??= EnsureDefaultSkillsGroup(groups);
				if (IsPlausibleSkillName(previewSkillName))
				{
					currentGroup.Skills.Add(new SkillItem { Name = previewSkillName });
				}

				continue;
			}

			if (line.Contains(':', StringComparison.Ordinal))
			{
				var parts = line.Split(':', 2);
				var category = parts[0].Trim();
				if (IsExportSubheadingLine(category) || string.IsNullOrWhiteSpace(parts[1]))
				{
					continue;
				}

				var group = new SkillsGroupEntry { Category = category };
				foreach (var skillName in SplitCommaList(parts[1]))
				{
					if (IsPlausibleSkillName(skillName))
					{
						group.Skills.Add(new SkillItem { Name = skillName });
					}
				}

				if (group.HasUserInput())
				{
					groups.Add(group);
					currentGroup = null;
				}

				continue;
			}

			if (line.StartsWith("- ", StringComparison.Ordinal))
			{
				currentGroup ??= EnsureDefaultSkillsGroup(groups);
				var bulletSkill = line[2..].Trim();
				if (IsPlausibleSkillName(bulletSkill))
				{
					currentGroup.Skills.Add(new SkillItem { Name = bulletSkill });
				}

				continue;
			}

			if (line.Contains(',', StringComparison.Ordinal))
			{
				var commaSkills = SplitCommaList(line).Where(IsPlausibleSkillName).ToArray();
				if (commaSkills.Length < 2)
				{
					continue;
				}

				currentGroup ??= EnsureDefaultSkillsGroup(groups);
				foreach (var skillName in commaSkills)
				{
					currentGroup.Skills.Add(new SkillItem { Name = skillName });
				}

				continue;
			}

			if (IsReVitaeExportSkillCategory(line, reVitaeSkillFormat, skillLines, lineIndex))
			{
				currentGroup = new SkillsGroupEntry { Category = line.Trim() };
				groups.Add(currentGroup);
				continue;
			}

			if (!reVitaeSkillFormat && IsPlausibleStandaloneSkillToken(line))
			{
				currentGroup ??= EnsureDefaultSkillsGroup(groups);
				currentGroup.Skills.Add(new SkillItem { Name = line.Trim() });
			}
		}

		RemovePersonNameSkillGroups(groups);

		if (groups.Count > 0)
		{
			context.AddConfidence("skills.import.defaultCategory", CvImportConfidence.Medium);
		}

		return groups;
	}

	private static bool TryParseSkillPreviewLine(string line, out string skillName)
	{
		skillName = string.Empty;
		if (!line.Contains('·', StringComparison.Ordinal) || LooksLikeWorkBleedLine(line))
		{
			return false;
		}

		var parts = line.Split('·', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length is < 2 or > 3)
		{
			return false;
		}

		skillName = parts[0];
		if (!IsPlausibleSkillName(skillName))
		{
			return false;
		}

		var tail = string.Join(" · ", parts.Skip(1));
		return !tail.Contains("Full-time", StringComparison.OrdinalIgnoreCase)
			&& !tail.Contains("Part-time", StringComparison.OrdinalIgnoreCase)
			&& !tail.Contains(" s.r.o.", StringComparison.OrdinalIgnoreCase)
			&& !DateRangeParser.TryParse(tail, out _);
	}

	private static bool IsKnownSkillCategoryLabel(string category) =>
		category.Equals("General", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Backend", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Frontend", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Programming", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("DevOps", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Languages", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Languages & Runtimes", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Backend & APIs", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Data & Storage", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Cloud & DevOps", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Observability", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Security", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Architecture", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Leadership", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Testing", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Frontend", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Mobile & Edge", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("AI & Automation", StringComparison.OrdinalIgnoreCase);

	private static bool IsReVitaeExportSkillCategory(
		string line,
		bool reVitaeSkillFormat,
		IReadOnlyList<string> skillLines,
		int lineIndex)
	{
		if (!reVitaeSkillFormat)
		{
			return IsPlausibleSkillCategory(line);
		}

		var category = line.Trim();
		if (Regex.IsMatch(
				category,
				@"^(Designed|Implemented|Built|Contributed|Collaborated|Delivered|Integrated|Worked|Senior|mobile|Developed|OAuth|Project|Platforms|development|focus|high|Uses)\b",
				RegexOptions.IgnoreCase))
		{
			return false;
		}

		if (category.Contains('·', StringComparison.Ordinal)
			|| category.Contains(':', StringComparison.Ordinal)
			|| category.StartsWith("- ", StringComparison.Ordinal)
			|| TryParseSkillPreviewLine(category, out _)
			|| IsExportSubheadingLine(category)
			|| LooksLikeWorkBleedLine(category)
			|| IsLikelyPersonNameLine(category))
		{
			return false;
		}

		var nextSkillPreviewIndex = FindNextSkillPreviewLineIndex(skillLines, lineIndex + 1);
		if (IsKnownSkillCategoryLabel(category)
			|| category.Contains('&', StringComparison.Ordinal)
			|| category.Contains(" and ", StringComparison.OrdinalIgnoreCase))
		{
			return nextSkillPreviewIndex >= 0
				|| IsPlausibleSkillCategory(category);
		}

		if (nextSkillPreviewIndex < 0)
		{
			return false;
		}

		return !IsPlausibleStandaloneSkillToken(category)
			&& !LooksLikeWorkBleedLine(category)
			&& IsPlausibleSkillCategory(category);
	}

	private static int FindNextSkillPreviewLineIndex(IReadOnlyList<string> skillLines, int startIndex)
	{
		for (var index = startIndex; index < skillLines.Count && index < startIndex + 4; index++)
		{
			if (string.IsNullOrWhiteSpace(skillLines[index]))
			{
				continue;
			}

			if (TryParseSkillPreviewLine(skillLines[index], out _))
			{
				return index;
			}
		}

		return -1;
	}

	private static bool IsPlausibleSkillCategory(string line)
	{
		var category = line.Trim();
		if (category.Length is < 2 or > 40)
		{
			return false;
		}

		if (IsKnownSkillCategoryLabel(category))
		{
			return true;
		}

		if (category.Contains(' ', StringComparison.Ordinal))
		{
			return false;
		}

		return !category.Contains('·', StringComparison.Ordinal)
			&& !category.Contains(',', StringComparison.Ordinal)
			&& !DateRangeParser.TryParse(category, out _)
			&& !LooksLikeWorkBleedLine(category)
			&& !IsExportSubheadingLine(category)
			&& !IsLikelyPersonNameLine(category)
			&& !IsPlausibleStandaloneSkillToken(category)
			&& !Regex.IsMatch(
				category,
				@"^(Designed|Implemented|Built|Contributed|Collaborated|Delivered|Integrated|Worked|Senior|Project|Platforms|development|OAuth|focus|high|Uses)\b",
				RegexOptions.IgnoreCase);
	}

	private static bool LooksLikeWorkBleedLine(string line) =>
		line.Contains(" s.r.o.", StringComparison.OrdinalIgnoreCase)
		|| line.Contains(" a.s.", StringComparison.OrdinalIgnoreCase)
		|| line.Contains("Full-time", StringComparison.OrdinalIgnoreCase)
		|| line.Contains("Part-time", StringComparison.OrdinalIgnoreCase)
		|| line.Contains(" · Full-time", StringComparison.OrdinalIgnoreCase)
		|| line.Contains(" · Part-time", StringComparison.OrdinalIgnoreCase)
		|| DateRangeParser.TryParse(line, out _);

	private static bool IsExportSubheadingLine(string line)
	{
		var label = line.Trim().TrimEnd(':');
		return label.Equals("Technologies", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Achievements", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Company URL", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Institution URL", StringComparison.OrdinalIgnoreCase);
	}

	private static IReadOnlyList<LanguageEntry> ExtractLanguages(string body, CvImportBuildContext context)
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

	private static IReadOnlyList<CertificateEntry> ExtractCertificates(string body)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return [];
		}

		var entries = new List<CertificateEntry>();
		foreach (var block in SplitCertificateBlocks(body))
		{
			var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (lines.Length == 0)
			{
				continue;
			}

			var entry = new CertificateEntry();
			ParsedDateRange? issueDate = null;
			ParsedDateRange? expirationDate = null;
			var pendingLines = new List<string>();

			foreach (var line in lines)
			{
				if (TryParseCertificateDetailLine(line, entry, ref issueDate, ref expirationDate))
				{
					continue;
				}

				pendingLines.Add(line);
			}

			AssignCertificateHeader(entry, pendingLines);
			ApplyCertificateIssueDate(entry, issueDate);
			ApplyCertificateExpirationDate(entry, expirationDate);

			if (entry.HasUserInput())
			{
				entries.Add(entry);
			}
		}

		return entries;
	}

	private static bool TryParseCertificateDetailLine(
		string line,
		CertificateEntry entry,
		ref ParsedDateRange? issueDate,
		ref ParsedDateRange? expirationDate)
	{
		if (TryParseLabeledIssueDateLine(line, out var labeledIssueDate))
		{
			issueDate = labeledIssueDate;
			return true;
		}

		if (TryParseLabeledExpirationDateLine(line, out var labeledExpirationDate))
		{
			expirationDate = labeledExpirationDate;
			return true;
		}

		if (TryParseLabeledCertificateValue(line, "issuing organization", out var issuer)
			|| TryParseLabeledCertificateValue(line, "issuer", out issuer))
		{
			entry.Issuer = issuer;
			return true;
		}

		if (TryParseLabeledCertificateValue(line, "credential id", out var credentialId))
		{
			entry.CredentialId = credentialId;
			return true;
		}

		if (TryParseLabeledCertificateValue(line, "credential url", out var credentialUrl))
		{
			entry.CredentialUrl = NormalizeImportedUrl(credentialUrl);
			return true;
		}

		if (issueDate is null
			&& !LooksLikeCertificateMetadataLine(line)
			&& !LooksLikeInlineCertificateHeaderLine(line)
			&& DateRangeParser.TryParse(line, out var parsedIssueDate))
		{
			issueDate = parsedIssueDate;
			return true;
		}

		return false;
	}

	private static bool LooksLikeInlineCertificateHeaderLine(string line) =>
		line.Contains('·', StringComparison.Ordinal)
		&& (line.StartsWith("Professional Certification #", StringComparison.OrdinalIgnoreCase)
			|| CertificateEntryHeader.IsMatch(line));

	private static void AssignCertificateHeader(CertificateEntry entry, List<string> pendingLines)
	{
		if (pendingLines.Count == 0)
		{
			return;
		}

		var headerLine = pendingLines[0];
		pendingLines.RemoveAt(0);

		if (TryParseInlineCertificateHeader(headerLine, entry, out var trailingDescription))
		{
			if (!string.IsNullOrWhiteSpace(trailingDescription))
			{
				pendingLines.Insert(0, trailingDescription);
			}
		}
		else
		{
			entry.Name = headerLine;
			if (pendingLines.Count > 0 && string.IsNullOrWhiteSpace(entry.Issuer))
			{
				if (TryParseLabeledCertificateValue(pendingLines[0], "issuing organization", out var labeledIssuer)
					|| TryParseLabeledCertificateValue(pendingLines[0], "issuer", out labeledIssuer))
				{
					entry.Issuer = labeledIssuer;
					pendingLines.RemoveAt(0);
				}
				else if (!LooksLikeCertificateMetadataLine(pendingLines[0]))
				{
					entry.Issuer = pendingLines[0];
					pendingLines.RemoveAt(0);
				}
			}
		}

		if (pendingLines.Count > 0)
		{
			entry.Description = string.IsNullOrWhiteSpace(entry.Description)
				? string.Join('\n', pendingLines).Trim()
				: $"{entry.Description}\n{string.Join('\n', pendingLines).Trim()}".Trim();
		}
	}

	private static bool TryParseInlineCertificateHeader(
		string line,
		CertificateEntry entry,
		out string trailingDescription)
	{
		trailingDescription = string.Empty;
		if (!line.Contains('·', StringComparison.Ordinal))
		{
			return false;
		}

		var segments = line.Split('·', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (segments.Length == 0)
		{
			return false;
		}

		entry.Name = segments[0];
		var segmentIndex = 1;
		if (segmentIndex < segments.Length && !LooksLikeCertificateDateSegment(segments[segmentIndex]))
		{
			entry.Issuer = segments[segmentIndex++];
		}

		ParsedDateRange? issueDate = null;
		ParsedDateRange? expirationDate = null;
		for (; segmentIndex < segments.Length; segmentIndex++)
		{
			var segment = segments[segmentIndex];
			if (segment.StartsWith("Valid until", StringComparison.OrdinalIgnoreCase)
				|| segment.StartsWith("Valid through", StringComparison.OrdinalIgnoreCase))
			{
				var expirationValue = segment[(segment.IndexOf(' ', StringComparison.Ordinal) + 1)..].Trim();
				if (DateRangeParser.TryParse(expirationValue, out var parsedExpiration))
				{
					expirationDate = parsedExpiration;
				}

				continue;
			}

			if (issueDate is null && DateRangeParser.TryParse(segment, out var parsedIssue))
			{
				issueDate = parsedIssue;
			}
			else if (string.IsNullOrWhiteSpace(trailingDescription))
			{
				trailingDescription = segment;
			}
			else
			{
				trailingDescription = $"{trailingDescription} · {segment}";
			}
		}

		if (issueDate is not null)
		{
			ApplyCertificateIssueDate(entry, issueDate);
		}

		if (expirationDate is not null)
		{
			ApplyCertificateExpirationDate(entry, expirationDate);
		}

		return true;
	}

	private static bool LooksLikeCertificateDateSegment(string segment) =>
		DateRangeParser.TryParse(segment, out _)
		|| segment.StartsWith("Valid until", StringComparison.OrdinalIgnoreCase)
		|| segment.StartsWith("Valid through", StringComparison.OrdinalIgnoreCase);

	private static bool LooksLikeCertificateMetadataLine(string line) =>
		line.Contains(':', StringComparison.Ordinal)
		|| line.Contains("Credential", StringComparison.OrdinalIgnoreCase)
		|| line.Contains("Issuing", StringComparison.OrdinalIgnoreCase)
		|| line.Contains("Focus area", StringComparison.OrdinalIgnoreCase);

	private static bool TryParseLabeledCertificateValue(string line, string labelKeyword, out string value)
	{
		value = string.Empty;
		var labeled = CvImportPatterns.LabeledValue.Match(line);
		if (!labeled.Success
			|| !labeled.Groups["label"].Value.Contains(labelKeyword, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		value = labeled.Groups["value"].Value.Trim();
		return !string.IsNullOrWhiteSpace(value);
	}

	private static IReadOnlyList<ProjectEntry> ExtractProjects(string body)
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

	private static IReadOnlyList<LinkEntry> ExtractLinks(
		string body,
		PersonalInformationImport personal,
		CvImportBuildContext context)
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

	private static string BuildAdditionalInformation(CvSegmentationResult segmentation, CvImportBuildContext context)
	{
		if (segmentation.SectionBodies.TryGetValue(CvImportSectionId.AdditionalInformation, out var body))
		{
			if (!string.IsNullOrWhiteSpace(body))
			{
				context.AddConfidence(Cv.AdditionalInformation.AdditionalInformationFieldKeys.Content, CvImportConfidence.Medium);
			}

			return NormalizeImportedBoundedText(body.Trim(), AdditionalInformationSchema.ContentMaxLength);
		}

		return string.Empty;
	}

	private static void AddEntryConfidences(
		CvImportBuildContext context,
		IReadOnlyList<WorkExperienceEntry> workExperience,
		IReadOnlyList<EducationEntry> education,
		IReadOnlyList<LanguageEntry> languages,
		IReadOnlyList<CertificateEntry> certificates,
		IReadOnlyList<ProjectEntry> projects,
		IReadOnlyList<LinkEntry> links)
	{
		foreach (var entry in workExperience)
		{
			if (!string.IsNullOrWhiteSpace(entry.JobTitle))
			{
				context.AddConfidence(WorkExperienceFieldKeys.Build(entry.Id, WorkExperienceFieldKeys.JobTitle), CvImportConfidence.Medium);
			}

			if (!string.IsNullOrWhiteSpace(entry.Company))
			{
				context.AddConfidence(WorkExperienceFieldKeys.Build(entry.Id, WorkExperienceFieldKeys.Company), CvImportConfidence.Medium);
			}
		}

		foreach (var entry in education)
		{
			if (!string.IsNullOrWhiteSpace(entry.Degree))
			{
				context.AddConfidence(EducationFieldKeys.Build(entry.Id, EducationFieldKeys.Degree), CvImportConfidence.Medium);
			}
		}

		foreach (var entry in languages)
		{
			if (!string.IsNullOrWhiteSpace(entry.Language))
			{
				context.AddConfidence(LanguagesFieldKeys.Build(entry.Id, LanguagesFieldKeys.Language), CvImportConfidence.Medium);
			}
		}

		foreach (var entry in certificates)
		{
			if (!string.IsNullOrWhiteSpace(entry.Name))
			{
				context.AddConfidence(CertificatesFieldKeys.Build(entry.Id, CertificatesFieldKeys.Name), CvImportConfidence.Medium);
			}
		}

		foreach (var entry in projects)
		{
			if (!string.IsNullOrWhiteSpace(entry.Name))
			{
				context.AddConfidence(ProjectsFieldKeys.Build(entry.Id, ProjectsFieldKeys.Name), CvImportConfidence.Medium);
			}
		}

		_ = links;
	}

	private static IReadOnlyDictionary<CvImportSectionId, bool> BuildSectionHasData(
		PersonalInformationImport personal,
		IReadOnlyList<WorkExperienceEntry> workExperience,
		IReadOnlyList<EducationEntry> education,
		IReadOnlyList<SkillsGroupEntry> skills,
		IReadOnlyList<LanguageEntry> languages,
		IReadOnlyList<CertificateEntry> certificates,
		IReadOnlyList<ProjectEntry> projects,
		IReadOnlyList<LinkEntry> links,
		string additional)
	{
		return new Dictionary<CvImportSectionId, bool>
		{
			[CvImportSectionId.PersonalInformation] = personal.HasAnyData(),
			[CvImportSectionId.WorkExperience] = workExperience.Count > 0,
			[CvImportSectionId.Education] = education.Count > 0,
			[CvImportSectionId.Skills] = skills.Count > 0,
			[CvImportSectionId.Languages] = languages.Count > 0,
			[CvImportSectionId.Certificates] = certificates.Count > 0,
			[CvImportSectionId.Projects] = projects.Count > 0,
			[CvImportSectionId.Links] = links.Count > 0,
			[CvImportSectionId.AdditionalInformation] = !string.IsNullOrWhiteSpace(additional)
		};
	}

	private static bool HasStructuredData(
		PersonalInformationImport personal,
		IReadOnlyList<WorkExperienceEntry> workExperience,
		IReadOnlyList<EducationEntry> education,
		IReadOnlyList<SkillsGroupEntry> skills,
		IReadOnlyList<LanguageEntry> languages,
		IReadOnlyList<CertificateEntry> certificates,
		IReadOnlyList<ProjectEntry> projects,
		IReadOnlyList<LinkEntry> links,
		string additional)
	{
		return personal.HasAnyData()
			|| workExperience.Count > 0
			|| education.Count > 0
			|| skills.Count > 0
			|| languages.Count > 0
			|| certificates.Count > 0
			|| projects.Count > 0
			|| links.Count > 0
			|| !string.IsNullOrWhiteSpace(additional);
	}

	private static string GetBody(CvSegmentationResult segmentation, CvImportSectionId sectionId)
	{
		return segmentation.SectionBodies.TryGetValue(sectionId, out var body) ? body : string.Empty;
	}

	private static void NormalizeWorkExperienceDates(WorkExperienceEntry entry)
	{
		if (entry.IsCurrentlyWorking)
		{
			entry.EndMonth = null;
			entry.EndYear = null;
			return;
		}

		if (entry.EndYear.HasValue && !entry.EndMonth.HasValue)
		{
			entry.EndMonth = entry.StartMonth ?? 12;
		}

		if (entry.StartYear.HasValue
			&& !entry.EndYear.HasValue
			&& entry.StartMonth.HasValue
			&& !entry.IsCurrentlyWorking)
		{
			entry.EndYear = entry.StartYear;
			entry.EndMonth = entry.StartMonth;
		}
	}

	private static void TryApplyPresentWorkDatesFromHeaderLines(
		string[] lines,
		WorkExperienceEntry entry,
		ref ParsedDateRange? dateRange)
	{
		for (var scanIndex = 0; scanIndex < Math.Min(lines.Length, 8); scanIndex++)
		{
			var line = lines[scanIndex];
			if (CvImportPatterns.IsPresentToken(line.Trim()) && (entry.StartYear.HasValue || scanIndex > 0))
			{
				entry.IsCurrentlyWorking = true;
				return;
			}

			if (DateRangeParser.TryParse(line, out var directRange) && directRange.IsPresent)
			{
				dateRange = directRange;
				entry.StartMonth = directRange.StartMonth;
				entry.StartYear = directRange.StartYear;
				entry.IsCurrentlyWorking = true;
				return;
			}

			if (DateRangeParser.TryParseTrailingDateRange(line, out var trailingRange, out _)
				&& trailingRange.IsPresent)
			{
				dateRange = trailingRange;
				entry.StartMonth = trailingRange.StartMonth;
				entry.StartYear = trailingRange.StartYear;
				entry.IsCurrentlyWorking = true;
				return;
			}

			if (line.Contains("present", StringComparison.OrdinalIgnoreCase)
				&& DateRangeParser.TryParse(line, out var inlinePresent)
				&& inlinePresent.IsPresent)
			{
				dateRange = inlinePresent;
				entry.StartMonth ??= inlinePresent.StartMonth;
				entry.StartYear ??= inlinePresent.StartYear;
				entry.IsCurrentlyWorking = true;
				return;
			}
		}
	}

	private static void TryParseLeadingWorkExperienceMetadata(
		string[] lines,
		ref int lineIndex,
		WorkExperienceEntry entry,
		out ParsedDateRange? dateRange,
		Queue<string> orphanWorkDateFragments)
	{
		dateRange = null;
		if (lineIndex < lines.Length
			&& TryParseExportDelimitedMetaLine(lines[lineIndex], orphanWorkDateFragments, out var metaParts, out var metaDates))
		{
			if (metaParts.Count >= 1)
			{
				entry.Company = metaParts[0];
			}

			if (metaParts.Count >= 2)
			{
				entry.Location = metaParts[1];
			}

			dateRange = metaDates;
			lineIndex++;
			return;
		}

		if (TryConsumeLeadingDateSection(lines, ref lineIndex, out dateRange, out var location)
			&& !string.IsNullOrWhiteSpace(location))
		{
			entry.Location = location;
		}
	}

	private static bool TryConsumeLeadingDateSection(
		string[] lines,
		ref int lineIndex,
		out ParsedDateRange? dateRange,
		out string? location)
	{
		dateRange = null;
		location = null;
		if (lineIndex >= lines.Length)
		{
			return false;
		}

		if (DateRangeParser.TryParse(lines[lineIndex], out var directDate))
		{
			dateRange = directDate;
			lineIndex++;
			if (lineIndex < lines.Length && LooksLikeLocationLine(lines[lineIndex]))
			{
				location = lines[lineIndex];
				lineIndex++;
			}

			return true;
		}

		if (lineIndex + 1 < lines.Length
			&& LooksLikeLocationLine(lines[lineIndex])
			&& DateRangeParser.TryParse(lines[lineIndex + 1], out var dateAfterLocation))
		{
			location = lines[lineIndex];
			dateRange = dateAfterLocation;
			lineIndex += 2;
			return true;
		}

		for (var index = lineIndex; index < Math.Min(lines.Length, lineIndex + 3); index++)
		{
			if (!DateRangeParser.TryParse(lines[index], out var candidateDates))
			{
				continue;
			}

			if (index > lineIndex && LooksLikeLocationLine(lines[lineIndex]))
			{
				location = lines[lineIndex];
			}

			dateRange = candidateDates;
			lineIndex = index + 1;
			return true;
		}

		return false;
	}

	private static void AssignEducationHeader(EducationEntry entry, IReadOnlyList<string> headerLines)
	{
		if (headerLines.Count == 0)
		{
			return;
		}

		if (LooksLikeInstitutionFirstEducationHeader(headerLines))
		{
			entry.Institution = headerLines[0];
			entry.Degree = headerLines[1];
			InferDegreeType(entry, headerLines[0]);
			return;
		}

		if (headerLines.Count >= 2)
		{
			var combined = string.Join(" ", headerLines);
			if (ShouldTreatAsSingleInstitution(headerLines, combined))
			{
				entry.Institution = combined;
				entry.Degree = InferDefaultDegreeLabel(combined);
				InferDegreeType(entry, combined);
				return;
			}

			if (headerLines.Count >= 3
				&& LooksLikeExplicitDegreeLine(headerLines[0])
				&& LooksLikeInstitutionName(headerLines[^1]))
			{
				entry.Degree = headerLines[0];
				entry.FieldOfStudy = headerLines[1];
				entry.Institution = headerLines[2];
				InferDegreeType(entry, headerLines[2]);
				return;
			}
		}

		if (headerLines.Count == 1)
		{
			var line = headerLines[0];
			if (TrySplitDegreeInstitution(line, out var degree, out var institution))
			{
				entry.Degree = degree;
				entry.Institution = institution;
			}
			else if (LooksLikeInstitutionName(line))
			{
				entry.Institution = line;
				entry.Degree = InferDefaultDegreeLabel(line);
				InferDegreeType(entry, line);
			}
			else
			{
				entry.Degree = line;
			}

			return;
		}

		entry.Degree = headerLines[0];
		entry.Institution = headerLines[1];
		InferDegreeType(entry, headerLines[1]);
	}

	private static void ApplyEducationDateRange(EducationEntry entry, ParsedDateRange range)
	{
		if (range.IsPresent)
		{
			entry.IsCurrentlyStudying = true;
			entry.StartMonth = range.StartMonth;
			entry.StartYear = range.StartYear;
			return;
		}

		var hasStart = range.StartYear.HasValue;
		var hasEnd = range.EndYear.HasValue;

		if (hasStart && hasEnd)
		{
			entry.StartMonth = range.StartMonth;
			entry.StartYear = range.StartYear;
			entry.EndMonth = range.EndMonth;
			entry.EndYear = range.EndYear;
			return;
		}

		if (hasEnd)
		{
			entry.EndMonth = range.EndMonth;
			entry.EndYear = range.EndYear;
			return;
		}

		if (hasStart)
		{
			entry.EndMonth = range.StartMonth;
			entry.EndYear = range.StartYear;
		}
	}

	private static bool InferMissingEducationStartDate(EducationEntry entry)
	{
		if (entry.IsCurrentlyStudying || entry.StartYear.HasValue || !entry.EndYear.HasValue)
		{
			return false;
		}

		var durationYears = entry.DegreeType switch
		{
			DegreeType.HighSchool => 4,
			DegreeType.Associate => 2,
			DegreeType.Bachelor => 3,
			DegreeType.Master => 2,
			DegreeType.Doctorate => 4,
			DegreeType.Certificate => 1,
			_ => 3
		};

		entry.StartMonth = 9;
		entry.StartYear = entry.EndYear.Value - durationYears;
		return true;
	}

	private static void ApplyCertificateIssueDate(CertificateEntry entry, ParsedDateRange? range)
	{
		if (range is null)
		{
			return;
		}

		entry.IssueMonth = range.StartMonth ?? range.EndMonth;
		entry.IssueYear = range.StartYear ?? range.EndYear;
	}

	private static void ApplyCertificateExpirationDate(CertificateEntry entry, ParsedDateRange? range)
	{
		if (range is null)
		{
			return;
		}

		entry.ExpirationMonth = range.StartMonth ?? range.EndMonth;
		entry.ExpirationYear = range.StartYear ?? range.EndYear;
	}

	private static void ApplyProjectDateRange(ProjectEntry entry, ParsedDateRange range)
	{
		if (range.IsPresent)
		{
			entry.IsCurrentlyActive = true;
			entry.StartMonth = range.StartMonth;
			entry.StartYear = range.StartYear;
			return;
		}

		var hasStart = range.StartYear.HasValue;
		var hasEnd = range.EndYear.HasValue;

		if (hasStart && hasEnd)
		{
			entry.StartMonth = range.StartMonth;
			entry.StartYear = range.StartYear;
			entry.EndMonth = range.EndMonth;
			entry.EndYear = range.EndYear;
			return;
		}

		if (hasStart)
		{
			entry.StartMonth = range.StartMonth;
			entry.StartYear = range.StartYear;
		}
	}

	private static bool TrySplitDegreeInstitution(string line, out string degree, out string institution)
	{
		degree = string.Empty;
		institution = string.Empty;

		var separatorIndex = line.IndexOf(" - ", StringComparison.Ordinal);
		if (separatorIndex > 0)
		{
			degree = line[..separatorIndex].Trim();
			institution = line[(separatorIndex + 3)..].Trim();
			return !string.IsNullOrWhiteSpace(degree) && !string.IsNullOrWhiteSpace(institution);
		}

		separatorIndex = line.IndexOf(" at ", StringComparison.OrdinalIgnoreCase);
		if (separatorIndex > 0)
		{
			degree = line[..separatorIndex].Trim();
			institution = line[(separatorIndex + 4)..].Trim();
			return !string.IsNullOrWhiteSpace(degree) && !string.IsNullOrWhiteSpace(institution);
		}

		return false;
	}

	private static bool LooksLikeInstitutionName(string line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return false;
		}

		return line.Contains("high school", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("university", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("college", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("academy", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("institute", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("gymnaz", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("gymnáz", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("stredna skola", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("stredná škola", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("electrical engineering", StringComparison.OrdinalIgnoreCase)
			|| line.Contains(" and training", StringComparison.OrdinalIgnoreCase);
	}

	private static bool LooksLikeExplicitDegreeLine(string line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return false;
		}

		return line.Contains("bachelor", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("master", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("bsc", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("msc", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("phd", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("doctor", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("diploma", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("ing.", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("bc.", StringComparison.OrdinalIgnoreCase)
			|| line.StartsWith("BSc ", StringComparison.OrdinalIgnoreCase)
			|| line.StartsWith("MSc ", StringComparison.OrdinalIgnoreCase);
	}

	private static bool ShouldTreatAsSingleInstitution(IReadOnlyList<string> headerLines, string combined)
	{
		if (headerLines.Any(line => line.StartsWith("and ", StringComparison.OrdinalIgnoreCase)))
		{
			return true;
		}

		if (headerLines.Count >= 2 && LooksLikeExplicitDegreeLine(headerLines[0]))
		{
			if (headerLines.Count >= 3 && LooksLikeInstitutionName(headerLines[^1]))
			{
				return false;
			}

			if (headerLines.Count == 2 && IsStandaloneInstitutionLine(headerLines[1]))
			{
				return false;
			}
		}

		if (LooksLikeInstitutionName(combined))
		{
			return true;
		}

		if (headerLines.Count >= 2 && LooksLikeIncompleteInstitutionLine(headerLines[0]))
		{
			return true;
		}

		return false;
	}

	private static bool IsStandaloneInstitutionLine(string line)
	{
		return line.Contains("university", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("college", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("institute", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("academy", StringComparison.OrdinalIgnoreCase);
	}

	private static bool LooksLikeIncompleteInstitutionLine(string line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return false;
		}

		if (LooksLikeInstitutionName(line))
		{
			return false;
		}

		if (line.EndsWith(" of", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return System.Text.RegularExpressions.Regex.IsMatch(
			line,
			@"(?i)\b(high school|school|university|college|academy)\s+of\s+[\p{L}\s]+$")
			&& !System.Text.RegularExpressions.Regex.IsMatch(
				line,
				@"(?i)\b(engineering|training|technology|management|sciences|informatics)\b");
	}

	private static bool LooksLikeEducationDescriptionLine(string line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return false;
		}

		return line.Length > 80
			|| (line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 12
				&& !LooksLikeInstitutionName(line));
	}

	private static bool LooksLikeGarbageEducationEntry(EducationEntry entry)
	{
		if (entry.Degree.StartsWith("and ", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (entry.Institution.Equals("Engineering", StringComparison.OrdinalIgnoreCase)
			&& !entry.Degree.Contains("school", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return false;
	}

	private static IEnumerable<string> SplitEducationBlocks(string body)
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

	private static IReadOnlyList<string> MergeEducationContinuationBlocks(IReadOnlyList<string> blocks)
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

	private static bool StartsReVitaeEducationEntryLine(string[] lines, int index) =>
		index == 0 || LooksLikeEducationDegreeTitleLine(lines[index]);

	private static bool LooksLikeInstitutionFirstEducationHeader(IReadOnlyList<string> headerLines) =>
		headerLines.Count >= 2
		&& LooksLikeInstitutionName(headerLines[0])
		&& (LooksLikeEducationDegreeTitleLine(headerLines[1]) || LooksLikeExplicitDegreeLine(headerLines[1]));

	private static bool EducationBodyLooksInstitutionFirst(string body)
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

	private static bool EducationBodyLooksDegreeFirst(string[] lines)
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

	private static bool LooksLikeEducationDegreeTitleLine(string line)
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

	private static readonly Regex EducationDegreeTitleLine = new(
		@"^(MSc|BSc|PhD|MEng|MBA|Bachelor|Master|Doctor)\b",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static bool LooksLikeEducationContinuationBlock(string block, string previousBlock)
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

	private static bool HasEducationEntryAnchor(string block)
	{
		var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		return lines.Any(line => DateRangeParser.TryParse(line, out _))
			|| lines.Any(LooksLikeInstitutionName)
			|| lines.Any(LooksLikeIncompleteInstitutionLine);
	}

	private static string GetLastMeaningfulLine(string block)
	{
		return block
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.LastOrDefault() ?? string.Empty;
	}

	private static string InferDefaultDegreeLabel(string institutionOrTitle)
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

	private static void InferDegreeType(EducationEntry entry, string institutionOrTitle)
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

	private static bool LooksLikeLocationLine(string line)
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

	private static bool LooksLikeTechnologyList(string line)
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

	private static bool ContainsTechnologyListProseIndicators(string line)
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

	private static HashSet<string> CollectSidebarSkillTokens(string skillsBody)
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

	private static bool IsSidebarSkillLine(string line, IReadOnlySet<string> sidebarSkillTokens)
	{
		return sidebarSkillTokens.Contains(line.Trim());
	}

	private static bool TrySkipSidebarSkillRun(
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

	private static bool IsLikelySidebarSkillBlockLine(string line, IReadOnlySet<string> sidebarSkillTokens)
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

	private static bool TryParseContactLocationLine(string line, out string location)
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

	private static bool LooksLikeCityCountryPair(string city, string country)
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

	private static bool LooksLikeSidebarSkillToken(string line)
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

	private static bool IsRepeatedJobTitleLine(string line, string jobTitle)
	{
		return !string.IsNullOrWhiteSpace(jobTitle)
			&& line.Equals(jobTitle, StringComparison.OrdinalIgnoreCase);
	}

	private static string MergeTechnologies(string existing, string additional)
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

	private static IEnumerable<string> SplitBlocks(string body)
	{
		return body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	private static IEnumerable<string> SplitCertificateBlocks(string body)
	{
		var lineBlocks = SplitLineBasedEntryBlocks(body, StartsCertificateEntryLine).ToList();
		return lineBlocks.Count > 1 ? lineBlocks : SplitBlocks(body);
	}

	private static IEnumerable<string> SplitProjectBlocks(string body)
	{
		var lineBlocks = SplitLineBasedEntryBlocks(body, StartsProjectEntryLine).ToList();
		return lineBlocks.Count > 1 ? lineBlocks : SplitBlocks(body);
	}

	private static bool StartsCertificateEntryLine(string[] lines, int index)
	{
		var line = lines[index];
		return index == 0
			|| line.StartsWith("Professional Certification #", StringComparison.OrdinalIgnoreCase)
			|| CertificateEntryHeader.IsMatch(line);
	}

	private static bool StartsProjectEntryLine(string[] lines, int index)
	{
		var line = lines[index];
		return index == 0
			|| ProjectEntryHeader.IsMatch(line);
	}

	private static IEnumerable<string> SplitLineBasedEntryBlocks(
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

	private static string MergeSplitExportMetaLines(string body)
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

	private static bool ShouldMergeExportMetaContinuation(string current, string next)
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

	private static string MergeSplitUrlLines(string text)
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

	private static string MergeSplitPersonalFieldLines(string text)
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

	private static bool ShouldMergeLabeledFieldContinuation(string current, string next)
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

	private static bool ShouldMergeUrlContinuation(string current, string next)
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

	private static bool IsReVitaeUrlLabelLine(string line) =>
		line.Contains("LinkedIn URL:", StringComparison.OrdinalIgnoreCase)
		|| line.Contains("GitHub URL:", StringComparison.OrdinalIgnoreCase)
		|| line.Contains("Portfolio URL:", StringComparison.OrdinalIgnoreCase);

	private static string[] CollectSegmentedPersonalLines(CvSegmentationResult segmentation)
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

	private static bool TryAssignReVitaeSidebarNameBeforeEmail(
		IReadOnlyList<string> lines,
		PersonalInformationImport personal,
		CvImportBuildContext context)
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

	private static void ApplyReVitaeExportHyperlinks(
		PersonalInformationImport personal,
		IReadOnlyList<string>? hyperlinkUrls,
		HashSet<string> assignedUrls,
		CvImportBuildContext context)
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

	private static bool ContainsPartialUrl(string line)
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

	private static string CollectSupplementalPersonalContactText(CvSegmentationResult segmentation)
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

	private static void CollectLabeledPersonalContactLines(string body, ICollection<string> collected)
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

	private static IEnumerable<string> ExtractInlineLabeledBlock(string body, string header)
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

	private static bool IsPersonalContactLabel(string label) =>
		label.Contains("location", StringComparison.OrdinalIgnoreCase)
		|| label.Contains("lokalita", StringComparison.OrdinalIgnoreCase)
		|| label.Contains("linkedin", StringComparison.OrdinalIgnoreCase)
		|| label.Contains("github", StringComparison.OrdinalIgnoreCase)
		|| label.Contains("portfolio", StringComparison.OrdinalIgnoreCase)
		|| label.Contains("website", StringComparison.OrdinalIgnoreCase)
		|| label.Contains("professional title", StringComparison.OrdinalIgnoreCase);

	private static string NormalizeImportedUrl(string value)
	{
		var trimmed = value.Trim();
		var match = CvImportPatterns.Url.Match(trimmed);
		return match.Success ? match.Value.Trim() : trimmed;
	}

	private static bool TryParseLabeledIssueDateLine(string line, out ParsedDateRange issueDate)
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

	private static bool TryParseLabeledExpirationDateLine(string line, out ParsedDateRange expirationDate)
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

	private static bool TryParseLabeledDateRangeLine(string line, out ParsedDateRange dateRange)
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

	private static readonly Regex CertificateEntryHeader = new(
		@"^Professional Certification\s+#\d+",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex ProjectEntryHeader = new(
		@"^Project\s+.+\s+[—-]\s+",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static IEnumerable<string> SplitWorkExperienceBlocks(string body)
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

	private static bool StartsWorkExperienceEntry(string[] lines, int index)
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

	private static bool LooksLikeReVitaeExportWorkEntry(string[] lines, int index)
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

	private static bool TryParseExportDelimitedMetaLine(
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

	private static bool LooksLikeWorkEntryHeader(string line)
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

	private static bool IsLikelyNameToken(string line)
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

	private static bool HeaderLooksLikeSummaryProse(IReadOnlyList<string> headerLines)
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

	private static string TrimReVitaeSkillBodyPrefix(string body)
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

	private static string MergeSplitReVitaeSkillLines(string body)
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

	private static bool TryDequeueStandaloneSkillProficiency(string line, out string proficiency)
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

	private static bool IsBareReVitaeSkillNameLine(string line)
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

	private static Queue<string> CollectOrphanWorkDateFragments(string headerBlock)
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

	private static bool TryCompletePartialWorkMetaLine(
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

	private static readonly Regex OrphanWorkDateFragment = new(
		@"^/\s*(?<startYear>\d{4})\s*[–-]\s*(?<endMonth>\d{1,2})\s*/\s*(?<endYear>\d{4})\s*$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static bool IsLikelyNamePart(string token)
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

	private static bool IsLikelyTechNameToken(string token) =>
		TechNameTokens.Contains(token);

	private static IEnumerable<string> SplitCommaList(string value)
	{
		return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	private static void SplitTitleCompany(string line, out string jobTitle, out string company)
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

	private static void TryAssignBestPersonNameFromLines(
		IReadOnlyList<string> lines,
		PersonalInformationImport personal,
		CvImportBuildContext context,
		CvImportConfidence confidence)
	{
		string? bestLine = null;
		var bestScore = int.MinValue;
		foreach (var line in lines)
		{
			if (!IsLikelyPersonNameLine(line))
			{
				continue;
			}

			var score = ScorePersonNameLine(line);
			if (score > bestScore)
			{
				bestScore = score;
				bestLine = line;
			}
		}

		if (bestLine is not null)
		{
			TryAssignPersonNameFromLine(bestLine, personal, context, confidence);
		}
	}

	private static int ScorePersonNameLine(string line)
	{
		var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var score = 0;
		foreach (var part in parts)
		{
			if (part.Length >= 4)
			{
				score += 4;
			}

			if (part.Any(c => c > 127))
			{
				score += 15;
			}

			if (part.Contains('-', StringComparison.Ordinal))
			{
				score -= 4;
			}

			if (IsSkillStopWord(part) || IsLikelyTechNameToken(part))
			{
				score -= 10;
			}
		}

		return score + Math.Min(line.Length, 40);
	}

	private static bool TryAssignPersonNameFromLine(
		string line,
		PersonalInformationImport personal,
		CvImportBuildContext context,
		CvImportConfidence confidence)
	{
		if (!IsLikelyPersonNameLine(line))
		{
			return false;
		}

		var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		personal.FirstName = parts[0];
		personal.LastName = string.Join(' ', parts.Skip(1));
		context.AddConfidence(MainPersonalInformationFieldKeys.FirstName, confidence);
		context.AddConfidence(MainPersonalInformationFieldKeys.LastName, confidence);
		return true;
	}

	private static void TryAssignNameFromSkillsSection(
		CvSegmentationResult segmentation,
		PersonalInformationImport personal,
		CvImportBuildContext context)
	{
		if (!segmentation.SectionBodies.TryGetValue(CvImportSectionId.Skills, out var skillsNameSource))
		{
			return;
		}

		var skillLines = skillsNameSource.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		TryAssignBestPersonNameFromLines(skillLines, personal, context, CvImportConfidence.Medium);
		if (!string.IsNullOrWhiteSpace(personal.FirstName))
		{
			return;
		}

		TryAssignSplitPersonNameFromLines(skillLines, personal, context, CvImportConfidence.Medium);
	}

	private static void TryAssignNameFromOtherSections(
		CvSegmentationResult segmentation,
		PersonalInformationImport personal,
		CvImportBuildContext context)
	{
		CvImportSectionId[] searchOrder =
		[
			CvImportSectionId.Contact,
			CvImportSectionId.Skills,
			CvImportSectionId.WorkExperience,
			CvImportSectionId.Summary,
		];

		foreach (var sectionId in searchOrder)
		{
			if (!segmentation.SectionBodies.TryGetValue(sectionId, out var body))
			{
				continue;
			}

			var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			var candidateLines = SelectNameSearchLines(sectionId, lines);
			TryAssignBestPersonNameFromLines(candidateLines, personal, context, CvImportConfidence.Low);
			if (!string.IsNullOrWhiteSpace(personal.FirstName))
			{
				context.Warnings.Add(new CvImportWarning(TranslationKeys.ImportWarningNameUncertain));
				return;
			}

			TryAssignSplitPersonNameFromLines(candidateLines, personal, context, CvImportConfidence.Low);
			if (!string.IsNullOrWhiteSpace(personal.FirstName))
			{
				context.Warnings.Add(new CvImportWarning(TranslationKeys.ImportWarningNameUncertain));
				return;
			}
		}
	}

	private static string[] SelectNameSearchLines(CvImportSectionId sectionId, string[] lines) =>
		sectionId switch
		{
			CvImportSectionId.WorkExperience or CvImportSectionId.Skills or CvImportSectionId.Contact => lines,
			_ => lines.Take(25).ToArray(),
		};

	private static bool TryAssignSplitPersonNameFromLines(
		IReadOnlyList<string> lines,
		PersonalInformationImport personal,
		CvImportBuildContext context,
		CvImportConfidence confidence)
	{
		for (var index = 0; index < lines.Count - 1; index++)
		{
			var first = lines[index];
			var second = lines[index + 1];
			if (!IsLikelyNamePart(first)
				|| !IsLikelyNamePart(second)
				|| CvImportPatterns.Email.IsMatch(first)
				|| CvImportPatterns.Email.IsMatch(second))
			{
				continue;
			}

			personal.FirstName = first;
			personal.LastName = second;
			context.AddConfidence(MainPersonalInformationFieldKeys.FirstName, confidence);
			context.AddConfidence(MainPersonalInformationFieldKeys.LastName, confidence);
			return true;
		}

		return false;
	}

	private static bool IsPlausibleStandaloneSkillToken(string line)
	{
		var token = line.Trim();
		if (!IsPlausibleSkillName(token))
		{
			return false;
		}

		if (token.Contains('.', StringComparison.Ordinal)
			|| token.Contains('#', StringComparison.Ordinal)
			|| token.Contains('+', StringComparison.Ordinal)
			|| token.Contains('/', StringComparison.Ordinal))
		{
			return true;
		}

		return token.Length <= 16 && !token.Contains(' ', StringComparison.Ordinal);
	}

	private static bool IsLikelyPersonNameLine(string line)
	{
		if (string.IsNullOrWhiteSpace(line) || line.Length > 60)
		{
			return false;
		}

		if (CvImportPatterns.Email.IsMatch(line)
			|| CvImportPatterns.Url.IsMatch(line)
			|| CvImportPatterns.Phone.IsMatch(line)
			|| DateRangeParser.TryParse(line, out _))
		{
			return false;
		}

		if (Regex.IsMatch(line, @"\b(Developer|Engineer|Manager|Director|years|experience|Full|Stack|Senior|Junior|Privileged|Access|Management|Platform|Copilot|Excalibur|OAuth|Cybersecurity|Microservices|integrating|Designed|Developed|Contributed|Implemented|Delivered|Building|frontend|backend|product|standards|technical|authentication)\b", RegexOptions.IgnoreCase))
		{
			return false;
		}

		var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (parts.Length is < 2 or > 4)
		{
			return false;
		}

		return parts.All(IsLikelyNamePart);
	}

	private static readonly HashSet<string> TechNameTokens = new(StringComparer.OrdinalIgnoreCase)
	{
		"TypeScript",
		"JavaScript",
		"Redis",
		"React",
		"ReactJS",
		"React-Native",
		"NestJS",
		"Node",
		"NodeJS",
		"Node.js",
		"PostgreSQL",
		"Docker",
		"Go",
		"PHP",
		"OAuth",
		"Copilot",
		"Excalibur",
		"Nette",
		"JQuery",
		"Next.js",
		"SharePoint",
		"Android",
		"Azure",
		"Frontend",
		"Backend",
		"Senior",
		"Junior",
		"Medior",
		"General",
		"Work",
		"Contact",
		"Design",
		"Built",
		"Developed",
		"Implemented",
		"Contributed",
		"Collaborated",
		"Delivered",
		"Integrated",
		"Worked",
		"Project",
		"Platforms",
		"Microservices",
		"Cybersecurity",
	};

	private static void RemovePersonNameSkillGroups(List<SkillsGroupEntry> groups)
	{
		groups.RemoveAll(group =>
			IsLikelyPersonNameLine(group.Category)
			&& group.Skills.Count == 0);
	}

	private static bool IsPlausibleSkillName(string value)
	{
		var skillName = value.Trim();
		if (skillName.Length is < 2 or > 48)
		{
			return false;
		}

		if (CvImportPatterns.Email.IsMatch(skillName)
			|| CvImportPatterns.Url.IsMatch(skillName)
			|| CvImportPatterns.Phone.IsMatch(skillName)
			|| DateRangeParser.TryParse(skillName, out _))
		{
			return false;
		}

		var words = skillName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (words.Length > 4)
		{
			return false;
		}

		if (words.Length == 1 && IsSkillStopWord(words[0]))
		{
			return false;
		}

		if (words.Length == 2
			&& char.IsUpper(words[0][0])
			&& char.IsUpper(words[1][0])
			&& !skillName.Contains('.', StringComparison.Ordinal)
			&& !skillName.Contains('#', StringComparison.Ordinal)
			&& !skillName.Contains('+', StringComparison.Ordinal))
		{
			return false;
		}

		return !Regex.IsMatch(
			skillName,
			@"\b(and|with|for|the|to|in|on|of|as|at|by|from|into|leading|worked|built|uses|uses|delivered|integrated|designed|implemented|contributed|collaborated|developed|experienced|strong|senior|full|stack|developer|project|technologies|contact|email|phone|location)\b",
			RegexOptions.IgnoreCase);
	}

	private static bool IsSkillStopWord(string token) =>
		token.Equals("and", StringComparison.OrdinalIgnoreCase)
		|| token.Equals("or", StringComparison.OrdinalIgnoreCase)
		|| token.Equals("with", StringComparison.OrdinalIgnoreCase)
		|| token.Equals("for", StringComparison.OrdinalIgnoreCase)
		|| token.Equals("the", StringComparison.OrdinalIgnoreCase)
		|| token.Equals("as", StringComparison.OrdinalIgnoreCase);

	private static SkillsGroupEntry EnsureDefaultSkillsGroup(List<SkillsGroupEntry> groups)
	{
		const string defaultCategory = "General";
		var existing = groups.FirstOrDefault(group => group.Category.Equals(defaultCategory, StringComparison.OrdinalIgnoreCase));
		if (existing is not null)
		{
			return existing;
		}

		var created = new SkillsGroupEntry { Category = defaultCategory };
		groups.Add(created);
		return created;
	}

	private static bool IsLanguageProficiencySubline(string line)
	{
		var labeled = CvImportPatterns.LabeledValue.Match(line.Trim());
		if (!labeled.Success)
		{
			return false;
		}

		var label = labeled.Groups["label"].Value.Trim();
		return label.Equals("Reading", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Writing", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Speaking", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Listening", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Translation", StringComparison.OrdinalIgnoreCase);
	}

	private static void MapLanguageProficiency(string token, LanguageEntry entry, CvImportBuildContext context)
	{
		token = token.Trim().Trim('(', ')');
		if (Enum.TryParse<CefrLevel>(token, ignoreCase: true, out var cefr))
		{
			entry.CefrLevel = cefr;
			context.AddConfidence(LanguagesFieldKeys.Build(entry.Id, LanguagesFieldKeys.CefrLevel), CvImportConfidence.High);
			return;
		}

		entry.Proficiency = token.ToLowerInvariant() switch
		{
			"native" or "mother tongue" => LanguageProficiency.Native,
			"fluent" or "full professional" => LanguageProficiency.Fluent,
			"advanced" or "upper intermediate" => LanguageProficiency.Advanced,
			"intermediate" or "working proficiency" => LanguageProficiency.Intermediate,
			"elementary" or "basic" or "beginner" => LanguageProficiency.Elementary,
			_ => LanguageProficiency.Intermediate
		};
		context.AddConfidence(LanguagesFieldKeys.Build(entry.Id, LanguagesFieldKeys.Proficiency), CvImportConfidence.Medium);
	}

	private static string InferLabelFromUrl(string url)
	{
		if (url.Contains("behance.net", StringComparison.OrdinalIgnoreCase))
		{
			return "Behance";
		}

		if (url.Contains("orcid.org", StringComparison.OrdinalIgnoreCase))
		{
			return "ORCID";
		}

		if (url.Contains("stackoverflow.com", StringComparison.OrdinalIgnoreCase))
		{
			return "Stack Overflow";
		}

		if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
		{
			var host = uri.Host.Replace("www.", string.Empty, StringComparison.OrdinalIgnoreCase);
			var label = host.Split('.')[0];
			return char.ToUpperInvariant(label[0]) + label[1..];
		}

		return "Link";
	}

	private static string NormalizeImportedBoundedText(string text, int maxLength)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}

		var normalized = text.Trim();
		return normalized.Length <= maxLength ? normalized : normalized[..maxLength].TrimEnd();
	}

	private static string CollapseRepeatedImportParagraphs(string text)
	{
		var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (paragraphs.Length <= 1)
		{
			return CollapseRepeatedImportSentence(text);
		}

		var unique = new List<string>();
		foreach (var paragraph in paragraphs)
		{
			if (unique.Count == 0 || !unique[^1].Equals(paragraph, StringComparison.Ordinal))
			{
				unique.Add(paragraph);
			}
		}

		return string.Join("\n\n", unique);
	}

	private static string CollapseRepeatedImportSentence(string text)
	{
		var trimmed = text.Trim();
		if (trimmed.Length < 160)
		{
			return trimmed;
		}

		var maxUnit = Math.Min(400, trimmed.Length / 2);
		for (var unitLength = maxUnit; unitLength >= 80; unitLength--)
		{
			var unit = trimmed[..unitLength].TrimEnd();
			if (string.IsNullOrWhiteSpace(unit))
			{
				continue;
			}

			var remainder = trimmed[unitLength..].Replace(unit, string.Empty, StringComparison.Ordinal).Trim();
			if (remainder.Length <= unit.Length / 2)
			{
				return unit;
			}
		}

		return trimmed;
	}
}
