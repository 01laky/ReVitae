using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Import.Importers;
using ReVitae.Core.Import.Pdf;

namespace ReVitae.Core.Import;

/// <summary>Backward-compatible helper that previously handled PDF‑only onboarding flows.</summary>
public sealed class CvPdfImporter
{
    private readonly PdfCvFormatImporter _pdf;

    public CvPdfImporter()
        : this(new Pdf.PdfPigTextExtractor())
    {
    }

    public CvPdfImporter(Pdf.IPdfTextExtractor extractor)
        : this(new PdfTextExtractorAdapter(extractor))
    {
    }

    internal CvPdfImporter(ICvTextExtractor pdfExtractor)
    {
        _pdf = new PdfCvFormatImporter(pdfExtractor);
    }

    public CvImportResult ImportFromPdf(string filePath)
    {
        return _pdf.Import(filePath);
    }

    public CvImportResult ImportFromText(string rawText, IReadOnlyList<string>? hyperlinkUrls = null)
    {
        return CvTextImportPipeline.Import(rawText, hyperlinkUrls);
    }
}
