using ReVitae.Core.Ai.Providers;

namespace ReVitae.Tests.Ai.Providers;

public sealed class AiActiveBackendServiceTests
{
    private static (AiProviderConfigService Config, AiActiveBackendService Backend, InMemoryAiSecretStorage Secrets) Create()
    {
        var directory = Path.Combine(Path.GetTempPath(), "revitae-ai-active-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var repository = new AiSettingsRepository(Path.Combine(directory, "ai-settings.json"));
        var secrets = new InMemoryAiSecretStorage();
        var config = new AiProviderConfigService(repository, secrets, new AiProviderConnectionTester());
        config.Load();
        return (config, new AiActiveBackendService(config), secrets);
    }

    [Fact]
    public void TryActivateOnline_FailsWhenProviderNotConfigured()
    {
        var (_, backend, _) = Create();

        Assert.False(backend.TryActivateOnline("openai"));
        Assert.Equal(AiBackendKind.None, backend.GetActiveSnapshot().Kind);
    }

    [Fact]
    public void TryActivateOnline_SwitchesActiveBackendExclusively()
    {
        var (config, backend, secrets) = Create();
        secrets.SaveApiKey("openai", "sk-test");
        config.SaveProviderConfig(
            "openai",
            OpenAiDraft("sk-test"),
            lastTestSucceeded: true,
            lastTestedAtUtc: DateTimeOffset.UtcNow);
        backend.TryActivateLocal("gemma2-2b");

        Assert.True(backend.TryActivateOnline("openai"));

        var snapshot = backend.GetActiveSnapshot();
        Assert.Equal(AiBackendKind.Online, snapshot.Kind);
        Assert.Equal("openai", snapshot.OnlineProviderId);
    }

    [Fact]
    public void Deactivate_ClearsActiveBackend()
    {
        var (config, backend, secrets) = Create();
        secrets.SaveApiKey("openai", "sk-test");
        config.SaveProviderConfig("openai", OpenAiDraft("sk-test"));
        backend.TryActivateOnline("openai");

        backend.Deactivate();

        Assert.Equal(AiBackendKind.None, backend.GetActiveSnapshot().Kind);
    }

    [Fact]
    public void RequiresSwitchConfirmation_ReturnsFalseWhenNoActiveBackend()
    {
        var (_, backend, _) = Create();

        Assert.False(backend.RequiresSwitchConfirmation(AiBackendKind.Online, "openai"));
        Assert.False(backend.RequiresSwitchConfirmation(AiBackendKind.Local, "gemma2-2b"));
    }

    [Fact]
    public void RequiresSwitchConfirmation_ReturnsTrueWhenSwitchingBetweenKinds()
    {
        var (config, backend, secrets) = Create();
        secrets.SaveApiKey("openai", "sk-test");
        config.SaveProviderConfig("openai", OpenAiDraft("sk-test"));
        backend.TryActivateOnline("openai");

        Assert.True(backend.RequiresSwitchConfirmation(AiBackendKind.Local, "gemma2-2b"));
    }

    [Fact]
    public void RequiresSwitchConfirmation_ReturnsFalseWhenReactivatingSameTarget()
    {
        var (config, backend, secrets) = Create();
        secrets.SaveApiKey("openai", "sk-test");
        config.SaveProviderConfig("openai", OpenAiDraft("sk-test"));
        backend.TryActivateOnline("openai");

        Assert.False(backend.RequiresSwitchConfirmation(AiBackendKind.Online, "openai"));
    }

    [Fact]
    public void NeedsUntestedActivationWarning_IsTrueWithoutSuccessfulTest()
    {
        var (config, backend, secrets) = Create();
        secrets.SaveApiKey("openai", "sk-test");
        config.SaveProviderConfig("openai", OpenAiDraft("sk-test"), lastTestSucceeded: false);

        Assert.True(backend.NeedsUntestedActivationWarning("openai"));
    }

    [Fact]
    public void NeedsUntestedActivationWarning_IsFalseAfterSuccessfulTest()
    {
        var (config, backend, secrets) = Create();
        secrets.SaveApiKey("openai", "sk-test");
        config.SaveProviderConfig(
            "openai",
            OpenAiDraft("sk-test"),
            lastTestSucceeded: true,
            lastTestedAtUtc: DateTimeOffset.UtcNow);

        Assert.False(backend.NeedsUntestedActivationWarning("openai"));
    }

    [Fact]
    public void GetActiveSnapshot_BuildsLocalDisplayMetadata()
    {
        var (_, backend, _) = Create();
        backend.TryActivateLocal("gemma2-2b");

        var snapshot = backend.GetActiveSnapshot();

        Assert.Equal(AiBackendKind.Local, snapshot.Kind);
        Assert.Equal("gemma2-2b", snapshot.LocalModelId);
        Assert.NotNull(snapshot.DisplayNameKey);
        Assert.Equal("gemma2:2b", snapshot.ModelLabel);
    }

    private static AiProviderConnectionDraft OpenAiDraft(string apiKey) =>
        new(
            "openai",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [AiProviderFieldIds.ModelId] = "gpt-4o-mini",
                [AiProviderFieldIds.BaseUrl] = "https://api.openai.com/v1",
            },
            apiKey);
}
