using System.Text;
using Encoding = System.Text.Encoding;
using ReVitae.Core.Localization;
using RtfPipe;

namespace ReVitae.Core.Import.Extraction;

public sealed class RtfTextExtractor : ICvTextExtractor
{
    static RtfTextExtractor()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public CvTextExtractionResult Extract(string filePath)
    {
        if (ImportExtractorGuards.TryRejectMissing(filePath, out var fail))
        {
            return fail;
        }

        try
        {
            var bytes = File.ReadAllBytes(filePath);
            var legacyText = Encoding.Latin1.GetString(bytes);
            var html = global::RtfPipe.Rtf.ToHtml(legacyText);
            return HtmlTextExtractor.FromHtmlMarkup(html);
        }
        catch (Exception)
        {
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
        }
    }
}
