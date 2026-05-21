using ReVitae.Core.Import.Importers;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import;

/// <summary>Imports CV files regardless of MIME type (routing, limits, standardized errors).</summary>
public static class CvDocumentImporter
{
    public static CvImportResult Import(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorFileNotFound);
        }

        long payloadLength;

        try
        {
            payloadLength = new FileInfo(filePath).Length;
        }
        catch (Exception)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);
        }

        if (payloadLength > CvImportLimits.MaxFileBytes)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorFileTooLarge);
        }

        var formatGuess = CvImportFormatDetector.DetectFormat(filePath);
        var driver = LocateImporter(formatGuess);
        if (driver is null)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnsupportedFormat);
        }

        CvImportResult response;

        try
        {
            response = driver.Import(filePath);
        }
        catch (Exception)
        {
            response = CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);
        }

        if (!response.Success)
        {
            return CvImportResult.Failed(NormalizeKey(response.ErrorMessageKey));
        }

        return response;
    }

    private static string NormalizeKey(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return TranslationKeys.ImportErrorUnreadableDocument;
        }

        if (string.Equals(key, TranslationKeys.ImportErrorEmptyPdf, StringComparison.Ordinal))
        {
            return TranslationKeys.ImportErrorEmptyDocument;
        }

        if (string.Equals(key, TranslationKeys.ImportErrorUnreadablePdf, StringComparison.Ordinal))
        {
            return TranslationKeys.ImportErrorUnreadableDocument;
        }

        return key;
    }

    private static ICvFormatImporter? LocateImporter(CvImportFormat blueprint)
        => blueprint switch
        {
            CvImportFormat.Pdf => new PdfCvFormatImporter(),
            CvImportFormat.Docx => new DocxCvFormatImporter(),
            CvImportFormat.Doc => new DocCvFormatImporter(),
            CvImportFormat.Odt => new OdtCvFormatImporter(),
            CvImportFormat.Rtf => new RtfCvFormatImporter(),
            CvImportFormat.PlainText => new PlainTextCvFormatImporter(),
            CvImportFormat.Markdown => new MarkdownCvFormatImporter(),
            CvImportFormat.Html => new HtmlCvFormatImporter(),
            CvImportFormat.Latex => new LatexCvFormatImporter(),
            CvImportFormat.Abw => new AbwCvFormatImporter(),
            CvImportFormat.Pages => new PagesCvFormatImporter(),
            CvImportFormat.Wps => new WpsCvFormatImporter(),
            CvImportFormat.JsonResume => new JsonResumeCvFormatImporter(),
            CvImportFormat.ReVitaeJson => new ReVitaeJsonCvFormatImporter(),
            CvImportFormat.YamlCv => new YamlCvFormatImporter(),
            CvImportFormat.CsvTabular => new TabularCvFormatImporter(),
            CvImportFormat.EuropassXml => new EuropassXmlCvFormatImporter(),
            CvImportFormat.HrXml => new HrXmlCvFormatImporter(),
            _ => null
        };
}
