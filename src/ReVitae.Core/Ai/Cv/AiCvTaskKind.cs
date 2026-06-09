namespace ReVitae.Core.Ai.Cv;

public enum AiCvTaskKind
{
	ImproveWorkDescription = 0,
	DraftWorkDescription = 1,
	ImproveProfessionalSummary = 2,
	DraftProfessionalSummary = 3,
	ImproveProjectDescription = 4,

	// 045 — broadened hint-mapped tasks (A.1) and measurable-results (C.2).
	ShortenProfessionalSummary = 5,
	SuggestSkillGrouping = 6,
	DraftSkillsFromContext = 7,
	AdviseEducationSection = 8,
	AdviseLanguagesSection = 9,
	SuggestMeasurableResults = 10,

	// 045 — generic per-section advisor (A.2).
	SectionAdvisor = 200,

	ExtractCvImportBatch = 100,
}
