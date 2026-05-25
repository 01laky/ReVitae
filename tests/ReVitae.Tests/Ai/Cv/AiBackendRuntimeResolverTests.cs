using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiBackendRuntimeResolverTests
{
	private readonly AiBackendRuntimeResolver _resolver = new();

	[Fact]
	public void Resolve_LocalActiveWithTag_ReturnsOllamaRuntime()
	{
		var settings = new AiSettingsDocument(
			AiSettingsDocument.CurrentSchemaVersion,
			AiBackendKind.Local,
			"gemma2-2b",
			null,
			new LocalAiSettingsRecord("gemma2-2b", "gemma2:2b", DateTimeOffset.UtcNow),
			new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal));

		var result = _resolver.Resolve(settings, new InMemorySecretStorage());

		Assert.True(result.IsAvailable);
		Assert.IsType<OllamaChatBackendRuntime>(result.Runtime);
	}

	[Fact]
	public void Resolve_OnlineActiveOpenAi_ReturnsOnlineRuntime()
	{
		var settings = new AiSettingsDocument(
			AiSettingsDocument.CurrentSchemaVersion,
			AiBackendKind.Online,
			null,
			"openai",
			null,
			new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal)
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
			});

		var secrets = new InMemorySecretStorage();
		secrets.SaveApiKey("openai", "sk-test");

		var result = _resolver.Resolve(settings, secrets);

		Assert.True(result.IsAvailable);
		Assert.IsType<OnlineChatBackendRuntime>(result.Runtime);
	}

	[Fact]
	public void Resolve_None_ReturnsNoBackendConfigured()
	{
		var result = _resolver.Resolve(AiSettingsDocument.Empty, new InMemorySecretStorage());

		Assert.False(result.IsAvailable);
		Assert.Equal(AiBackendUnavailableReason.NoBackendConfigured, result.UnavailableReason);
		Assert.Equal(TranslationKeys.AiCvNoBackendConfigured, result.ErrorMessageKey);
	}

	[Fact]
	public void Resolve_LocalMissingTag_ReturnsUnavailable()
	{
		var settings = new AiSettingsDocument(
			AiSettingsDocument.CurrentSchemaVersion,
			AiBackendKind.Local,
			"unknown-model-id",
			null,
			null,
			new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal));

		var result = _resolver.Resolve(settings, new InMemorySecretStorage());

		Assert.False(result.IsAvailable);
		Assert.Equal(AiBackendUnavailableReason.LocalModelTagMissing, result.UnavailableReason);
	}

	[Fact]
	public void ResolveLocalModelTag_UsesCatalogFallback()
	{
		var settings = new AiSettingsDocument(
			AiSettingsDocument.CurrentSchemaVersion,
			AiBackendKind.Local,
			"gemma2-2b",
			null,
			null,
			new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal));

		var tag = AiBackendRuntimeResolver.ResolveLocalModelTag(settings, "gemma2-2b");

		Assert.Equal("gemma2:2b", tag);
	}

	private sealed class InMemorySecretStorage : IAiSecretStorage
	{
		private readonly Dictionary<string, string> _keys = new(StringComparer.Ordinal);

		public void SaveApiKey(string providerId, string apiKey) => _keys[providerId] = apiKey;

		public string? TryGetApiKey(string providerId) =>
			_keys.TryGetValue(providerId, out var key) ? key : null;

		public void DeleteApiKey(string providerId) => _keys.Remove(providerId);

		public void DeleteAll() => _keys.Clear();
	}
}
