using ReVitae.Core.Ai.Providers;

namespace ReVitae.Tests.Ai.Providers;

public sealed class AiProviderRowStateMapperTests
{
	[Fact]
	public void Map_UnconfiguredProvider_ShowsConfigureAction()
	{
		var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
		var presentation = AiProviderRowStateMapper.Map(
			provider,
			AiSettingsDocument.Empty,
			new InMemoryAiSecretStorage());

		Assert.Equal(AiProviderUiAction.Configure, presentation.PrimaryAction);
		Assert.False(presentation.ShowEditLink);
		Assert.False(presentation.IsConfigured);
		Assert.False(presentation.IsActive);
	}

	[Fact]
	public void Map_ConfiguredInactiveProvider_ShowsActivateAndEdit()
	{
		var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
		var secrets = new InMemoryAiSecretStorage();
		secrets.SaveApiKey("openai", "sk-test");
		var settings = SettingsWithOpenAiConfigured();

		var presentation = AiProviderRowStateMapper.Map(provider, settings, secrets);

		Assert.Equal(AiProviderUiAction.Activate, presentation.PrimaryAction);
		Assert.True(presentation.ShowEditLink);
		Assert.True(presentation.IsConfigured);
		Assert.False(presentation.IsActive);
	}

	[Fact]
	public void Map_ActiveProvider_ShowsDeactivateAndEdit()
	{
		var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
		var secrets = new InMemoryAiSecretStorage();
		secrets.SaveApiKey("openai", "sk-test");
		var settings = SettingsWithOpenAiConfigured() with
		{
			ActiveBackend = AiBackendKind.Online,
			ActiveOnlineProviderId = "openai",
		};

		var presentation = AiProviderRowStateMapper.Map(provider, settings, secrets);

		Assert.Equal(AiProviderUiAction.Deactivate, presentation.PrimaryAction);
		Assert.True(presentation.ShowEditLink);
		Assert.True(presentation.IsActive);
	}

	[Fact]
	public void Map_PropagatesLastTestResult()
	{
		var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
		var secrets = new InMemoryAiSecretStorage();
		secrets.SaveApiKey("openai", "sk-test");
		var config = new AiProviderConnectionConfig(
			"openai",
			"gpt-4o-mini",
			"https://api.openai.com/v1",
			null,
			null,
			null,
			DateTimeOffset.UtcNow,
			false);
		var settings = AiSettingsDocument.Empty with
		{
			OnlineProviders = new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal)
			{
				["openai"] = config,
			},
		};

		var presentation = AiProviderRowStateMapper.Map(provider, settings, secrets);

		Assert.False(presentation.LastTestSucceeded);
	}

	private static AiSettingsDocument SettingsWithOpenAiConfigured()
	{
		var config = new AiProviderConnectionConfig(
			"openai",
			"gpt-4o-mini",
			"https://api.openai.com/v1",
			null,
			null,
			null,
			null,
			true);

		return AiSettingsDocument.Empty with
		{
			OnlineProviders = new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal)
			{
				["openai"] = config,
			},
		};
	}
}
