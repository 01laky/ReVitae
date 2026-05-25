namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using ReVitae.Core.Export.Pdf;

internal static class ExecutiveBlueSidebarPdfTemplate
{
	private const string Navy = "#1E3A5F";

	public static byte[] Render(CvExportDocument document)
	{
		return CvPdfRenderHelper.Generate(document, container =>
		{
			container.Page(page =>
			{
				CvPdfLayoutHelpers.ConfigureA4Page(page);
				page.Content().Row(row =>
				{
					row.RelativeItem(34).Background("#E5E5E5").Column(sidebar =>
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
						sidebar.Item().Height(8).Background(Navy);
					});
					row.RelativeItem(66).PaddingLeft(12).Column(main =>
					{
						CvPdfExtendedHelpers.ComposeMainSections(main, document, document.Labels.Summary);
					});
				});
			});
		});
	}
}
