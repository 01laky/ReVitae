using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Cv;

/// <summary>
/// Optional, session-only role / job-description bias for AI tasks (045 C.1).
/// Never persisted to the CV or project; when both fields are empty the prompts
/// behave exactly as without it.
/// </summary>
public sealed record AiCvTargetContext(string? Role, string? JobDescriptionExcerpt)
{
	public static AiCvTargetContext Empty { get; } = new(null, null);

	public bool HasValue =>
		!string.IsNullOrWhiteSpace(Role) || !string.IsNullOrWhiteSpace(JobDescriptionExcerpt);
}

/// <summary>One advice item parsed from model output (045 C.5): advice plus optional "why".</summary>
public sealed record AiCvParsedAdvice(string Advice, string? Rationale);

/// <summary>
/// A single advisor suggestion. <see cref="ApplyTarget"/> / <see cref="ApplyValue"/> are
/// non-null only when the suggestion carries a concrete value that can be written into a
/// field via the 039 accept path; otherwise it is advice-only. <see cref="Rationale"/>
/// is the short "why" line (045 C.5), null when the model omits it.
/// </summary>
public sealed record AiCvAdvisorSuggestion(
	string Text,
	AiCvFieldTarget? ApplyTarget = null,
	string? ApplyValue = null,
	string? Rationale = null);

/// <summary>
/// Result of <see cref="AiCvCompletionService.AdviseSectionAsync"/> (045 A.2 / A.3).
/// </summary>
public sealed record AiCvAdvisorResult(
	bool Succeeded,
	IReadOnlyList<AiCvAdvisorSuggestion> Suggestions,
	string? ErrorMessageKey,
	AiCvBackendDescriptor? BackendUsed,
	CvImportSectionId Section,
	bool Cancelled = false,
	bool FromCache = false)
{
	public static AiCvAdvisorResult Fail(
		string errorMessageKey,
		CvImportSectionId section,
		AiCvBackendDescriptor? backendUsed = null) =>
		new(false, [], errorMessageKey, backendUsed, section);

	public static AiCvAdvisorResult CancelledResult(CvImportSectionId section) =>
		new(false, [], null, null, section, Cancelled: true);
}
