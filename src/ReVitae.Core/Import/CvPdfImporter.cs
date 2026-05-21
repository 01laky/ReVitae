using ReVitae.Core.Import.Pdf;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import;

public sealed class CvPdfImporter
{
    private readonly IPdfTextExtractor _pdfTextExtractor;

    public CvPdfImporter()
        : this(new PdfPigTextExtractor())
    {
    }

    public CvPdfImporter(IPdfTextExtractor pdfTextExtractor)
    {
        _pdfTextExtractor = pdfTextExtractor;
    }

    public CvImportResult ImportFromPdf(string filePath)
    {
        var extraction = _pdfTextExtractor.Extract(filePath);
        if (!extraction.Success)
        {
            return CvImportResult.Failed(extraction.ErrorMessageKey ?? TranslationKeys.ImportErrorUnreadablePdf);
        }

        return ImportFromText(extraction.Text);
    }

    public CvImportResult ImportFromText(string rawText)
    {
        var normalized = CvTextNormalizer.Normalize(rawText);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorEmptyPdf);
        }

        var segmentation = CvSectionSegmenter.Segment(normalized);
        var result = CvImportFieldExtractor.Extract(segmentation);
        if (!result.Success)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorNoStructuredData);
        }

        return result;
    }
}
