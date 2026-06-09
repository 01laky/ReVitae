using ReVitae.Core.Import;
using ReVitae.Core.Quality;

namespace ReVitae.Core.Ai.Cv;

public static class AiCvTaskRegistry
{
	private static readonly IReadOnlyDictionary<string, AiCvTaskKind> HintToTask =
		new Dictionary<string, AiCvTaskKind>(StringComparer.Ordinal)
		{
			[CvQualityHintIds.WorkGenericDescription] = AiCvTaskKind.ImproveWorkDescription,
			[CvQualityHintIds.WorkEntryMissingDescription] = AiCvTaskKind.DraftWorkDescription,
			[CvQualityHintIds.PersonalSummaryTooShort] = AiCvTaskKind.ImproveProfessionalSummary,
			[CvQualityHintIds.PersonalSummaryMissing] = AiCvTaskKind.DraftProfessionalSummary,
			[CvQualityHintIds.ProjectsEntryMissingDescription] = AiCvTaskKind.ImproveProjectDescription,

			// 045 A.1 — broadened hint coverage.
			[CvQualityHintIds.PersonalSummaryTooLong] = AiCvTaskKind.ShortenProfessionalSummary,
			[CvQualityHintIds.SkillsSingleLargeGroup] = AiCvTaskKind.SuggestSkillGrouping,
			[CvQualityHintIds.SkillsSectionEmpty] = AiCvTaskKind.DraftSkillsFromContext,
			[CvQualityHintIds.EducationSectionEmpty] = AiCvTaskKind.AdviseEducationSection,
			[CvQualityHintIds.LanguagesSectionEmpty] = AiCvTaskKind.AdviseLanguagesSection,
		};

	// Tasks whose output is a list of short advice items (advisor modal), not a single
	// field value (045 A.3 / C.2 / C.5).
	private static readonly HashSet<AiCvTaskKind> AdviceListTasks =
	[
		AiCvTaskKind.SectionAdvisor,
		AiCvTaskKind.SuggestSkillGrouping,
		AiCvTaskKind.DraftSkillsFromContext,
		AiCvTaskKind.AdviseEducationSection,
		AiCvTaskKind.AdviseLanguagesSection,
		AiCvTaskKind.SuggestMeasurableResults,
	];

	// Rewrite tasks whose output replaces existing CV text and must pass the entity guard
	// (045 C.3).
	private static readonly HashSet<AiCvTaskKind> RewriteTasks =
	[
		AiCvTaskKind.ImproveWorkDescription,
		AiCvTaskKind.ImproveProfessionalSummary,
		AiCvTaskKind.ShortenProfessionalSummary,
		AiCvTaskKind.ImproveProjectDescription,
	];

	public static bool SupportsQualityHint(string hintId) =>
		HintToTask.ContainsKey(hintId);

	public static AiCvTaskKind? TryGetTaskForQualityHint(string hintId) =>
		HintToTask.TryGetValue(hintId, out var task) ? task : null;

	/// <summary>True when the task produces a list of advice items rather than one field value.</summary>
	public static bool ProducesAdviceList(AiCvTaskKind task) => AdviceListTasks.Contains(task);

	/// <summary>True when the task rewrites existing CV text and should be entity-guarded (C.3).</summary>
	public static bool IsRewriteTask(AiCvTaskKind task) => RewriteTasks.Contains(task);

	/// <summary>
	/// True when the task's output language must follow the CV content language rather than
	/// the UI culture (045 C.4) — i.e. anything that writes text into a CV field.
	/// </summary>
	public static bool ProducesCvContent(AiCvTaskKind task) =>
		!ProducesAdviceList(task);

	public static AiCvFieldTarget ResolveFieldTarget(CvQualityHint hint)
	{
		if (hint.Section is null || string.IsNullOrWhiteSpace(hint.FieldKey))
		{
			throw new InvalidOperationException($"Quality hint '{hint.Id}' has no field target.");
		}

		return new AiCvFieldTarget(hint.Section.Value, hint.FieldKey, hint.EntryId);
	}
}
