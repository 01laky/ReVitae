using ReVitae.Core.Ai.Providers;

namespace ReVitae.Tests.Ai.Providers;

public sealed class AiActiveBackendPresentationTests
{
    [Fact]
    public void GetSnapshot_ReturnsNoneWhenNoActiveBackend()
    {
        var snapshot = AiActiveBackendPresentation.GetSnapshot(AiSettingsDocument.Empty);

        Assert.Equal(AiBackendKind.None, snapshot.Kind);
        Assert.Null(snapshot.DisplayNameKey);
    }

    [Fact]
    public void GetOnlineModelLabel_ReturnsConfiguredModelId()
    {
        var settings = AiSettingsDocument.Empty with
        {
            OnlineProviders = new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal)
            {
                ["openai"] = new("openai", "gpt-4o-mini", "https://api.openai.com/v1", null, null, null, null, true),
            },
        };

        Assert.Equal("gpt-4o-mini", AiActiveBackendPresentation.GetOnlineModelLabel(settings, "openai"));
    }

    [Fact]
    public void GetSnapshot_OnlineIncludesProviderDisplayNameKey()
    {
        var settings = AiSettingsDocument.Empty with
        {
            ActiveBackend = AiBackendKind.Online,
            ActiveOnlineProviderId = "anthropic",
            OnlineProviders = new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal)
            {
                ["anthropic"] = new(
                    "anthropic",
                    "claude-3-5-haiku-20241022",
                    "https://api.anthropic.com",
                    null,
                    null,
                    null,
                    null,
                    true),
            },
        };

        var snapshot = AiActiveBackendPresentation.GetSnapshot(settings);

        Assert.Equal(AiBackendKind.Online, snapshot.Kind);
        Assert.Equal("anthropic", snapshot.OnlineProviderId);
        Assert.NotNull(snapshot.DisplayNameKey);
        Assert.Equal("claude-3-5-haiku-20241022", snapshot.ModelLabel);
    }

    [Fact]
    public void TryActivateLocal_InvalidModelId_ReturnsFalse()
    {
        var directory = Path.Combine(Path.GetTempPath(), "revitae-ai-active-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var config = new AiProviderConfigService(
            new AiSettingsRepository(Path.Combine(directory, "ai-settings.json")),
            new InMemoryAiSecretStorage(),
            new AiProviderConnectionTester());
        config.Load();
        var backend = new AiActiveBackendService(config);

        Assert.False(backend.TryActivateLocal("not-a-real-model"));
        Assert.Equal(AiBackendKind.None, backend.GetActiveSnapshot().Kind);

        Directory.Delete(directory, recursive: true);
    }
}
