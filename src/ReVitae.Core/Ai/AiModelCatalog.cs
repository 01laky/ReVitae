using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai;

public static class AiModelCatalog
{
    private static readonly AiPlatform[] AllPlatforms =
    [
        AiPlatform.Windows,
        AiPlatform.MacOS,
        AiPlatform.Linux,
    ];

    public static IReadOnlyList<AiModelCatalogEntry> Default { get; } =
    [
        new AiModelCatalogEntry(
            "small-instruct",
            TranslationKeys.AiModelSmallInstructName,
            ApproxDownloadBytes: 2L * 1024 * 1024 * 1024,
            MinimumMemoryBytes: 8L * 1024 * 1024 * 1024,
            AiModelTier.Small,
            "llama3.2:3b-instruct",
            AllPlatforms),
        new AiModelCatalogEntry(
            "medium-instruct",
            TranslationKeys.AiModelMediumInstructName,
            ApproxDownloadBytes: (long)(4.7 * 1024 * 1024 * 1024),
            MinimumMemoryBytes: 16L * 1024 * 1024 * 1024,
            AiModelTier.Medium,
            "llama3.1:8b-instruct",
            AllPlatforms),
        new AiModelCatalogEntry(
            "large-instruct",
            TranslationKeys.AiModelLargeInstructName,
            ApproxDownloadBytes: 40L * 1024 * 1024 * 1024,
            MinimumMemoryBytes: 64L * 1024 * 1024 * 1024,
            AiModelTier.Large,
            "llama3.1:70b-instruct",
            AllPlatforms),
    ];
}
