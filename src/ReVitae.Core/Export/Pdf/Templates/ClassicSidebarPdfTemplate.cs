namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using ReVitae.Core.Export.Pdf;

internal static class ClassicSidebarPdfTemplate
{
	public static byte[] Render(CvExportDocument document)
	{
		return CvPdfRenderHelper.RenderPage(document, page =>
		{

			CvPdfLayoutHelpers.ComposeFullHeightSidebarPage(
				page,
				36,
				64,
				"#D8D8D8",
				sidebarOnLeft: true,
				sidebar =>
				{
					sidebar.Spacing(14);
					sidebar.Item().Element(container =>
						CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(
							container,
							document,
							88,
							CvPdfPalette.AvatarNeutral,
							CvPdfPalette.White));
					sidebar.Item().Text(document.FirstName).FontSize(24).Bold();
					sidebar.Item().Text(document.LastName).FontSize(24).Bold().FontColor("#F47C2C");

					CvPdfLayoutHelpers.ComposeSection(
						sidebar,
						document.Labels.Contact,
						CvExportPreviewContentBuilder.BuildContactLines(document));
				},
				content =>
				{
					CvPdfSectionContent.ComposeAllSections(
						content,
						document,
						document.Labels.Summary,
						document.Labels.ContactLinks,
						CvExportPreviewContentBuilder.BuildContactLinksLines(document));
				});
		});
	}
}
