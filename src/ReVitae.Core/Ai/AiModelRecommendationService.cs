using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai;

/// <summary>
/// Deterministic local-model recommendation based on detected RAM tiers.
/// Headroom factor prevents recommending models that fit on paper but leave no runtime margin.
/// </summary>
public static class AiModelRecommendationService
{
    private const double HeadroomFactor = 1.25;

    public static AiSystemDetectionResult Recommend(
        SystemProfile profile,
        OllamaRuntimeStatus ollama,
        IReadOnlyList<AiModelCatalogEntry>? catalog = null)
    {
        catalog ??= AiModelCatalog.Default;
        var ramBytes = profile.TotalPhysicalMemoryBytes;
        var warningKey = profile.DetectionWarningKey;

        if (ramBytes is null && warningKey is null)
        {
            warningKey = TranslationKeys.AiSetupUnknownRam;
        }

        var profileWithWarning = profile with { DetectionWarningKey = warningKey };
        var recommendedEntry = PickRecommendedModel(catalog, ramBytes);
        var models = catalog
            .Select(entry => ToRecommendation(entry, recommendedEntry, ramBytes))
            .ToArray();

        return new AiSystemDetectionResult(
            profileWithWarning,
            ollama,
            models,
            recommendedEntry);
    }

    private static AiModelCatalogEntry? PickRecommendedModel(
        IReadOnlyList<AiModelCatalogEntry> catalog,
        long? ramBytes)
    {
        if (ramBytes is null)
        {
            return catalog.FirstOrDefault(entry => entry.Tier == AiModelTier.Small);
        }

        if (ramBytes < 8L * 1024 * 1024 * 1024)
        {
            return catalog.FirstOrDefault(entry => entry.Tier == AiModelTier.Small);
        }

        if (ramBytes < 16L * 1024 * 1024 * 1024)
        {
            return catalog.FirstOrDefault(entry => entry.Tier == AiModelTier.Small);
        }

        if (ramBytes < 64L * 1024 * 1024 * 1024)
        {
            return catalog.FirstOrDefault(entry => entry.Tier == AiModelTier.Medium);
        }

        var large = catalog.FirstOrDefault(entry => entry.Tier == AiModelTier.Large);
        if (large is not null && ramBytes >= (long)(large.MinimumMemoryBytes * HeadroomFactor))
        {
            return large;
        }

        return catalog.FirstOrDefault(entry => entry.Tier == AiModelTier.Medium);
    }

    private static AiModelRecommendation ToRecommendation(
        AiModelCatalogEntry entry,
        AiModelCatalogEntry? recommendedEntry,
        long? ramBytes)
    {
        var isRecommended = recommendedEntry?.Id == entry.Id;
        var isDownloadAllowed = IsDownloadAllowed(entry, ramBytes);
        string? reasonKey = isRecommended
            ? TranslationKeys.AiSetupReasonRecommended
            : isDownloadAllowed
                ? null
                : TranslationKeys.AiSetupRequiresMoreMemory;

        return new AiModelRecommendation(entry, isRecommended, isDownloadAllowed, reasonKey);
    }

    internal static bool IsDownloadAllowed(AiModelCatalogEntry entry, long? ramBytes)
    {
        if (ramBytes is null)
        {
            return entry.Tier == AiModelTier.Small;
        }

        if (ramBytes < 8L * 1024 * 1024 * 1024)
        {
            return false;
        }

        if (entry.Tier == AiModelTier.Large)
        {
            return ramBytes >= (long)(entry.MinimumMemoryBytes * HeadroomFactor);
        }

        return ramBytes >= entry.MinimumMemoryBytes;
    }
}
