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

    private const long Gb = 1024L * 1024 * 1024;

    public static IReadOnlyList<AiModelCatalogEntry> Default { get; } =
    [
        Entry("gemma2-2b", TranslationKeys.AiModelGemma2_2bName, 1.6 * Gb, 4 * Gb, AiModelTier.Compact, "gemma2:2b", 10),
        Entry("phi3-mini", TranslationKeys.AiModelPhi3MiniName, 2.3 * Gb, 8 * Gb, AiModelTier.Small, "phi3:mini", 20),
        Entry("llama32-3b", TranslationKeys.AiModelLlama32_3bName, 2 * Gb, 8 * Gb, AiModelTier.Small, "llama3.2:3b-instruct", 100),
        Entry("qwen25-3b", TranslationKeys.AiModelQwen25_3bName, 2 * Gb, 8 * Gb, AiModelTier.Small, "qwen2.5:3b-instruct", 30),
        Entry("mistral-7b", TranslationKeys.AiModelMistral7bName, 4.1 * Gb, 16 * Gb, AiModelTier.Medium, "mistral:7b-instruct", 40),
        Entry("llama31-8b", TranslationKeys.AiModelLlama31_8bName, 4.7 * Gb, 16 * Gb, AiModelTier.Medium, "llama3.1:8b-instruct", 100),
        Entry("gemma2-9b", TranslationKeys.AiModelGemma2_9bName, 5.4 * Gb, 16 * Gb, AiModelTier.Medium, "gemma2:9b", 25),
        Entry("qwen25-7b", TranslationKeys.AiModelQwen25_7bName, 4.7 * Gb, 16 * Gb, AiModelTier.Medium, "qwen2.5:7b-instruct", 35),
        Entry("mixtral-8x7b", TranslationKeys.AiModelMixtral8x7bName, 26 * Gb, 48 * Gb, AiModelTier.Large, "mixtral:8x7b-instruct", 100),
        Entry("llama31-70b", TranslationKeys.AiModelLlama31_70bName, 40 * Gb, 64 * Gb, AiModelTier.ExtraLarge, "llama3.1:70b-instruct", 100),
        Entry("llama33-70b", TranslationKeys.AiModelLlama33_70bName, 43 * Gb, 64 * Gb, AiModelTier.ExtraLarge, "llama3.3:70b", 90),
    ];

    public static AiModelCatalogEntry? TryGetById(string id) =>
        Default.FirstOrDefault(entry => string.Equals(entry.Id, id, StringComparison.Ordinal));

    private static AiModelCatalogEntry Entry(
        string id,
        string displayNameKey,
        double approxDownloadGb,
        long minimumMemoryBytes,
        AiModelTier tier,
        string ollamaTag,
        int recommendationPriority) =>
        new(
            id,
            displayNameKey,
            (long)approxDownloadGb,
            minimumMemoryBytes,
            tier,
            ollamaTag,
            AllPlatforms,
            recommendationPriority);
}
