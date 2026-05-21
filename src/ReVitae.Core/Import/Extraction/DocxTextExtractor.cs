using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ReVitae.Core.Import.Structured;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Extraction;

public sealed class DocxTextExtractor : ICvTextExtractor
{
    public CvTextExtractionResult Extract(string filePath)
    {
        if (ImportExtractorGuards.TryRejectMissing(filePath, out var fail))
        {
            return fail;
        }

        List<CvImportWarning>? warnings = null;
        try
        {
            using WordprocessingDocument document = WordprocessingDocument.Open(filePath, false);
            var part = document.MainDocumentPart ?? throw new InvalidOperationException("Missing DOCX body.");
            var body = part.Document?.Body ?? throw new InvalidOperationException("Missing DOCX paragraphs.");
            if (body.Descendants<Drawing>().Any())
            {
                warnings ??= [];
                warnings.Add(new CvImportWarning(TranslationKeys.ImportWarningPartialDocumentContent));
            }

            var buffer = new StringBuilder();
            foreach (Paragraph paragraph in body.Descendants<Paragraph>())
            {
                buffer.AppendLine(paragraph.InnerText.Trim());
            }

            var links = CollectHyperlinks(part);
            var flattened = CvTextNormalizer.Normalize(buffer.ToString()).Trim();
            DiscardEmptyHrefList(ref links);
            DiscardWarningsIfEmpty(ref warnings);
            return string.IsNullOrWhiteSpace(flattened)
                ? new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorEmptyDocument, links, warnings)
                : new CvTextExtractionResult(true, flattened, null, links, warnings);
        }
        catch (IOException)
        {
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
        }
        catch (Exception ex) when (
            ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("crypt", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("encrypt", StringComparison.OrdinalIgnoreCase))
        {
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorPasswordProtected);
        }
        catch (Exception)
        {
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
        }
    }

    private static List<string>? CollectHyperlinks(MainDocumentPart part)
    {
        List<string>? hrefs = null;
        HashSet<string>? seen = null;
        foreach (Hyperlink hyperlink in part.Document!.Descendants<Hyperlink>())
        {
            var relationshipId = hyperlink.Id?.Value;
            if (string.IsNullOrEmpty(relationshipId))
            {
                continue;
            }

            var relationship =
                part.HyperlinkRelationships.FirstOrDefault(candidate => candidate.Id.Equals(relationshipId, StringComparison.Ordinal));
            if (relationship?.IsExternal is not true)
            {
                continue;
            }

            var uri = relationship.Uri?.ToString();
            if (string.IsNullOrWhiteSpace(uri))
            {
                continue;
            }

            hrefs ??= [];
            seen ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CvStructuredImportMapper.AddUniqueHref(hrefs!, seen!, uri);
        }

        return hrefs;
    }

    private static void DiscardWarningsIfEmpty(ref List<CvImportWarning>? warns)
    {
        if (warns?.Count == 0)
        {
            warns = null;
        }
    }

    private static void DiscardEmptyHrefList(ref List<string>? links)
    {
        if (links?.Count == 0)
        {
            links = null;
        }
    }
}
