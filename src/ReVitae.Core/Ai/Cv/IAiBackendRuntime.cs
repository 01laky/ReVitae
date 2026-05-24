using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Cv;

public interface IAiBackendRuntime
{
    AiBackendKind Kind { get; }

    string DescribeActiveBackend(AppLocalizer localizer);

    Task<AiChatCompletionResult> CompleteAsync(
        AiCvPromptMessages messages,
        CancellationToken cancellationToken = default);
}
