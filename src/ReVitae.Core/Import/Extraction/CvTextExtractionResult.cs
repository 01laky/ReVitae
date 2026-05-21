namespace ReVitae.Core.Import.Extraction;

/// <summary>Unified text extraction envelope for Category A imports (replacing PDF-only results).</summary>
public sealed record CvTextExtractionResult(
    bool Success,
    string Text,
    string? ErrorMessageKey,
    IReadOnlyList<string>? HyperlinkUrls = null,
    IReadOnlyList<CvImportWarning>? Warnings = null,
    int? PageCount = null);
