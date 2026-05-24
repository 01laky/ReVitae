using ReVitae.Core.Ai.Cv;

namespace ReVitae.Core.Ai.Providers.Chat;

public interface IChatCompletionClient
{
    Task<AiChatCompletionResult> CompleteAsync(
        AiOnlineProviderDefinition provider,
        AiProviderConnectionDraft draft,
        string prompt,
        CancellationToken cancellationToken = default);

    Task<AiChatCompletionResult> CompleteWithMessagesAsync(
        AiOnlineProviderDefinition provider,
        AiProviderConnectionDraft draft,
        AiCvPromptMessages messages,
        int maxTokens = 512,
        CancellationToken cancellationToken = default);
}

public static class AiProviderTestPrompt
{
    public const string Message = "Reply with exactly OK";
}
