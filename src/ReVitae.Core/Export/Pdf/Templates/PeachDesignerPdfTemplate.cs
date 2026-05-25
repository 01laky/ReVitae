namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using ReVitae.Core.Export.Pdf;

internal static class PeachDesignerPdfTemplate
{
	private const string Peach = "#E9B083";

	public static byte[] Render(CvExportDocument document)
	{
		return CvPdfRenderHelper.Generate(document, container =>
		{
			container.Page(page =>
			{
				CvPdfLayoutHelpers.ConfigureA4Page(page);
				page.Content().Column(root =>
				{
					root.Spacing(10);
					root.Item().Row(header =>
					{
						header.ConstantItem(72).Element(c =>
							CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 68, Peach, "#FFFFFF", circular: true));
						header.RelativeItem().Background(Peach).CornerRadius(16).Padding(14).Column(contact =>
						{
							contact.Item().Text(document.FullName.ToUpperInvariant()).FontSize(22).Bold();
							CvPdfExtendedHelpers.ComposeContactLine(contact.Item(), document);
						});
					});
					root.Item().Row(body =>
					{
						body.RelativeItem(34).Background("#E5E5E5").CornerRadius(16).Padding(12).Column(sidebar =>
						{
							CvPdfLayoutHelpers.ComposeSection(sidebar, document.Labels.Summary,
								CvExportPreviewContentBuilder.BuildSummary(document));
							CvPdfLayoutHelpers.ComposeSection(sidebar, document.Labels.PreviewSkills,
								CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
						});
						body.RelativeItem(66).PaddingLeft(10).Column(main =>
						{
							CvPdfSectionContent.ComposeWorkExperienceOnly(main, document);
							CvPdfSectionContent.ComposeEducationPublic(main, document);
						});
					});
				});
			});
		});
	}
}
