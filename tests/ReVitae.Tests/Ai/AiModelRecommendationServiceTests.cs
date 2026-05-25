using ReVitae.Core.Ai;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai;

public sealed class AiModelRecommendationServiceTests
{
	private static SystemProfile CreateProfile(long? ramBytes, string? warningKey = null) =>
		new(AiPlatform.MacOS, "arm64", ramBytes, 8, warningKey);

	private static OllamaRuntimeStatus OfflineOllama => new(false, []);

	[Fact]
	public void Recommend_With8GbRam_SelectsSmallTierFlagship()
	{
		var result = AiModelRecommendationService.Recommend(
			CreateProfile(8L * 1024 * 1024 * 1024),
			OfflineOllama);

		Assert.Equal("llama32-3b", result.RecommendedModel?.Id);
		Assert.True(result.Models.Single(model => model.Model.Id == "llama32-3b").IsRecommended);
		Assert.True(result.Models.Single(model => model.Model.Id == "mistral-7b").IsDownloadAllowed);
		Assert.True(result.Models.Single(model => model.Model.Id == "mistral-7b").RequiresOversizedWarning);
		Assert.False(result.Models.Single(model => model.Model.Id == "mixtral-8x7b").IsDownloadAllowed);
	}

	[Fact]
	public void Recommend_With16GbRam_SelectsMediumTierFlagship()
	{
		var result = AiModelRecommendationService.Recommend(
			CreateProfile(16L * 1024 * 1024 * 1024),
			OfflineOllama);

		Assert.Equal("llama31-8b", result.RecommendedModel?.Id);
		Assert.True(result.Models.Single(model => model.Model.Id == "llama31-8b").IsStrictFit());
		Assert.True(result.Models.Single(model => model.Model.Id == "mixtral-8x7b").RequiresOversizedWarning);
		Assert.False(result.Models.Single(model => model.Model.Id == "llama31-70b").IsDownloadAllowed);
	}

	[Fact]
	public void Recommend_With80GbRam_CanSelectExtraLarge()
	{
		var result = AiModelRecommendationService.Recommend(
			CreateProfile(80L * 1024 * 1024 * 1024),
			OfflineOllama);

		Assert.Equal("llama31-70b", result.RecommendedModel?.Id);
		Assert.True(result.Models.Single(model => model.Model.Id == "llama31-70b").IsDownloadAllowed);
		Assert.False(result.Models.Single(model => model.Model.Id == "llama31-70b").RequiresOversizedWarning);
	}

	[Fact]
	public void Recommend_WithUnknownRam_IsConservativeAndAllowsOneTierUp()
	{
		var result = AiModelRecommendationService.Recommend(
			CreateProfile(null),
			OfflineOllama);

		Assert.Equal("llama32-3b", result.RecommendedModel?.Id);
		Assert.Equal(TranslationKeys.AiSetupUnknownRam, result.Profile.DetectionWarningKey);
		Assert.True(result.Models.Single(model => model.Model.Id == "llama32-3b").IsDownloadAllowed);
		Assert.True(result.Models.Single(model => model.Model.Id == "mistral-7b").RequiresOversizedWarning);
		Assert.False(result.Models.Single(model => model.Model.Id == "mixtral-8x7b").IsDownloadAllowed);
	}

	[Fact]
	public void Recommend_FlagsModelsMoreThanOneTierAbove()
	{
		var result = AiModelRecommendationService.Recommend(
			CreateProfile(16L * 1024 * 1024 * 1024),
			OfflineOllama);

		var extraLarge = result.Models.Single(model => model.Model.Id == "llama31-70b");
		Assert.False(extraLarge.IsDownloadAllowed);
		Assert.Equal(TranslationKeys.AiSetupRequiresMoreMemory, extraLarge.ReasonKey);
	}

	[Fact]
	public void Catalog_EntriesHaveUniqueIds()
	{
		var ids = AiModelCatalog.Default.Select(entry => entry.Id).ToArray();

		Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
		Assert.True(AiModelCatalog.Default.Count >= 10);
	}

	[Fact]
	public void FormatDetailLines_IncludesPlatformCpuAndOllama()
	{
		var localizer = new AppLocalizer("en");
		var lines = AiSystemInfoFormatter.FormatDetailLines(
			CreateProfile(16L * 1024 * 1024 * 1024),
			new OllamaRuntimeStatus(true, ["llama3.2:3b-instruct"]),
			100L * 1024 * 1024 * 1024,
			localizer);

		Assert.Equal(6, lines.Count);
		Assert.Contains(lines, line => line.Contains("macOS", StringComparison.Ordinal));
		Assert.Contains(lines, line => line.Contains("arm64", StringComparison.Ordinal));
		Assert.Contains(lines, line => line.Contains("running", StringComparison.OrdinalIgnoreCase));
	}
}

file static class AiModelRecommendationTestExtensions
{
	public static bool IsStrictFit(this AiModelRecommendation recommendation) =>
		recommendation.IsDownloadAllowed && !recommendation.RequiresOversizedWarning;
}
