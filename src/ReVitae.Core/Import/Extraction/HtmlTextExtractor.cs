using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ReVitae.Core.Import.Structured;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Extraction;

public sealed class HtmlTextExtractor : ICvTextExtractor
{
    private static readonly HashSet<string> SkipTagNames =
    [
        "script",
        "style",
        "noscript"
    ];

    public CvTextExtractionResult Extract(string filePath)
    {
        if (ImportExtractorGuards.TryRejectMissing(filePath, out var fail))
        {
            return fail;
        }

        try
        {
            var html = File.ReadAllText(filePath, new UTF8Encoding(false, false));
            return ExtractFromMarkup(html);
        }
        catch (Exception)
        {
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
        }
    }

    /// <summary>Converts snippets (for example RtfPipe HTML) into plaintext with collected anchor targets.</summary>
    public static CvTextExtractionResult FromHtmlMarkup(string markup)
    {
        return ExtractFromMarkup(markup);
    }

    private static CvTextExtractionResult ExtractFromMarkup(string html)
    {
        var hyperlinkUrls = new List<string>();
        var seenHref = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HtmlDocument htmlDoc = new();
        htmlDoc.OptionFixNestedTags = true;
        htmlDoc.LoadHtml(html);

        foreach (HtmlNode anchor in htmlDoc.DocumentNode.Descendants("a"))
        {
            CvStructuredImportMapper.AddUniqueHref(
                hyperlinkUrls,
                seenHref,
                anchor.GetAttributeValue("href", string.Empty));
        }

        var body = htmlDoc.DocumentNode.SelectSingleNode("//body") ?? htmlDoc.DocumentNode;
        TrimHiddenNodes(body);

        var buffer = new StringBuilder();
        WriteNode(body, buffer, preserveWhitespace: false);
        var text = buffer.ToString().Trim();
        IReadOnlyList<string>? hrefList = hyperlinkUrls.Count > 0 ? hyperlinkUrls : null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorEmptyDocument, hrefList);
        }

        return new CvTextExtractionResult(true, text, null, hrefList);
    }

    private static void TrimHiddenNodes(HtmlNode root)
    {
        var hidden = root.SelectNodes(".//*[@style[contains(translate(., 'DISPLAY', 'display'), 'display:none')] or translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='hidden' or translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='hidden']");
        if (hidden is null)
        {
            return;
        }

        foreach (var node in hidden)
        {
            node.Remove();
        }
    }

    private static bool IsHiddenCss(string css)
    {
        return Regex.IsMatch(css, @"display\s*:\s*none", RegexOptions.IgnoreCase);
    }

    private static void WriteNode(HtmlNode node, StringBuilder sink, bool preserveWhitespace)
    {
        if (node.NodeType == HtmlNodeType.Element && SkipTagNames.Contains(node.Name.ToLowerInvariant()))
        {
            return;
        }

        if (node.NodeType == HtmlNodeType.Element)
        {
            var styleAttr = node.GetAttributeValue("style", string.Empty);
            if (!string.IsNullOrEmpty(styleAttr) && IsHiddenCss(styleAttr))
            {
                return;
            }
        }

        var name = node.NodeType == HtmlNodeType.Element ? node.Name.ToLowerInvariant() : string.Empty;
        var blocks = name is "p" or "div" or "br" or "li" or "tr" or "table" or "pre"
            or "h1" or "h2" or "h3" or "h4" or "h5" or "h6";

        if (blocks)
        {
            EnsureLineBreak(sink);
        }

        if (node.NodeType == HtmlNodeType.Text)
        {
            var text = HtmlEntity.DeEntitize(node.InnerText);
            if (preserveWhitespace)
            {
                sink.Append(text);
            }
            else
            {
                AppendTextChunk(sink, text.Trim());
            }

            return;
        }

        var preserveChildren = preserveWhitespace || name == "pre";

        foreach (var child in node.ChildNodes)
        {
            WriteNode(child, sink, preserveChildren);
            if (blocks && child.NextSibling is not null)
            {
                EnsureLineBreak(sink);
            }
        }
    }

    private static void EnsureLineBreak(StringBuilder sink)
    {
        if (sink.Length > 0 && sink[^1] != '\n')
        {
            sink.Append('\n');
        }
    }

    private static void AppendTextChunk(StringBuilder sink, string chunk)
    {
        if (string.IsNullOrEmpty(chunk))
        {
            return;
        }

        if (sink.Length > 0 && !char.IsWhiteSpace(sink[^1]))
        {
            sink.Append(' ');
        }

        sink.Append(chunk);
    }
}
