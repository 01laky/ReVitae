using System.Text;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Extraction;

internal static class ImportExtractorGuards
{
    public static bool TryRejectMissing(string filePath, out CvTextExtractionResult failure)
    {
        failure = null!;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            failure = new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorFileNotFound);
            return true;
        }

        try
        {
            var attrs = File.GetAttributes(filePath);
            if (attrs.HasFlag(FileAttributes.Directory))
            {
                failure = new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
                return true;
            }
        }
        catch
        {
            failure = new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
            return true;
        }

        return false;
    }
}

public sealed class PlainTextExtractor : ICvTextExtractor
{
    public CvTextExtractionResult Extract(string filePath)
    {
        if (ImportExtractorGuards.TryRejectMissing(filePath, out var fail))
        {
            return fail;
        }

        try
        {
            var bytes = File.ReadAllBytes(filePath);
            if (bytes.Length == 0)
            {
                return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorEmptyDocument);
            }

            var encoding = ProbeEncoding(bytes);
            var text = encoding.GetString(bytes);
            return CvTextOk(text);
        }
        catch (Exception)
        {
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
        }
    }

    private static CvTextExtractionResult CvTextOk(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorEmptyDocument);
        }

        return new CvTextExtractionResult(true, raw, null);
    }

    private static Encoding ProbeEncoding(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8;
        }

        if (bytes.Length >= 2)
        {
            if (bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                return Encoding.Unicode;
            }

            if (bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode;
            }
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            var utf8 = Encoding.UTF8;
            var decoded = utf8.GetString(bytes);
            var bounced = utf8.GetBytes(decoded);
            if (bytes.SequenceEqual(bounced))
            {
                return utf8;
            }
        }
        catch
        {
            // Fallback below.
        }

        try
        {
            Encoding.GetEncoding(1250).GetString(bytes);
            return Encoding.GetEncoding(1250);
        }
        catch
        {
            return new UTF8Encoding(false, false);
        }
    }
}
