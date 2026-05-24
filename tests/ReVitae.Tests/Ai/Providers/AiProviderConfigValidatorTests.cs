using ReVitae.Core.Ai.Providers;

namespace ReVitae.Tests.Ai.Providers;

public sealed class AiProviderConfigValidatorTests
{
    private static AiProviderConnectionDraft Draft(
        string providerId,
        params (string Id, string? Value)[] values) =>
        new(
            providerId,
            values.ToDictionary(pair => pair.Id, pair => pair.Value, StringComparer.Ordinal),
            values.FirstOrDefault(pair => pair.Id == AiProviderFieldIds.ApiKey).Value);

    [Fact]
    public void IsValid_OpenAi_WithModelAndKey_ReturnsTrue()
    {
        var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
        var draft = Draft(
            "openai",
            (AiProviderFieldIds.ModelId, "gpt-4o-mini"),
            (AiProviderFieldIds.ApiKey, "sk-test"));

        Assert.True(AiProviderConfigValidator.IsValid(provider, draft));
    }

    [Fact]
    public void IsValid_OpenAi_WithoutModel_ReturnsFalse()
    {
        var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
        var draft = Draft("openai", (AiProviderFieldIds.ApiKey, "sk-test"));

        Assert.False(AiProviderConfigValidator.IsValid(provider, draft));
    }

    [Fact]
    public void IsValid_Azure_RequiresBaseUrlDeploymentAndKey()
    {
        var provider = AiOnlineProviderCatalog.TryGetById("azure-openai")!;
        var incomplete = Draft(
            "azure-openai",
            (AiProviderFieldIds.BaseUrl, "https://example.openai.azure.com"),
            (AiProviderFieldIds.ApiKey, "key"));
        var complete = Draft(
            "azure-openai",
            (AiProviderFieldIds.BaseUrl, "https://example.openai.azure.com"),
            (AiProviderFieldIds.DeploymentName, "gpt-4o"),
            (AiProviderFieldIds.ApiKey, "key"));

        Assert.False(AiProviderConfigValidator.IsValid(provider, incomplete, requireApiKey: true));
        Assert.True(AiProviderConfigValidator.IsValid(provider, complete, requireApiKey: true));
    }

    [Fact]
    public void IsValid_CustomOpenAi_AllowsMissingApiKey()
    {
        var provider = AiOnlineProviderCatalog.TryGetById("custom-openai")!;
        var draft = Draft(
            "custom-openai",
            (AiProviderFieldIds.BaseUrl, "http://127.0.0.1:11434/v1"),
            (AiProviderFieldIds.ModelId, "llama3.2"));

        Assert.True(AiProviderConfigValidator.IsValid(provider, draft));
    }

    [Fact]
    public void IsConfigured_RequiresStoredApiKeyWhenNotInDraft()
    {
        var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
        var secrets = new InMemoryAiSecretStorage();
        var config = AiProviderConfigValidator.ToConfig(
            provider,
            Draft("openai", (AiProviderFieldIds.ModelId, "gpt-4o-mini"), (AiProviderFieldIds.ApiKey, "sk-test")),
            existing: null);

        Assert.False(AiProviderConfigValidator.IsConfigured(provider, config, secrets));

        secrets.SaveApiKey("openai", "sk-test");
        Assert.True(AiProviderConfigValidator.IsConfigured(provider, config, secrets));
    }

    [Fact]
    public void NormalizeBaseUrl_TrimsTrailingSlash()
    {
        var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
        var draft = Draft(
            "openai",
            (AiProviderFieldIds.BaseUrl, "https://api.openai.com/v1/"),
            (AiProviderFieldIds.ModelId, "gpt-4o-mini"));

        Assert.Equal("https://api.openai.com/v1", AiProviderConfigValidator.NormalizeBaseUrl(provider, draft));
    }

    [Fact]
    public void ToDraft_UsesDefaultBaseUrlWhenMissing()
    {
        var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
        var draft = AiProviderConfigValidator.ToDraft(provider, config: null, apiKey: null);

        Assert.Equal("https://api.openai.com/v1", draft.Values[AiProviderFieldIds.BaseUrl]);
    }

    [Fact]
    public void ResolveModelId_AzureUsesDeploymentName()
    {
        var provider = AiOnlineProviderCatalog.TryGetById("azure-openai")!;
        var draft = Draft(
            "azure-openai",
            (AiProviderFieldIds.DeploymentName, "my-deployment"),
            (AiProviderFieldIds.ModelId, "ignored-model"));

        Assert.Equal("my-deployment", AiProviderConfigValidator.ResolveModelId(provider, draft));
    }
}
