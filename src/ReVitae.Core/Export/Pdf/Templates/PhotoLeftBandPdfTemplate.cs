namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using ReVitae.Core.Export.Pdf;

internal static class PhotoLeftBandPdfTemplate
{
    public static byte[] Render(CvExportDocument document)
    {
        return CvPdfRenderHelper.Generate(document, container =>
        {
            container.Page(page =>
            {
                CvPdfLayoutHelpers.ConfigureA4Page(page);
                page.Content().Column(root =>
                {
                    root.Spacing(12);
                    root.Item().Row(header =>
                    {
                        header.ConstantItem(96).Element(c =>
                            CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 88, "#E67E22", "#FFFFFF"));
                        header.RelativeItem().PaddingLeft(12).Column(nameCol =>
                        {
                            nameCol.Item().Text(document.FullName).FontSize(24).Bold();
                            nameCol.Item().Text(document.ProfessionalTitle).FontSize(12).SemiBold();
                        });
                    });
                    root.Item().Background("#E8E8E8").Padding(12)
                        .Text(CvExportPreviewContentBuilder.BuildSummary(document));
                    root.Item().Row(body =>
                    {
                        body.RelativeItem(34).PaddingRight(10).Column(sidebar =>
                        {
                            CvPdfLayoutHelpers.ComposeSection(sidebar, document.Labels.Contact,
                                CvExportPreviewContentBuilder.BuildContactLines(document));
                            CvPdfExtendedHelpers.ComposeSidebarSections(sidebar, document);
                        });
                        body.RelativeItem(66).Column(main =>
                        {
                            CvPdfSectionContent.ComposeWorkExperienceOnly(main, document);
                            CvPdfSectionContent.ComposeProjectsPublic(main, document);
                            CvPdfSectionContent.ComposeAdditionalInformationPublic(main, document);
                        });
                    });
                });
            });
        });
    }
}
