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
	internal static void AddEntryConfidences(
		ImportSectionExtractionContext context,
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

	internal static IReadOnlyDictionary<CvImportSectionId, bool> BuildSectionHasData(
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

	internal static bool HasStructuredData(
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

	internal static string GetBody(CvSegmentationResult segmentation, CvImportSectionId sectionId)
	{
		return segmentation.SectionBodies.TryGetValue(sectionId, out var body) ? body : string.Empty;
	}

	internal static void NormalizeWorkExperienceDates(WorkExperienceEntry entry)
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

	internal static void TryApplyPresentWorkDatesFromHeaderLines(
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

	internal static void TryParseLeadingWorkExperienceMetadata(
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

	internal static bool TryConsumeLeadingDateSection(
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

	internal static void AssignEducationHeader(EducationEntry entry, IReadOnlyList<string> headerLines)
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

	internal static void ApplyEducationDateRange(EducationEntry entry, ParsedDateRange range)
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

	internal static bool InferMissingEducationStartDate(EducationEntry entry)
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

	internal static void ApplyCertificateIssueDate(CertificateEntry entry, ParsedDateRange? range)
	{
		if (range is null)
		{
			return;
		}

		entry.IssueMonth = range.StartMonth ?? range.EndMonth;
		entry.IssueYear = range.StartYear ?? range.EndYear;
	}

	internal static void ApplyCertificateExpirationDate(CertificateEntry entry, ParsedDateRange? range)
	{
		if (range is null)
		{
			return;
		}

		entry.ExpirationMonth = range.StartMonth ?? range.EndMonth;
		entry.ExpirationYear = range.StartYear ?? range.EndYear;
	}

	internal static void ApplyProjectDateRange(ProjectEntry entry, ParsedDateRange range)
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

	internal static bool TrySplitDegreeInstitution(string line, out string degree, out string institution)
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

	internal static bool LooksLikeInstitutionName(string line)
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

	internal static bool LooksLikeExplicitDegreeLine(string line)
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

	internal static bool ShouldTreatAsSingleInstitution(IReadOnlyList<string> headerLines, string combined)
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

	internal static bool IsStandaloneInstitutionLine(string line)
	{
		return line.Contains("university", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("college", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("institute", StringComparison.OrdinalIgnoreCase)
			|| line.Contains("academy", StringComparison.OrdinalIgnoreCase);
	}

	internal static bool LooksLikeIncompleteInstitutionLine(string line)
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

	internal static bool LooksLikeEducationDescriptionLine(string line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return false;
		}

		return line.Length > 80
			|| (line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 12
				&& !LooksLikeInstitutionName(line));
	}

	internal static bool LooksLikeGarbageEducationEntry(EducationEntry entry)
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

}
