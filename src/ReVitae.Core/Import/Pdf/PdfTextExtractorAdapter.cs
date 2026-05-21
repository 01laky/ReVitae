using ReVitae.Core.Import.Extraction;

namespace ReVitae.Core.Import.Pdf;

/// <summary>Wraps legacy <see cref="IPdfTextExtractor"/> implementations as shared <see cref="ICvTextExtractor"/> instances.</summary>
public sealed class PdfTextExtractorAdapter(IPdfTextExtractor inner) : ICvTextExtractor
{
    public CvTextExtractionResult Extract(string filePath)
    {
        var legacy = inner.Extract(filePath);
        return new CvTextExtractionResult(
            legacy.Success,
            legacy.Text,
            legacy.ErrorMessageKey,
            legacy.HyperlinkUrls,
            Warnings: null,
            legacy.PageCount);
    }
}
