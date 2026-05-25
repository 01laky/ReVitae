using ReVitae.Core.Import.Extraction;

namespace ReVitae.Core.Import;

public sealed record CvTextImportAttempt(
    CvImportFormat Format,
    CvTextExtractionResult Extraction,
    string NormalizedText,
    CvSegmentationResult Segmentation,
    CvImportResult Deterministic)
{
    public bool IsTextRoute { get; init; } = true;

    public int NonWhitespaceCharCount =>
        string.IsNullOrEmpty(NormalizedText)
            ? 0
            : NormalizedText.Count(ch => !char.IsWhiteSpace(ch));
}
