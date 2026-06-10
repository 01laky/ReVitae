namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Export.Pdf;

internal static class CleanTopHeaderPdfTemplate
{
	public static byte[] Render(CvExportDocument document)
	{
		var hasPhoto = ProfilePhotoStorage.FileExists(document.PhotoPath);

		return CvPdfRenderHelper.RenderPage(document, page =>
		{

			page.Content().Column(content =>
			{
				content.Spacing(16);
				content.Item().Background("#5A9BD5").Padding(20).Row(header =>
				{
					if (hasPhoto)
					{
						header.ConstantItem(72).Element(container =>
							CvPdfPhotoHelpers.ComposeHeaderPhoto(container, document, 64));
					}

					header.RelativeItem(55).Column(name =>
					{
						name.Spacing(5);
						name.Item().Text(document.FullName).FontSize(28).Bold().FontColor(Colors.White);
						name.Item().Text(document.ProfessionalTitle).FontSize(13).SemiBold().FontColor(Colors.White);
					});

					header.RelativeItem(45).Column(contact =>
					{
						contact.Spacing(2);
						contact.Item().Text($"{document.Labels.Email}: {document.Email}").FontSize(10).SemiBold().FontColor(Colors.White);
						contact.Item().Text($"{document.Labels.Phone}: {document.Phone}").FontSize(10).SemiBold().FontColor(Colors.White);
						contact.Item().Text($"{document.Labels.Location}: {document.Location}").FontSize(10).SemiBold().FontColor(Colors.White);
					});
				});

				content.Item().Column(sections =>
				{
					CvPdfSectionContent.ComposeAllSections(
						sections,
						document,
						document.Labels.Summary,
						document.Labels.Links,
						CvExportPreviewContentBuilder.BuildLinksLines(document));
				});
			});
		});
	}
}
