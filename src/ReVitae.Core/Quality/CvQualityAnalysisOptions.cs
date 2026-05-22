using ReVitae.Core.Import;

namespace ReVitae.Core.Quality;

public sealed record CvQualityAnalysisOptions(
    IReadOnlyList<ImportedFieldConfidence>? ImportConfidences = null,
    IReadOnlySet<string>? DismissedHintKeys = null)
{
    public static CvQualityAnalysisOptions Default { get; } = new();
}
