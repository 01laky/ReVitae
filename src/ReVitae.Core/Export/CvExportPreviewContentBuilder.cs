namespace ReVitae.Core.Export;

public static class CvExportPreviewContentBuilder
{
	public static string BuildSummary(CvExportDocument document)
	{
		return string.IsNullOrWhiteSpace(document.ShortSummary) ? "-" : document.ShortSummary;
	}

	public static string BuildWorkExperiencePreviewContent(CvExportDocument document)
	{
		if (document.WorkExperienceEntries.Count == 0)
		{
			return string.Empty;
		}

		var entries = new List<string>();
		foreach (var entry in document.WorkExperienceEntries)
		{
			var block = new List<string>
			{
				entry.JobTitle,
				BuildWorkExperienceMetaLine(entry)
			};

			if (!string.IsNullOrWhiteSpace(entry.Description))
			{
				block.Add(entry.Description);
			}

			if (!string.IsNullOrWhiteSpace(entry.Achievements))
			{
				block.Add($"{document.Labels.PreviewAchievements}:{Environment.NewLine}{entry.Achievements}");
			}

			if (!string.IsNullOrWhiteSpace(entry.Technologies))
			{
				block.Add($"{document.Labels.PreviewTechnologies}: {entry.Technologies}");
			}

			if (!string.IsNullOrWhiteSpace(entry.CompanyUrl))
			{
				block.Add($"{document.Labels.WorkExperienceCompanyUrl}: {entry.CompanyUrl}");
			}

			entries.Add(string.Join(Environment.NewLine, block));
		}

		return string.Join($"{Environment.NewLine}{Environment.NewLine}", entries);
	}

	public static string BuildWorkExperienceMetaLine(WorkExperienceEntry entry)
	{
		var parts = new List<string> { entry.Company };
		if (!string.IsNullOrWhiteSpace(entry.Location) && entry.Location != "-")
		{
			parts.Add(entry.Location);
		}

		parts.Add(entry.EmploymentTypeLabel);
		parts.Add(entry.DateRange);
		return string.Join(" · ", parts);
	}

	public static string BuildEducationPreviewContent(CvExportDocument document)
	{
		if (document.EducationEntries.Count == 0)
		{
			return string.Empty;
		}

		var entries = new List<string>();
		foreach (var entry in document.EducationEntries)
		{
			var block = new List<string>
			{
				entry.Degree,
				BuildEducationMetaLine(entry)
			};

			if (!string.IsNullOrWhiteSpace(entry.FieldOfStudy))
			{
				block.Add($"{document.Labels.PreviewFieldOfStudy}: {entry.FieldOfStudy}");
			}

			if (!string.IsNullOrWhiteSpace(entry.Description))
			{
				block.Add(entry.Description);
			}

			if (!string.IsNullOrWhiteSpace(entry.Grade))
			{
				block.Add($"{document.Labels.PreviewGrade}: {entry.Grade}");
			}

			if (!string.IsNullOrWhiteSpace(entry.InstitutionUrl))
			{
				block.Add($"{document.Labels.EducationInstitutionUrl}: {entry.InstitutionUrl}");
			}

			entries.Add(string.Join(Environment.NewLine, block));
		}

		return string.Join($"{Environment.NewLine}{Environment.NewLine}", entries);
	}

	public static string BuildEducationMetaLine(EducationEntry entry)
	{
		var parts = new List<string> { entry.Institution };
		if (!string.IsNullOrWhiteSpace(entry.Location) && entry.Location != "-")
		{
			parts.Add(entry.Location);
		}

		parts.Add(entry.DegreeTypeLabel);
		parts.Add(entry.DateRange);
		return string.Join(" · ", parts);
	}

	public static string BuildSkillsPreviewContent(CvExportDocument document)
	{
		if (document.SkillsGroups.Count == 0)
		{
			return string.Empty;
		}

		var groups = new List<string>();
		foreach (var group in document.SkillsGroups)
		{
			var lines = new List<string> { group.Category };
			lines.AddRange(group.Skills.Select(skill => FormatSkillPreviewLine(skill, document.Labels)));
			groups.Add(string.Join(Environment.NewLine, lines));
		}

		return string.Join($"{Environment.NewLine}{Environment.NewLine}", groups);
	}

