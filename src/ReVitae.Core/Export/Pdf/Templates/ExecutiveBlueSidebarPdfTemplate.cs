namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using ReVitae.Core.Export.Pdf;

internal static class ExecutiveBlueSidebarPdfTemplate
{
	private const string Navy = "#1E3A5F";

	public static byte[] Render(CvExportDocument document)
	{
		return CvPdfRenderHelper.RenderPage(document, page =>
		{

			// Full-height grey sidebar band via page background so it reaches the bottom of every page.
			page.Margin(0);
			page.Background().Row(bg =>
			{
				bg.RelativeItem(34).Background("#E5E5E5");
				bg.RelativeItem(66);
			});

			page.Content().Row(row =>
			{
				row.RelativeItem(34).Column(sidebar =>
				{
					sidebar.Item().Height(8).Background(Navy);
					sidebar.Item().Padding(14).Column(sidebarBody =>
					{
						sidebarBody.Spacing(12);
						sidebarBody.Item().Text(document.FullName.ToUpperInvariant()).FontSize(16).Bold().FontColor(Navy);
						sidebarBody.Item().Element(c =>
							CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 72, Navy, Colors.White));
						CvPdfLayoutHelpers.ComposeSection(sidebarBody, document.Labels.Contact,
							CvExportPreviewContentBuilder.BuildContactLines(document), Navy);
						CvPdfExtendedHelpers.ComposeOptionalSection(sidebarBody, document.Labels.PreviewLanguages,
							CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document), false);
					});
				});
				row.RelativeItem(66).PaddingVertical(24).PaddingLeft(16).PaddingRight(24).Column(main =>
				{
					CvPdfExtendedHelpers.ComposeMainSections(main, document, document.Labels.Summary);
				});
			});
		});
	}
}
