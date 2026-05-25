namespace ReVitae.Core.Export.Pdf;

using QuestPDF.Fluent;

public static class CvPdfSectionContent
{
	public static void ComposeAllSections(
		ColumnDescriptor column,
		CvExportDocument document,
		string summarySectionTitle,
		string linksSectionTitle,
		string linksSectionContent)
	{
		column.Spacing(16);

		CvPdfLayoutHelpers.ComposeSection(
			column,
			summarySectionTitle,
			CvExportPreviewContentBuilder.BuildSummary(document));

		ComposeWorkExperience(column, document);
		ComposeEducation(column, document);
		ComposeSkills(column, document);
		ComposeLanguages(column, document);
		ComposeCertificates(column, document);
		ComposeProjects(column, document);
		ComposeCustomLinks(column, document);
		ComposeAdditionalInformation(column, document);
		ComposeLinks(column, linksSectionTitle, linksSectionContent);
	}

	public static void ComposeWorkExperienceOnly(ColumnDescriptor column, CvExportDocument document)
	{
		ComposeWorkExperience(column, document);
	}

	public static void ComposeEducationPublic(ColumnDescriptor column, CvExportDocument document)
	{
		ComposeEducation(column, document);
	}

	public static void ComposeProjectsPublic(ColumnDescriptor column, CvExportDocument document)
	{
		ComposeProjects(column, document);
	}

	public static void ComposeAdditionalInformationPublic(ColumnDescriptor column, CvExportDocument document)
	{
		ComposeAdditionalInformation(column, document);
	}

	public static void ComposeSkillsPublic(ColumnDescriptor column, CvExportDocument document)
	{
		ComposeSkills(column, document);
	}

	public static void ComposeLanguagesPublic(ColumnDescriptor column, CvExportDocument document)
	{
		ComposeLanguages(column, document);
	}

	public static void ComposeCertificatesPublic(ColumnDescriptor column, CvExportDocument document)
	{
		ComposeCertificates(column, document);
	}

	public static void ComposeCustomLinksPublic(ColumnDescriptor column, CvExportDocument document)
	{
		ComposeCustomLinks(column, document);
	}

	public static void ComposeLinksPublic(ColumnDescriptor column, CvExportDocument document)
	{
		ComposeLinks(column, document.Labels.Links, CvExportPreviewContentBuilder.BuildLinksLines(document));
	}

	private static void ComposeLinks(ColumnDescriptor column, string linksSectionTitle, string linksSectionContent)
	{
		if (string.IsNullOrWhiteSpace(linksSectionContent) || linksSectionContent == "-")
		{
			return;
		}

		CvPdfLayoutHelpers.ComposeSection(column, linksSectionTitle, linksSectionContent);
	}

	private static void ComposeWorkExperience(ColumnDescriptor column, CvExportDocument document)
	{
		if (document.WorkExperienceEntries.Count == 0)
		{
			return;
		}

		CvPdfLayoutHelpers.ComposeSection(
			column,
			document.Labels.PreviewWorkExperience,
			CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
	}

	private static void ComposeEducation(ColumnDescriptor column, CvExportDocument document)
	{
		if (document.EducationEntries.Count == 0)
		{
			return;
		}

		CvPdfLayoutHelpers.ComposeSection(
			column,
			document.Labels.PreviewEducation,
			CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));
	}

	private static void ComposeSkills(ColumnDescriptor column, CvExportDocument document)
	{
		if (document.SkillsGroups.Count == 0)
		{
			return;
		}

		CvPdfLayoutHelpers.ComposeSection(
			column,
			document.Labels.PreviewSkills,
			CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
	}

	private static void ComposeLanguages(ColumnDescriptor column, CvExportDocument document)
	{
		if (document.LanguageEntries.Count == 0)
		{
			return;
		}

		CvPdfLayoutHelpers.ComposeSection(
			column,
			document.Labels.PreviewLanguages,
			CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document));
	}

	private static void ComposeCertificates(ColumnDescriptor column, CvExportDocument document)
	{
		if (document.CertificateEntries.Count == 0)
		{
			return;
		}

		CvPdfLayoutHelpers.ComposeSection(
			column,
			document.Labels.PreviewCertificates,
			CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document));
	}

	private static void ComposeProjects(ColumnDescriptor column, CvExportDocument document)
	{
		if (document.ProjectEntries.Count == 0)
		{
			return;
		}

		CvPdfLayoutHelpers.ComposeSection(
			column,
			document.Labels.PreviewProjects,
			CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document));
	}

	private static void ComposeCustomLinks(ColumnDescriptor column, CvExportDocument document)
	{
		if (document.CustomLinkLines.Count == 0)
		{
			return;
		}

		CvPdfLayoutHelpers.ComposeSection(
			column,
			document.Labels.PreviewCustomLinks,
			CvExportPreviewContentBuilder.BuildCustomLinksPreviewContent(document));
	}

	private static void ComposeAdditionalInformation(ColumnDescriptor column, CvExportDocument document)
	{
		if (string.IsNullOrWhiteSpace(document.AdditionalInformationContent))
		{
			return;
		}

		CvPdfLayoutHelpers.ComposeSection(
			column,
			document.Labels.PreviewAdditionalInformation,
			CvExportPreviewContentBuilder.BuildAdditionalInformationPreviewContent(document));
	}
}