	public static string FormatSkillPreviewLine(SkillItem skill, CvExportSectionLabels labels)
	{
		var parts = new List<string> { skill.Name, skill.ProficiencyLabel };
		if (skill.YearsOfExperience is not null)
		{
			parts.Add($"{skill.YearsOfExperience} {labels.PreviewYearsSuffix}");
		}

		return string.Join(" · ", parts);
	}

	public static string BuildLanguagesPreviewContent(CvExportDocument document)
	{
		if (document.LanguageEntries.Count == 0)
		{
			return string.Empty;
		}

		var entries = new List<string>();
		foreach (var entry in document.LanguageEntries)
		{
			var block = new List<string> { entry.MainLine };
			block.AddRange(entry.SubSkillLines);
			entries.Add(string.Join(Environment.NewLine, block));
		}

		return string.Join($"{Environment.NewLine}{Environment.NewLine}", entries);
	}

	public static string BuildCertificatesPreviewContent(CvExportDocument document)
	{
		if (document.CertificateEntries.Count == 0)
		{
			return string.Empty;
		}

		var entries = new List<string>();
		foreach (var entry in document.CertificateEntries)
		{
			var block = new List<string> { entry.MainLine };
			block.AddRange(entry.DetailLines);
			entries.Add(string.Join(Environment.NewLine, block));
		}

		return string.Join($"{Environment.NewLine}{Environment.NewLine}", entries);
	}

	public static string BuildProjectsPreviewContent(CvExportDocument document)
	{
		if (document.ProjectEntries.Count == 0)
		{
			return string.Empty;
		}

		var entries = new List<string>();
		foreach (var entry in document.ProjectEntries)
		{
			var block = new List<string> { entry.MainLine };
			block.AddRange(entry.DetailLines);
			entries.Add(string.Join(Environment.NewLine, block));
		}

		return string.Join($"{Environment.NewLine}{Environment.NewLine}", entries);
	}

	public static string BuildCustomLinksPreviewContent(CvExportDocument document)
	{
		return document.CustomLinkLines.Count == 0
			? string.Empty
			: string.Join(Environment.NewLine, document.CustomLinkLines);
	}

	public static string BuildAdditionalInformationPreviewContent(CvExportDocument document)
	{
		return string.IsNullOrWhiteSpace(document.AdditionalInformationContent)
			? string.Empty
			: document.AdditionalInformationContent;
	}

	public static string BuildContactLines(CvExportDocument document)
	{
		return BuildLines(
			document.Labels.Email, document.Email,
			document.Labels.Phone, document.Phone,
			document.Labels.Location, document.Location);
	}

	public static string BuildContactLinksLines(CvExportDocument document)
	{
		return BuildOptionalLines(
			document.Labels.LinkedInUrl, document.LinkedInUrl,
			document.Labels.PortfolioUrl, document.PortfolioUrl,
			document.Labels.GitHubUrl, document.GitHubUrl);
	}

	public static string BuildDigitalLines(CvExportDocument document)
	{
		return BuildLines(
			document.Labels.PortfolioUrl, document.PortfolioUrl,
			document.Labels.GitHubUrl, document.GitHubUrl);
	}

	public static string BuildOnlineLines(CvExportDocument document)
	{
		return BuildContactLinksLines(document);
	}

	public static string BuildLinksLines(CvExportDocument document)
	{
		return BuildContactLinksLines(document);
	}

	public static string BuildLines(params string[] labelValuePairs)
	{
		var lines = new List<string>();
		for (var index = 0; index < labelValuePairs.Length; index += 2)
		{
			var label = labelValuePairs[index];
			var value = labelValuePairs[index + 1];
			if (!string.IsNullOrWhiteSpace(value) && value != "-")
			{
				lines.Add($"{label}: {value}");
			}
		}

		return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
	}

	private static string BuildOptionalLines(params string[] labelValuePairs)
	{
		var lines = new List<string>();
		for (var index = 0; index < labelValuePairs.Length; index += 2)
		{
			var label = labelValuePairs[index];
			var value = labelValuePairs[index + 1];
			if (!string.IsNullOrWhiteSpace(value) && value != "-")
			{
				lines.Add($"{label}: {value}");
			}
		}

		return lines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, lines);
	}
}
