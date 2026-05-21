using System;
using ReVitae.Core.Import.Extraction;

namespace ReVitae.Core.Import;

internal static class CvTextImportFlows
{
    public static CvImportResult FromExtractor(ICvTextExtractor extractor, string filePath)
    {
        CvTextExtractionResult extraction;
        try
        {
            extraction = extractor.Extract(filePath);
        }
        catch (Exception)
        {
            return CvImportResult.Failed(ReVitae.Core.Localization.TranslationKeys.ImportErrorUnreadableDocument);
        }

        if (!extraction.Success)
        {
            return CvImportResult.Failed(extraction.ErrorMessageKey ?? ReVitae.Core.Localization.TranslationKeys.ImportErrorUnreadableDocument);
        }

        return CvTextImportPipeline.Import(extraction.Text, extraction.HyperlinkUrls, extraction.Warnings);
    }
}
