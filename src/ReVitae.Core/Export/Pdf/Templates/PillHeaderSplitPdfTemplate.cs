namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using ReVitae.Core.Export.Pdf;

internal static class PillHeaderSplitPdfTemplate
{
	public static byte[] Render(CvExportDocument document)
	{
		return CvPdfRenderHelper.RenderPage(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Spacing(10);
				root.Item().Row(header =>
				{
					header.ConstantItem(72).Element(c =>
						CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 68, "#E9967A", CvPdfPalette.White));
					header.RelativeItem().PaddingLeft(10).Background(CvPdfPalette.MutedOnDark).CornerRadius(20).Padding(14).Column(pill =>
					{
						pill.Item().Text(document.FullName).FontSize(22).Bold();
						pill.Item().Text($"{document.Phone}  {document.Email}  {document.LinkedInUrl}").FontColor("#E9967A");
					});
				});
				root.Item().Text(CvExportPreviewContentBuilder.BuildSummary(document));
				root.Item().Row(body =>
				{
					body.RelativeItem(34).Background(CvPdfPalette.MutedOnDark).CornerRadius(16).Padding(12).Column(left =>
					{
						CvPdfLayoutHelpers.ComposeSection(left, document.Labels.PreviewSkills,
							CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
					});
					body.RelativeItem(66).PaddingLeft(10).Column(right =>
					{
						CvPdfSectionContent.ComposeWorkExperienceOnly(right, document);
						CvPdfSectionContent.ComposeEducationPublic(right, document);
					});
				});
			});
		});
	}
}
