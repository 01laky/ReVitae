using ReVitae.Core.Ai.Providers.Chat;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Providers;

public sealed class AiProviderConnectionTester
{
	private readonly ChatCompletionClientFactory _clientFactory;

	public AiProviderConnectionTester()
		: this(new ChatCompletionClientFactory())
	{
	}

	public AiProviderConnectionTester(ChatCompletionClientFactory clientFactory)
	{
		_clientFactory = clientFactory;
	}

	public async Task<AiProviderTestResult> TestAsync(
		AiOnlineProviderDefinition provider,
		AiProviderConnectionDraft draft,
		CancellationToken cancellationToken = default)
	{
		if (!AiProviderConfigValidator.IsValid(provider, draft))
		{
			return new AiProviderTestResult(
				false,
				AiProviderConnectionErrorKind.InvalidConfiguration,
				TranslationKeys.AiSetupProviderModelNotFound,
				null);
		}

		if (provider.Id != "custom-openai" && string.IsNullOrWhiteSpace(draft.ApiKey))
		{
			return new AiProviderTestResult(
				false,
				AiProviderConnectionErrorKind.InvalidKey,
				TranslationKeys.AiSetupProviderInvalidKey,
				null);
		}

		var client = _clientFactory.Create(provider.ApiStyle);
		var result = await client
			.CompleteAsync(provider, draft, AiProviderTestPrompt.Message, cancellationToken)
			.ConfigureAwait(false);

		if (result.Succeeded)
		{
			return new AiProviderTestResult(true, AiProviderConnectionErrorKind.None, null, null);
		}

		var errorKind = result.ErrorMessage switch
		{
			TranslationKeys.AiSetupProviderInvalidKey => AiProviderConnectionErrorKind.InvalidKey,
			TranslationKeys.AiSetupProviderModelNotFound => AiProviderConnectionErrorKind.ModelNotFound,
			TranslationKeys.AiSetupProviderRateLimited => AiProviderConnectionErrorKind.RateLimited,
			TranslationKeys.AiSetupProviderUnavailable => AiProviderConnectionErrorKind.ProviderUnavailable,
			_ => AiProviderConnectionErrorKind.NetworkError,
		};

		return new AiProviderTestResult(false, errorKind, result.ErrorMessage, null);
	}
}
