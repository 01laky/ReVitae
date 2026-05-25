using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Ai.Providers.Chat;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Cv;

public sealed class OllamaChatBackendRuntime : IAiBackendRuntime
{
	private readonly string _modelTag;
	private readonly string _displayNameKey;
	private readonly IOllamaChatClient _chatClient;

	public OllamaChatBackendRuntime(
		string modelTag,
		string displayNameKey,
		IOllamaChatClient? chatClient = null)
	{
		_modelTag = modelTag;
		_displayNameKey = displayNameKey;
		_chatClient = chatClient ?? new OllamaChatClient();
	}

	public AiBackendKind Kind => AiBackendKind.Local;

	public string DescribeActiveBackend(AppLocalizer localizer) =>
		localizer.Format(TranslationKeys.AiCvBackendLocal, localizer.Get(_displayNameKey));

	public Task<AiChatCompletionResult> CompleteAsync(
		AiCvPromptMessages messages,
		CancellationToken cancellationToken = default) =>
		_chatClient.ChatAsync(_modelTag, messages, cancellationToken);
}

public sealed class OnlineChatBackendRuntime : IAiBackendRuntime
{
	private readonly AiOnlineProviderDefinition _provider;
	private readonly AiProviderConnectionDraft _draft;
	private readonly ChatCompletionClientFactory _clientFactory;

	public OnlineChatBackendRuntime(
		AiOnlineProviderDefinition provider,
		AiProviderConnectionDraft draft,
		ChatCompletionClientFactory? clientFactory = null)
	{
		_provider = provider;
		_draft = draft;
		_clientFactory = clientFactory ?? new ChatCompletionClientFactory();
	}

	public AiBackendKind Kind => AiBackendKind.Online;

	public string DescribeActiveBackend(AppLocalizer localizer) =>
		localizer.Format(
			TranslationKeys.AiCvBackendOnline,
			localizer.Get(_provider.DisplayNameKey));

	public Task<AiChatCompletionResult> CompleteAsync(
		AiCvPromptMessages messages,
		CancellationToken cancellationToken = default)
	{
		var client = _clientFactory.Create(_provider.ApiStyle);
		return client.CompleteWithMessagesAsync(_provider, _draft, messages, cancellationToken: cancellationToken);
	}
}
