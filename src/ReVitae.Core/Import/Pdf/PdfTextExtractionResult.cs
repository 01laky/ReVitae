namespace ReVitae.Core.Import.Pdf;

public sealed record PdfTextExtractionResult(
    bool Success,
    string Text,
    int PageCount,
    string? ErrorMessageKey,
    IReadOnlyList<string>? HyperlinkUrls = null);
