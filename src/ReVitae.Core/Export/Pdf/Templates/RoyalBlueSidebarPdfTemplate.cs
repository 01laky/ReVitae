namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using ReVitae.Core.Export.Pdf;

internal static class RoyalBlueSidebarPdfTemplate
{
	private const string Blue = "#4A76C0";
	private const string Header = "#333A45";

	public static byte[] Render(CvExportDocument document)
	{
		return CvPdfRenderHelper.Generate(document, container =>
		{
			container.Page(page =>
			{
				CvPdfLayoutHelpers.ConfigureA4Page(page, Blue);
				page.Content().Row(row =>
				{
					row.RelativeItem(34).Background(Blue).Padding(14).Column(sidebar =>
					{
						sidebar.Item().Text(document.Labels.Summary).Bold().FontColor(Colors.White);
						sidebar.Item().Text(CvExportPreviewContentBuilder.BuildSummary(document)).FontColor(Colors.White);
						sidebar.Item().PaddingTop(10).Text(document.Labels.PreviewSkills).Bold().FontColor(Colors.White);
						sidebar.Item().Text(CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document)).FontColor(Colors.White);
					});
					row.RelativeItem(66).Column(main =>
					{
						main.Item().Background(Header).Padding(14).Row(header =>
						{
							header.RelativeItem().Column(info =>
							{
								info.Item().Text(document.FullName).FontSize(22).Bold().FontColor(Colors.White);
								info.Item().Text(document.Location).FontColor(Colors.White);
								CvPdfExtendedHelpers.ComposeContactLine(info.Item(), document, Colors.White);
							});
							header.ConstantItem(72).Element(c =>
								CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 68, "#666666", Colors.White));
						});
						main.Item().Background(Colors.White).Padding(14).Column(body =>
						{
							CvPdfSectionContent.ComposeWorkExperienceOnly(body, document);
							CvPdfSectionContent.ComposeEducationPublic(body, document);
						});
					});
				});
			});
		});
	}
}
