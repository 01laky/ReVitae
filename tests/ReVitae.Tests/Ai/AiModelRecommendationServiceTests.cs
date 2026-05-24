using ReVitae.Core.Ai;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai;

public sealed class AiModelRecommendationServiceTests
{
    private static SystemProfile CreateProfile(long? ramBytes, string? warningKey = null) =>
        new(AiPlatform.MacOS, "arm64", ramBytes, 8, warningKey);

    private static OllamaRuntimeStatus OfflineOllama => new(false, []);

    [Fact]
    public void Recommend_With8GbRam_SelectsSmallModel()
    {
        var result = AiModelRecommendationService.Recommend(
            CreateProfile(8L * 1024 * 1024 * 1024),
            OfflineOllama);

        Assert.Equal("small-instruct", result.RecommendedModel?.Id);
        Assert.True(result.Models.Single(model => model.Model.Id == "small-instruct").IsRecommended);
        Assert.False(result.Models.Single(model => model.Model.Id == "medium-instruct").IsDownloadAllowed);
        Assert.False(result.Models.Single(model => model.Model.Id == "large-instruct").IsDownloadAllowed);
    }

    [Fact]
    public void Recommend_With16GbRam_SelectsMedium()
    {
        var result = AiModelRecommendationService.Recommend(
            CreateProfile(16L * 1024 * 1024 * 1024),
            OfflineOllama);

        Assert.Equal("medium-instruct", result.RecommendedModel?.Id);
        Assert.False(result.Models.Single(model => model.Model.Id == "large-instruct").IsDownloadAllowed);
    }

    [Fact]
    public void Recommend_With80GbRam_CanSelectLarge()
    {
        var result = AiModelRecommendationService.Recommend(
            CreateProfile(80L * 1024 * 1024 * 1024),
            OfflineOllama);

        Assert.Equal("large-instruct", result.RecommendedModel?.Id);
        Assert.True(result.Models.Single(model => model.Model.Id == "large-instruct").IsDownloadAllowed);
    }

    [Fact]
    public void Recommend_WithUnknownRam_IsConservative()
    {
        var result = AiModelRecommendationService.Recommend(
            CreateProfile(null),
            OfflineOllama);

        Assert.Equal("small-instruct", result.RecommendedModel?.Id);
        Assert.Equal(TranslationKeys.AiSetupUnknownRam, result.Profile.DetectionWarningKey);
        Assert.True(result.Models.Single(model => model.Model.Id == "small-instruct").IsDownloadAllowed);
        Assert.False(result.Models.Single(model => model.Model.Id == "medium-instruct").IsDownloadAllowed);
    }

    [Fact]
    public void Recommend_FlagsOversizedModels()
    {
        var result = AiModelRecommendationService.Recommend(
            CreateProfile(16L * 1024 * 1024 * 1024),
            OfflineOllama);

        var large = result.Models.Single(model => model.Model.Id == "large-instruct");
        Assert.False(large.IsDownloadAllowed);
        Assert.Equal(TranslationKeys.AiSetupRequiresMoreMemory, large.ReasonKey);
    }

    [Fact]
    public void Catalog_EntriesHaveUniqueIds()
    {
        var ids = AiModelCatalog.Default.Select(entry => entry.Id).ToArray();

        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }
}
