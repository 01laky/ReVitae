namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using ReVitae.Core.Export.Pdf;

internal static class CenteredMinimalPdfTemplate
{
	public static byte[] Render(CvExportDocument document)
	{
		return CvPdfRenderHelper.RenderPage(document, page =>
		{
			page.Content().Column(column =>
			{
				column.Spacing(14);
				column.Item().AlignCenter().Text(document.FullName).FontSize(28).Bold();
				column.Item().Background("#E0E0E0").CornerRadius(12).Padding(16)
					.Text(CvExportPreviewContentBuilder.BuildSummary(document)).FontSize(CvPdfLayoutHelpers.BaseFontSize);
				column.Item().Background("#212121").CornerRadius(12).Padding(10).AlignCenter()
					.Text(BuildContactInline(document)).FontSize(CvPdfLayoutHelpers.BaseFontSize).FontColor(Colors.White);
				CvPdfExtendedHelpers.ComposeCenteredSection(column, document.Labels.PreviewWorkExperience,
					CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
				CvPdfExtendedHelpers.ComposeCenteredSection(column, document.Labels.PreviewSkills,
					CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
				CvPdfExtendedHelpers.ComposeCenteredSection(column, document.Labels.PreviewEducation,
					CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));
			});
		});
	}

	private static string BuildContactInline(CvExportDocument document)
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

		return string.Join("  ", parts);
	}
}
