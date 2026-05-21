namespace ReVitae.Core.Import.Pdf;

public interface IPdfTextExtractor
{
    PdfTextExtractionResult Extract(string filePath);
}
