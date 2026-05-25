using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Pdf;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Import.Fixtures.JohnDoe;

public static class JohnDoeVariantGenerator
{
    public static GeneratedJohnDoeVariantFile Generate(JohnDoeVariantSpec spec)
    {
        var document = JohnDoeStressCvDataset.CreateDocument(spec.PdfTemplate ?? CvExportTemplateId.ModernSidebar);

        return spec.Kind switch
        {
            JohnDoeVariantKind.PdfTemplate => GeneratePdf(spec, document),
            JohnDoeVariantKind.DeferredSidebarPdf => GenerateDeferredSidebarPdf(spec),
            JohnDoeVariantKind.PlainTextProfile => GeneratePlainText(spec, document),
            JohnDoeVariantKind.MarkdownExport => GenerateExported(spec, document, CvExportFormat.Markdown),
            JohnDoeVariantKind.HtmlExport => GenerateExported(spec, document, CvExportFormat.Html),
            JohnDoeVariantKind.DocxExport => GenerateExported(spec, document, CvExportFormat.Docx),
            _ => throw new InvalidOperationException($"Unsupported John Doe variant kind: {spec.Kind}")
        };
    }

    private static GeneratedJohnDoeVariantFile GeneratePdf(JohnDoeVariantSpec spec, CvExportDocument document)
    {
        var bytes = new QuestPdfCvExporter().Export(document);
        return GeneratedJohnDoeVariantFile.Write(spec, bytes);
    }

    private static GeneratedJohnDoeVariantFile GenerateDeferredSidebarPdf(JohnDoeVariantSpec spec)
    {
        var bytes = SidebarLayoutPdfWriter.Create(SidebarLayoutPdfWriter.CreateDeferredSidebarStressLayout());
        return GeneratedJohnDoeVariantFile.Write(spec, bytes);
    }

    private static GeneratedJohnDoeVariantFile GeneratePlainText(JohnDoeVariantSpec spec, CvExportDocument document)
    {
        var profile = spec.TextProfile ?? JohnDoeTextFormattingProfile.DefaultReVitae;
        var text = JohnDoeTextVariantRenderer.Render(document, profile);
        return GeneratedJohnDoeVariantFile.WriteText(spec, text);
    }

    private static GeneratedJohnDoeVariantFile GenerateExported(
        JohnDoeVariantSpec spec,
        CvExportDocument document,
        CvExportFormat format)
    {
        using var stream = new MemoryStream();
        var source = new CvExportSourceData(
            new PersonalInformationImport(),
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            null);
        var result = CvDocumentExporter.Export(document, source, format, stream);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to export John Doe variant {spec.Id} as {format}.");
        }

        if (spec.TextProfile == JohnDoeTextFormattingProfile.MarkdownHeadings)
        {
            var markdown = JohnDoeTextVariantRenderer.Render(document, JohnDoeTextFormattingProfile.MarkdownHeadings);
            return GeneratedJohnDoeVariantFile.WriteText(spec, markdown);
        }

        return GeneratedJohnDoeVariantFile.Write(spec, stream.ToArray());
    }
}
