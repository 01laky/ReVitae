using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Import;

/// <summary>
/// A single low-confidence field selected for targeted AI repair (045 B.2). Built by joining
/// <see cref="ImportedFieldConfidence"/> (FieldKey + Confidence) with the parsed document to
/// recover the section, optional entry id, and current value. Fields whose current value
/// cannot be resolved are not turned into targets.
/// </summary>
public sealed record AiImportFieldRepairTarget(
	CvImportSectionId Section,
	string FieldKey,
	string? EntryId,
	string CurrentValue,
	CvImportConfidence Confidence);

/// <summary>One repaired field: the original target plus the model's corrected value.</summary>
public sealed record AiImportFieldRepairResult(
	AiImportFieldRepairTarget Target,
	string RepairedValue)
{
	public bool Changed => !string.Equals(Target.CurrentValue.Trim(), RepairedValue.Trim(), StringComparison.Ordinal);
}

/// <summary>
/// Outcome of <see cref="AiCvImportFieldRepairService.RepairImportFieldsAsync"/> (045 B.2).
/// Distinct from <see cref="AiCvImportOutcome"/> — it carries per-field before/after data and
/// the cap accounting (045 C.9), not section counts.
/// </summary>
public sealed record AiCvImportRepairOutcome(
	bool Succeeded,
	IReadOnlyList<AiImportFieldRepairResult> Repairs,
	int RequestedFieldCount,
	int SentFieldCount,
	int DroppedFieldCount,
	int BatchesFailed,
	string? ErrorMessageKey,
	AiCvBackendDescriptor? BackendUsed,
	bool Cancelled = false)
{
	public int ChangedFieldCount => Repairs.Count(r => r.Changed);

	public static AiCvImportRepairOutcome Fail(string errorMessageKey, int requested) =>
		new(false, [], requested, 0, 0, 0, errorMessageKey, null);

	public static AiCvImportRepairOutcome CancelledResult(int requested, int sent, int dropped) =>
		new(false, [], requested, sent, dropped, 0, null, null, Cancelled: true);
}
