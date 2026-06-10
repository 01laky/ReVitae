namespace ReVitae.Core.Export.Pdf;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public static class CvPdfExtendedHelpers
{
	public static void ComposeContactLine(IContainer container, CvExportDocument document, string textColor = CvPdfPalette.Black)
	{
		var parts = new List<string>();
		if (!string.IsNullOrWhiteSpace(document.Phone))
		{
			parts.Add(document.Phone);
		}

		if (!string.IsNullOrWhiteSpace(document.Email))
		{
			parts.Add(document.Email);
		}

		if (!string.IsNullOrWhiteSpace(document.PortfolioUrl))
		{
			parts.Add(document.PortfolioUrl);
		}

		if (parts.Count == 0 && !string.IsNullOrWhiteSpace(document.Location))
		{
			parts.Add(document.Location);
		}

		container.Text(string.Join("  ", parts)).FontSize(CvPdfLayoutHelpers.BaseFontSize).FontColor(textColor);
	}

	public static void ComposeCenteredSection(ColumnDescriptor column, string title, string content, string headingColor = CvPdfPalette.Black)
	{
		column.Item().AlignCenter().Column(section =>
		{
			section.Item().AlignCenter().Text(title).FontSize(14).SemiBold().FontColor(headingColor);
			section.Item().PaddingTop(4).LineHorizontal(1).LineColor(CvPdfPalette.AvatarNeutral);
			section.Item().PaddingTop(6).AlignCenter().Text(content).FontSize(CvPdfLayoutHelpers.BaseFontSize);
		});
	}

	public static void ComposeSidebarSections(ColumnDescriptor sidebar, CvExportDocument document, bool uppercaseHeadings = false)
	{
		sidebar.Spacing(12);
		ComposeOptionalSection(sidebar, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document), uppercaseHeadings);
		ComposeOptionalSection(sidebar, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document), uppercaseHeadings);
		ComposeOptionalSection(sidebar, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document), uppercaseHeadings);
		ComposeOptionalSection(sidebar, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document), uppercaseHeadings);
	}

	public static void ComposeMainSections(ColumnDescriptor column, CvExportDocument document, string summaryTitle)
	{
		column.Spacing(14);
		CvPdfLayoutHelpers.ComposeSection(column, summaryTitle, CvExportPreviewContentBuilder.BuildSummary(document));
		CvPdfSectionContent.ComposeWorkExperienceOnly(column, document);
		CvPdfSectionContent.ComposeEducationPublic(column, document);
		CvPdfSectionContent.ComposeSkillsPublic(column, document);
		CvPdfSectionContent.ComposeLanguagesPublic(column, document);
		CvPdfSectionContent.ComposeCertificatesPublic(column, document);
		CvPdfSectionContent.ComposeProjectsPublic(column, document);
		CvPdfSectionContent.ComposeAdditionalInformationPublic(column, document);
	}

	public static void ComposeOptionalSection(ColumnDescriptor column, string title, string content, bool uppercase)
	{
		if (string.IsNullOrWhiteSpace(content) || content == "-")
		{
			return;
		}

		if (uppercase)
		{
			CvPdfLayoutHelpers.ComposeUppercaseSection(column, title, content);
		}
		else
		{
			CvPdfLayoutHelpers.ComposeSection(column, title, content);
		}
	}

	public static void ComposeSkillDots(IContainer container, string skillName, int filledDots, int totalDots, string fillColor)
	{
		container.Row(row =>
		{
			row.RelativeItem().Text(skillName).FontSize(CvPdfLayoutHelpers.BaseFontSize);
			row.ConstantItem(90).Row(dots =>
			{
				for (var i = 0; i < totalDots; i++)
				{
					dots.ConstantItem(7).Height(7).Background(i < filledDots ? fillColor : "#D8D8D8");
					if (i < totalDots - 1)
					{
						dots.ConstantItem(2);
					}
				}
			});
		});
	}
}
