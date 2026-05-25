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
		};

	public static bool SupportsQualityHint(string hintId) =>
		HintToTask.ContainsKey(hintId);

	public static AiCvTaskKind? TryGetTaskForQualityHint(string hintId) =>
		HintToTask.TryGetValue(hintId, out var task) ? task : null;

	public static AiCvFieldTarget ResolveFieldTarget(CvQualityHint hint)
	{
		if (hint.Section is null || string.IsNullOrWhiteSpace(hint.FieldKey))
		{
			throw new InvalidOperationException($"Quality hint '{hint.Id}' has no field target.");
		}

		return new AiCvFieldTarget(hint.Section.Value, hint.FieldKey, hint.EntryId);
	}
}
