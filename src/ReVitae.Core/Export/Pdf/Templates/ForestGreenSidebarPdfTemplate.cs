namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using ReVitae.Core.Export.Pdf;

internal static class ForestGreenSidebarPdfTemplate
{
	private const string Green = "#2F5D3A";

	public static byte[] Render(CvExportDocument document)
	{
		return CvPdfRenderHelper.Generate(document, container =>
		{
			container.Page(page =>
			{
				CvPdfLayoutHelpers.ConfigureA4Page(page);
				page.Content().Row(row =>
				{
					row.RelativeItem(34).Column(sidebar =>
					{
						sidebar.Item().Element(c => CvPdfPhotoHelpers.TryComposePhoto(c, document.PhotoPath, 96, circular: false));
						sidebar.Item().Background(Green).CornerRadius(12).Padding(12).Column(name =>
						{
							name.Item().Text(document.FullName).FontSize(18).Bold().FontColor(Colors.White);
						});
						sidebar.Item().Padding(12).Column(body =>
						{
							CvPdfLayoutHelpers.ComposeSection(
								body,
								document.Labels.Contact,
								CvExportPreviewContentBuilder.BuildContactLines(document));
						});
						sidebar.Item().Background(Green).Height(24).CornerRadius(12);
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
