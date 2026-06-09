using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Cv;

public sealed record AiCvCompletionContext(
	AiCvTaskKind Task,
	string CurrentText,
	string? JobTitle = null,
	string? Company = null,
	string? ProjectName = null,
	string? ProfessionalTitle = null,
	// 045 — advisor + relevance context.
	CvImportSectionId? Section = null,
	string? SectionContent = null,
	bool SectionIsEmpty = false,
	AiCvTargetContext? TargetContext = null);
