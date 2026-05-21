using ReVitae.Core.Localization;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ReVitae.Core.Import.Pdf;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public PdfTextExtractionResult Extract(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new PdfTextExtractionResult(false, string.Empty, 0, TranslationKeys.ImportErrorFileNotFound);
        }

        try
        {
            using var document = PdfDocument.Open(filePath);
            var pages = document.GetPages().ToArray();
            var chunks = new List<string>();

            foreach (var page in pages)
            {
                var pageText = ExtractPageText(page);
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    chunks.Add(pageText);
                }
            }

            var text = string.Join("\n\n", chunks).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return new PdfTextExtractionResult(false, string.Empty, pages.Length, TranslationKeys.ImportErrorEmptyPdf);
            }

            return new PdfTextExtractionResult(true, text, pages.Length, null);
        }
        catch (UglyToad.PdfPig.Exceptions.PdfDocumentEncryptedException)
        {
            return new PdfTextExtractionResult(false, string.Empty, 0, TranslationKeys.ImportErrorPasswordProtected);
        }
        catch (Exception)
        {
            return new PdfTextExtractionResult(false, string.Empty, 0, TranslationKeys.ImportErrorUnreadablePdf);
        }
    }

    private static string ExtractPageText(Page page)
    {
        var words = page.GetWords().ToArray();

        if (words.Length == 0)
        {
            return page.Text;
        }

        var columns = SplitIntoColumns(words);
        return string.Join("\n\n", columns.Select(ExtractColumnText).Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static IReadOnlyList<IReadOnlyList<Word>> SplitIntoColumns(IReadOnlyList<Word> words)
    {
        var minX = words.Min(word => word.BoundingBox.Left);
        var maxX = words.Max(word => word.BoundingBox.Right);
        var splitX = minX + ((maxX - minX) * 0.38);
        var left = words.Where(word => word.BoundingBox.Left <= splitX).ToArray();
        var right = words.Where(word => word.BoundingBox.Left > splitX).ToArray();

        if (left.Length < 12 || right.Length < 12)
        {
            return [words];
        }

        return [left, right];
    }

    private static string ExtractColumnText(IReadOnlyList<Word> words)
    {
        var lines = new List<List<Word>>();
        foreach (var word in words
            .OrderByDescending(word => word.BoundingBox.Bottom)
            .ThenBy(word => word.BoundingBox.Left))
        {
            var line = lines.FirstOrDefault(candidate =>
                Math.Abs(candidate[0].BoundingBox.Bottom - word.BoundingBox.Bottom) < 3);
            if (line is null)
            {
                lines.Add([word]);
            }
            else
            {
                line.Add(word);
            }
        }

        return string.Join(
            "\n",
            lines.Select(line => string.Join(
                " ",
                line.OrderBy(word => word.BoundingBox.Left).Select(word => word.Text))));
    }
}
