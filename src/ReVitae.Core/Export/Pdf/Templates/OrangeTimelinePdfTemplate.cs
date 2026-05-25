namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using ReVitae.Core.Export.Pdf;

internal static class OrangeTimelinePdfTemplate
{
	private const string Orange = "#E67E22";

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
							CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 68, Orange, "#FFFFFF"));
						header.RelativeItem().PaddingLeft(10).Column(info =>
						{
							info.Item().Text(text =>
							{
								text.Span(document.FirstName.ToUpperInvariant() + " ").FontSize(22).Bold();
								text.Span(document.LastName.ToUpperInvariant()).FontSize(22).Bold().FontColor(Orange);
							});
							CvPdfExtendedHelpers.ComposeContactLine(info.Item(), document);
						});
					});
					root.Item().Row(timeline =>
					{
						timeline.ConstantItem(16).Column(line =>
						{
							line.Item().Width(2).Background(Orange);
						});
						timeline.RelativeItem().PaddingLeft(8).Column(content =>
						{
							ComposeTimelineSection(content, document.Labels.Summary,
								CvExportPreviewContentBuilder.BuildSummary(document));
							ComposeTimelineSection(content, document.Labels.PreviewSkills,
								CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
							ComposeTimelineSection(content, document.Labels.PreviewWorkExperience,
								CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
							ComposeTimelineSection(content, document.Labels.PreviewEducation,
								CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));
						});
					});
				});
			});
		});
	}

	private static void ComposeTimelineSection(ColumnDescriptor column, string title, string content)
	{
		if (string.IsNullOrWhiteSpace(content) || content == "-")
		{
			return;
		}

		column.Item().PaddingBottom(10).Column(section =>
		{
			section.Item().Text(title).SemiBold().FontColor(Orange);
			section.Item().Text(content);
		});
	}
}
