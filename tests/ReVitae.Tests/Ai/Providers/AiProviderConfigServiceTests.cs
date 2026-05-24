using ReVitae.Core.Ai.Providers;

namespace ReVitae.Tests.Ai.Providers;

public sealed class AiProviderConfigServiceTests
{
    private static AiProviderConfigService CreateService(
        out AiSettingsRepository repository,
        out InMemoryAiSecretStorage secrets,
        out string directory)
    {
        directory = Path.Combine(Path.GetTempPath(), "revitae-ai-config-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        repository = new AiSettingsRepository(Path.Combine(directory, "ai-settings.json"));
        secrets = new InMemoryAiSecretStorage();
        return new AiProviderConfigService(repository, secrets, new AiProviderConnectionTester());
    }

    [Fact]
    public void SaveProviderConfig_PersistsConfigWithoutApiKeyInSettings()
    {
        var directory = Path.Combine(Path.GetTempPath(), "revitae-ai-config-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var settingsPath = Path.Combine(directory, "ai-settings.json");
        var repository = new AiSettingsRepository(settingsPath);
        var secrets = new InMemoryAiSecretStorage();
        var service = new AiProviderConfigService(repository, secrets, new AiProviderConnectionTester());
        service.Load();
        var draft = OpenAiDraft("sk-test-key");

        service.SaveProviderConfig("openai", draft);

        Assert.Equal("sk-test-key", secrets.TryGetApiKey("openai"));
        Assert.True(service.IsProviderConfigured("openai"));
        var json = File.ReadAllText(settingsPath);
        Assert.DoesNotContain("sk-test-key", json, StringComparison.Ordinal);
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void ResetProviderConfig_DeactivatesWhenActiveAndClearsSecrets()
    {
        var service = CreateService(out _, out var secrets, out var directory);
        service.Load();
        service.SaveProviderConfig("openai", OpenAiDraft("sk-test"));
        service.UpdateActiveBackend(AiBackendKind.Online, null, "openai");

        service.ResetProviderConfig("openai");

        Assert.Null(secrets.TryGetApiKey("openai"));
        Assert.Equal(AiBackendKind.None, service.CurrentSettings.ActiveBackend);
        Assert.False(service.CurrentSettings.OnlineProviders.ContainsKey("openai"));
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void SaveLocalDownloadCompletion_AutoActivatesWhenNoBackendSelected()
    {
        var service = CreateService(out _, out _, out var directory);
        service.Load();

        service.SaveLocalDownloadCompletion("gemma2-2b", "gemma2:2b", DateTimeOffset.UtcNow);

        Assert.Equal(AiBackendKind.Local, service.CurrentSettings.ActiveBackend);
        Assert.Equal("gemma2-2b", service.CurrentSettings.ActiveLocalModelId);
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void SaveLocalDownloadCompletion_DoesNotSwitchAwayFromOnlineBackend()
    {
        var service = CreateService(out _, out var secrets, out var directory);
        service.Load();
        service.SaveProviderConfig("openai", OpenAiDraft("sk-test"));
        service.UpdateActiveBackend(AiBackendKind.Online, null, "openai");

        service.SaveLocalDownloadCompletion("gemma2-2b", "gemma2:2b", DateTimeOffset.UtcNow);

        Assert.Equal(AiBackendKind.Online, service.CurrentSettings.ActiveBackend);
        Assert.Equal("openai", service.CurrentSettings.ActiveOnlineProviderId);
        Assert.Equal("gemma2-2b", service.CurrentSettings.Local?.SelectedModelId);
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void ClearLocalSettingsIfMatches_DeactivatesWhenActiveLocalMatches()
    {
        var service = CreateService(out _, out _, out var directory);
        service.Load();
        service.SaveLocalDownloadCompletion("gemma2-2b", "gemma2:2b", DateTimeOffset.UtcNow);

        service.ClearLocalSettingsIfMatches("gemma2-2b");

        Assert.Null(service.CurrentSettings.Local);
        Assert.Equal(AiBackendKind.None, service.CurrentSettings.ActiveBackend);
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void GetDraft_ReusesStoredApiKeyWhenConfigExists()
    {
        var service = CreateService(out _, out var secrets, out var directory);
        service.Load();
        secrets.SaveApiKey("openai", "sk-stored");
        service.SaveProviderConfig(
            "openai",
            OpenAiDraft("sk-stored"),
            lastTestSucceeded: true,
            lastTestedAtUtc: DateTimeOffset.UtcNow);

        var draft = service.GetDraft("openai");

        Assert.Equal("sk-stored", draft.ApiKey);
        Assert.Equal("gpt-4o-mini", draft.Values[AiProviderFieldIds.ModelId]);
        Directory.Delete(directory, recursive: true);
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
