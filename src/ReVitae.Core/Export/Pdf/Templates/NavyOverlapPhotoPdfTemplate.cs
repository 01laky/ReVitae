namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using ReVitae.Core.Export.Pdf;

internal static class NavyOverlapPhotoPdfTemplate
{
	private const string Navy = "#1E3A5F";

	public static byte[] Render(CvExportDocument document)
	{
		return CvPdfRenderHelper.RenderPage(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Spacing(8);
				root.Item().Row(headerBand =>
				{
					headerBand.RelativeItem().Height(72).Background(Navy);
					headerBand.ConstantItem(72).Element(c =>
						CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 72, "#CCCCCC", Navy));
				});
				root.Item().PaddingHorizontal(20).Column(info =>
				{
					info.Item().Text(document.FullName).FontSize(24).Bold();
					info.Item().Text($"{document.Phone}    {document.Email}");
				});
				root.Item().PaddingTop(20).PaddingHorizontal(20).Text(text =>
				{
					text.Span(document.ProfessionalTitle + " ").Bold();
					text.Span(CvExportPreviewContentBuilder.BuildSummary(document));
				});
				root.Item().PaddingHorizontal(20).Row(body =>
				{
					body.RelativeItem(64).Column(left =>
					{
						CvPdfLayoutHelpers.ComposeSection(left, document.Labels.PreviewWorkExperience,
							CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document), Navy);
					});
					body.RelativeItem(36).PaddingLeft(10).Column(right =>
					{
						CvPdfLayoutHelpers.ComposeSection(right, document.Labels.PreviewSkills,
							CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document), Navy);
						CvPdfSectionContent.ComposeEducationPublic(right, document);
					});
				});
			});
		});
	}
}
