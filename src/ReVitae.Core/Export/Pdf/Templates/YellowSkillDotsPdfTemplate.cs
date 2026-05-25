namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using ReVitae.Core.Export.Pdf;

internal static class YellowSkillDotsPdfTemplate
{
	private const string Yellow = "#F5C400";

	public static byte[] Render(CvExportDocument document)
	{
		return CvPdfRenderHelper.Generate(document, container =>
		{
			container.Page(page =>
			{
				CvPdfLayoutHelpers.ConfigureA4Page(page);
				page.Content().Row(row =>
				{
					row.RelativeItem(64).Column(main =>
					{
						main.Item().Row(titleRow =>
						{
							titleRow.ConstantItem(12).Height(12).Background(Yellow);
							titleRow.RelativeItem().PaddingLeft(8).Text(document.FullName).FontSize(24).Bold();
						});
						main.Item().PaddingTop(10).Column(body =>
						{
							CvPdfLayoutHelpers.ComposeSection(body, document.Labels.Summary,
								CvExportPreviewContentBuilder.BuildSummary(document));
							CvPdfSectionContent.ComposeWorkExperienceOnly(body, document);
							CvPdfSectionContent.ComposeEducationPublic(body, document);
						});
					});
					row.RelativeItem(36).PaddingLeft(10).Column(sidebar =>
					{
						sidebar.Item().Element(c => CvPdfPhotoHelpers.TryComposePhoto(c, document.PhotoPath, 96, circular: false));
						CvPdfLayoutHelpers.ComposeSection(sidebar, document.Labels.Contact,
							CvExportPreviewContentBuilder.BuildContactLines(document));
						sidebar.Item().PaddingTop(8).Text(document.Labels.PreviewSkills).SemiBold();
						var index = 0;
						foreach (var group in document.SkillsGroups)
						{
							foreach (var skill in group.Skills)
							{
								var filled = Math.Clamp(6 + index % 4, 4, 9);
								sidebar.Item().PaddingTop(4).Element(c =>
									CvPdfExtendedHelpers.ComposeSkillDots(c, skill.Name, filled, 10, Yellow));
								index++;
							}
						}
					});
				});
			});
		});
	}
}
