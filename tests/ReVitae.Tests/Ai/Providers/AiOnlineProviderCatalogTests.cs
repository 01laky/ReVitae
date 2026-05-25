using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Providers;

public sealed class AiOnlineProviderCatalogTests
{
	[Fact]
	public void GetAll_ReturnsNineProvidersWithUniqueIds()
	{
		var providers = AiOnlineProviderCatalog.GetAll();

		Assert.Equal(9, providers.Count);
		Assert.Equal(providers.Count, providers.Select(provider => provider.Id).Distinct(StringComparer.Ordinal).Count());
	}

	[Theory]
	[InlineData("openai")]
	[InlineData("anthropic")]
	[InlineData("google-gemini")]
	[InlineData("groq")]
	[InlineData("azure-openai")]
	[InlineData("mistral")]
	[InlineData("deepseek")]
	[InlineData("openrouter")]
	[InlineData("custom-openai")]
	public void TryGetById_ReturnsProvider(string providerId)
	{
		var provider = AiOnlineProviderCatalog.TryGetById(providerId);

		Assert.NotNull(provider);
		Assert.Equal(providerId, provider!.Id);
	}

	[Fact]
	public void AzureProvider_UsesDeploymentFieldsNotModelSelect()
	{
		var azure = AiOnlineProviderCatalog.TryGetById("azure-openai")!;

		Assert.Contains(azure.Fields, field => field.Id == AiProviderFieldIds.DeploymentName && field.Required);
		Assert.DoesNotContain(azure.Fields, field => field.Id == AiProviderFieldIds.ModelId);
		Assert.Null(azure.DefaultBaseUrl);
	}

	[Fact]
	public void CustomProvider_RequiresBaseUrlAndModelWithoutMandatoryApiKey()
	{
		var custom = AiOnlineProviderCatalog.TryGetById("custom-openai")!;

		Assert.Contains(custom.Fields, field => field.Id == AiProviderFieldIds.BaseUrl && field.Required);
		Assert.Contains(custom.Fields, field => field.Id == AiProviderFieldIds.ModelId && field.Required);
		Assert.Contains(custom.Fields, field => field.Id == AiProviderFieldIds.ApiKey && !field.Required);
	}

	[Theory]
	[InlineData("groq", true)]
	[InlineData("google-gemini", true)]
	[InlineData("openrouter", true)]
	[InlineData("openai", false)]
	public void FreeTierBadge_MatchesCatalog(string providerId, bool expectedBadge)
	{
		var provider = AiOnlineProviderCatalog.TryGetById(providerId)!;

		Assert.Equal(expectedBadge, provider.HasFreeTierBadge);
	}

	[Fact]
	public void OpenAiProvider_HasSuggestedModelsWithHintKeys()
	{
		var openAi = AiOnlineProviderCatalog.TryGetById("openai")!;

		Assert.NotEmpty(openAi.SuggestedModels);
		Assert.All(openAi.SuggestedModels, option => Assert.False(string.IsNullOrWhiteSpace(option.HintKey)));
		Assert.Equal(TranslationKeys.AiSetupProviderUseCaseQuickEdits, openAi.ModelUseCaseHintKey);
	}
}
