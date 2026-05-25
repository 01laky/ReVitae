using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Cv;

public enum AiBackendUnavailableReason
{
	None = 0,
	NoBackendConfigured = 1,
	LocalModelTagMissing = 2,
	OnlineProviderMisconfigured = 3,
}

public sealed record AiCvBackendDescriptor(AiBackendKind Kind, string Label);

public sealed record AiCvBackendStatus(
	AiBackendKind Kind,
	bool IsAvailable,
	AiBackendUnavailableReason UnavailableReason,
	string? UnavailableMessageKey,
	ActiveAiBackendSnapshot? Snapshot);

public sealed record AiCvCompletionResult(
	bool Succeeded,
	string? SuggestedText,
	string? ErrorMessageKey,
	AiCvBackendDescriptor? BackendUsed,
	bool Cancelled = false);
