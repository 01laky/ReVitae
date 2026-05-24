using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Providers;

namespace ReVitae.Tests.Ai.Providers;

public sealed class AiSettingsRepositoryTests : IDisposable
{
    private readonly string _directory;
    private readonly string _settingsPath;

    public AiSettingsRepositoryTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "revitae-ai-provider-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _settingsPath = Path.Combine(_directory, "ai-settings.json");
    }

    [Fact]
    public void SaveAndLoad_RoundTripsV2Document()
    {
        var repository = new AiSettingsRepository(_settingsPath);
        var settings = new AiSettingsDocument(
            AiSettingsDocument.CurrentSchemaVersion,
            AiBackendKind.Online,
            null,
            "openai",
            new LocalAiSettingsRecord("gemma2-2b", "gemma2:2b", DateTimeOffset.UtcNow),
            new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal)
            {
                ["openai"] = new(
                    "openai",
                    "gpt-4o-mini",
                    "https://api.openai.com/v1",
                    null,
                    null,
                    null,
                    DateTimeOffset.UtcNow,
                    true),
            });

        repository.Save(settings);
        var loaded = repository.LoadOrDefault();

        Assert.Equal(AiBackendKind.Online, loaded.ActiveBackend);
        Assert.Equal("openai", loaded.ActiveOnlineProviderId);
        Assert.True(loaded.OnlineProviders.ContainsKey("openai"));
        Assert.Equal("gemma2-2b", loaded.Local?.SelectedModelId);
    }

    [Fact]
    public void LoadOrDefault_MigratesLegacyV1SnapshotToLocalActive()
    {
        File.WriteAllText(
            _settingsPath,
            """
            {
              "selectedModelId": "medium-instruct",
              "ollamaModelTag": "llama3.1:8b-instruct",
              "downloadedAtUtc": "2026-05-21T12:00:00Z"
            }
            """);

        var loaded = new AiSettingsRepository(_settingsPath).LoadOrDefault();

        Assert.Equal(AiBackendKind.Local, loaded.ActiveBackend);
        Assert.Equal("medium-instruct", loaded.ActiveLocalModelId);
        Assert.Equal("llama3.1:8b-instruct", loaded.Local?.OllamaModelTag);
    }

    [Fact]
    public void Save_DoesNotPersistApiKeysInJson()
    {
        var repository = new AiSettingsRepository(_settingsPath);
        repository.Save(AiSettingsDocument.Empty with
        {
            OnlineProviders = new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal)
            {
                ["openai"] = new(
                    "openai",
                    "gpt-4o-mini",
                    "https://api.openai.com/v1",
                    null,
                    null,
                    null,
                    null,
                    true),
            },
        });

        var json = File.ReadAllText(_settingsPath);

        Assert.DoesNotContain("sk-", json, StringComparison.Ordinal);
        Assert.DoesNotContain("apiKey", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadOrDefault_ReturnsEmptyWhenFileMissing()
    {
        var loaded = new AiSettingsRepository(_settingsPath).LoadOrDefault();

        Assert.Equal(AiBackendKind.None, loaded.ActiveBackend);
        Assert.Empty(loaded.OnlineProviders);
    }

    [Fact]
    public void LoadOrDefault_ReturnsEmptyWhenJsonCorrupt()
    {
        File.WriteAllText(_settingsPath, "{ not valid json");

        var loaded = new AiSettingsRepository(_settingsPath).LoadOrDefault();

        Assert.Equal(AiBackendKind.None, loaded.ActiveBackend);
        Assert.Empty(loaded.OnlineProviders);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
