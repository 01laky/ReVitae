using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Import;

/// <summary>
/// Selects and groups field-repair targets (045 B.2 / C.9). Drops targets without a current
/// value, keeps the lowest-confidence fields first, and caps the batch at
/// <see cref="AiImportLimits.MaxRepairFields"/> — the dropped count is reported, never silently
/// truncated.
/// </summary>
public static class AiImportFieldRepairPlanner
{
	public static IReadOnlyList<AiImportFieldRepairTarget> SelectTargets(
		IReadOnlyList<AiImportFieldRepairTarget> candidates,
		out int dropped)
	{
		var usable = candidates
			.Where(t => !string.IsNullOrWhiteSpace(t.CurrentValue))
			// Confidence enum: High=0, Medium=1, Low=2 → lowest confidence first.
			.OrderByDescending(t => (int)t.Confidence)
			.ToList();

		if (usable.Count <= AiImportLimits.MaxRepairFields)
		{
			dropped = 0;
			return usable;
		}

		dropped = usable.Count - AiImportLimits.MaxRepairFields;
		return usable.Take(AiImportLimits.MaxRepairFields).ToList();
	}

	/// <summary>Groups selected targets by section so each batch shares context.</summary>
	public static IReadOnlyList<IReadOnlyList<AiImportFieldRepairTarget>> GroupBySection(
		IReadOnlyList<AiImportFieldRepairTarget> targets) =>
		targets
			.GroupBy(t => t.Section)
			.Select(g => (IReadOnlyList<AiImportFieldRepairTarget>)g.ToList())
			.ToList();
}
