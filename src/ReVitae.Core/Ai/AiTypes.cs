namespace ReVitae.Core.Ai;

public enum AiPlatform
{
    Windows = 0,
    MacOS = 1,
    Linux = 2,
    Unknown = 3,
}

public enum AiModelTier
{
    Small = 0,
    Medium = 1,
    Large = 2,
}

public enum AiRuntimeKind
{
    Disabled = 0,
    Ollama = 1,
}

public sealed record SystemProfile(
    AiPlatform Platform,
    string Architecture,
    long? TotalPhysicalMemoryBytes,
    int ProcessorCount,
    string? DetectionWarningKey);

public sealed record OllamaRuntimeStatus(
    bool IsReachable,
    IReadOnlyList<string> InstalledModelTags);

public sealed record AiModelCatalogEntry(
    string Id,
    string DisplayNameKey,
    long ApproxDownloadBytes,
    long MinimumMemoryBytes,
    AiModelTier Tier,
    string OllamaModelTag,
    IReadOnlyList<AiPlatform> SupportedPlatforms);

public sealed record AiModelRecommendation(
    AiModelCatalogEntry Model,
    bool IsRecommended,
    bool IsDownloadAllowed,
    string? ReasonKey);

public sealed record AiSystemDetectionResult(
    SystemProfile Profile,
    OllamaRuntimeStatus Ollama,
    IReadOnlyList<AiModelRecommendation> Models,
    AiModelCatalogEntry? RecommendedModel);

public sealed record AiSettingsSnapshot(
    string SelectedModelId,
    string OllamaModelTag,
    DateTimeOffset DownloadedAtUtc);

public sealed record OllamaPullProgress(string Status, long? Completed, long? Total);

public enum OllamaPullOutcome
{
    Succeeded,
    Failed,
    Cancelled,
}

public sealed record OllamaPullResult(OllamaPullOutcome Outcome, string? ErrorMessage);
