using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Import;

public sealed record AiCvImportProgress(
    int CompletedBatches,
    int TotalBatches,
    AiImportPhase Phase,
    int BatchIndex,
    int BatchCountInPhase,
    string PhaseLabelKey);

public sealed record AiCvImportRequest(
    string NormalizedText,
    CvSegmentationResult Segmentation,
    CvImportResult? DeterministicBaseline,
    AiImportBatchPlan Plan,
    string UiCulture,
    AiCvImportMergeMode MergeMode,
    IReadOnlyList<CvImportWarning> AcquisitionWarnings,
    string? ExistingProfilePhotoPath,
    CancellationToken CancellationToken = default);

public sealed record AiCvImportOutcome(
    bool Succeeded,
    CvImportResult? Result,
    AiCvImportReviewSummary? ReviewSummary,
    int BatchesCompleted,
    int BatchesFailed,
    string? ErrorMessageKey,
    string? LastParseError,
    bool Cancelled);
