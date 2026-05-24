using ReVitae.Core.Ai.Providers;

namespace ReVitae.Tests.Ai.Providers;

public sealed class AiSecretStorageTests : IDisposable
{
    private readonly string _directory;
    private readonly string _secretsPath;
    private readonly string _keyPath;

    public AiSecretStorageTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "revitae-ai-secret-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _secretsPath = Path.Combine(_directory, "ai-secrets.enc");
        _keyPath = Path.Combine(_directory, "ai-secrets.key");
    }

    [Fact]
    public void InMemoryStorage_IsolatesProviderKeys()
    {
        var storage = new InMemoryAiSecretStorage();
        storage.SaveApiKey("openai", "sk-openai");
        storage.SaveApiKey("anthropic", "sk-anthropic");

        Assert.Equal("sk-openai", storage.TryGetApiKey("openai"));
        Assert.Equal("sk-anthropic", storage.TryGetApiKey("anthropic"));
        Assert.Null(storage.TryGetApiKey("groq"));
    }

    [Fact]
    public void InMemoryStorage_DeleteRemovesOnlyTargetProvider()
    {
        var storage = new InMemoryAiSecretStorage();
        storage.SaveApiKey("openai", "sk-openai");
        storage.SaveApiKey("groq", "sk-groq");

        storage.DeleteApiKey("openai");

        Assert.Null(storage.TryGetApiKey("openai"));
        Assert.Equal("sk-groq", storage.TryGetApiKey("groq"));
    }

    [Fact]
    public void FileStorage_RoundTripsEncryptedSecrets()
    {
        var storage = new FileAiSecretStorage(_secretsPath, _keyPath);
        storage.SaveApiKey("openai", "sk-secret-value");
        storage.SaveApiKey("anthropic", "sk-other");

        var reloaded = new FileAiSecretStorage(_secretsPath, _keyPath);

        Assert.Equal("sk-secret-value", reloaded.TryGetApiKey("openai"));
        Assert.Equal("sk-other", reloaded.TryGetApiKey("anthropic"));
        Assert.DoesNotContain("sk-secret-value", File.ReadAllText(_secretsPath));
    }

    [Fact]
    public void FileStorage_DeleteAllRemovesEncryptedFile()
    {
        var storage = new FileAiSecretStorage(_secretsPath, _keyPath);
        storage.SaveApiKey("openai", "sk-secret");
        storage.DeleteAll();

        Assert.False(File.Exists(_secretsPath));
        Assert.Null(storage.TryGetApiKey("openai"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
