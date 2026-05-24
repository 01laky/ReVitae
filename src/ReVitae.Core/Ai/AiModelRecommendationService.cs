using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai;

/// <summary>
/// Deterministic local-model recommendation based on detected RAM tiers.
/// Allows downloading exactly one tier above the strict fit, with a warning flag.
/// </summary>
public static class AiModelRecommendationService
{
    private const double ExtraLargeHeadroomFactor = 1.25;
    private const long MinRamForAnyDownload = 4L * 1024 * 1024 * 1024;

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
        var strictMaxTier = GetStrictMaxTier(ramBytes);
        var recommendedEntry = PickRecommendedModel(catalog, ramBytes, strictMaxTier);
        var models = catalog
            .Select(entry => ToRecommendation(entry, recommendedEntry, ramBytes, strictMaxTier))
            .OrderBy(recommendation => recommendation.Model.Tier)
            .ThenByDescending(recommendation => recommendation.Model.RecommendationPriority)
            .ThenBy(recommendation => recommendation.Model.DisplayNameKey, StringComparer.Ordinal)
            .ToArray();

        return new AiSystemDetectionResult(
            profileWithWarning,
            ollama,
            models,
            recommendedEntry);
    }

    internal static AiModelTier GetStrictMaxTier(long? ramBytes)
    {
        if (ramBytes is null)
        {
            return AiModelTier.Small;
        }

        if (ramBytes < 8L * 1024 * 1024 * 1024)
        {
            return ramBytes >= MinRamForAnyDownload ? AiModelTier.Compact : AiModelTier.Compact;
        }

        if (ramBytes < 16L * 1024 * 1024 * 1024)
        {
            return AiModelTier.Small;
        }

        if (ramBytes < 48L * 1024 * 1024 * 1024)
        {
            return AiModelTier.Medium;
        }

        if (ramBytes < 64L * 1024 * 1024 * 1024)
        {
            return AiModelTier.Large;
        }

        return AiModelTier.ExtraLarge;
    }

    private static AiModelCatalogEntry? PickRecommendedModel(
        IReadOnlyList<AiModelCatalogEntry> catalog,
        long? ramBytes,
        AiModelTier strictMaxTier)
    {
        return catalog
            .Where(entry => entry.Tier == strictMaxTier && IsStrictFit(entry, ramBytes))
            .OrderByDescending(entry => entry.RecommendationPriority)
            .FirstOrDefault()
            ?? catalog
                .Where(entry => IsStrictFit(entry, ramBytes))
                .OrderByDescending(entry => entry.Tier)
                .ThenByDescending(entry => entry.RecommendationPriority)
                .FirstOrDefault()
            ?? catalog
                .Where(entry => entry.Tier == strictMaxTier)
                .OrderByDescending(entry => entry.RecommendationPriority)
                .FirstOrDefault();
    }

    private static AiModelRecommendation ToRecommendation(
        AiModelCatalogEntry entry,
        AiModelCatalogEntry? recommendedEntry,
        long? ramBytes,
        AiModelTier strictMaxTier)
    {
        var isRecommended = recommendedEntry?.Id == entry.Id;
        var strictFit = IsStrictFit(entry, ramBytes);
        var isOneTierUp = entry.Tier == strictMaxTier + 1 && CanAttemptOneTierUp(ramBytes);
        var isDownloadAllowed = strictFit || isOneTierUp;
        var requiresWarning = isOneTierUp && !strictFit;

        string? reasonKey = isRecommended
            ? TranslationKeys.AiSetupReasonRecommended
            : requiresWarning
                ? TranslationKeys.AiSetupOversizedWarning
                : isDownloadAllowed
                    ? null
                    : TranslationKeys.AiSetupRequiresMoreMemory;

        return new AiModelRecommendation(entry, isRecommended, isDownloadAllowed, requiresWarning, reasonKey);
    }

    internal static bool IsStrictFit(AiModelCatalogEntry entry, long? ramBytes)
    {
        if (ramBytes is null)
        {
            return entry.Tier <= AiModelTier.Small;
        }

        if (ramBytes < MinRamForAnyDownload)
        {
            return false;
        }

        if (entry.Tier == AiModelTier.ExtraLarge)
        {
            return ramBytes >= (long)(entry.MinimumMemoryBytes * ExtraLargeHeadroomFactor);
        }

        return ramBytes >= entry.MinimumMemoryBytes;
    }

    internal static bool CanAttemptOneTierUp(long? ramBytes)
    {
        return ramBytes is null || ramBytes >= MinRamForAnyDownload;
    }
}
