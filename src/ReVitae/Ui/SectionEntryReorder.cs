using System;
using System.Collections.Generic;

namespace ReVitae.Ui;

/// <summary>
/// Shared drag-to-reorder list logic for entry-list section views (047 T2). Six section views
/// previously held a byte-identical <c>MoveEntryToIndex</c>; this is the single, unit-tested
/// implementation. Pure list manipulation — no UI dependency — so callers invoke it and only
/// rebuild/notify when it reports a real move.
/// </summary>
public static class SectionEntryReorder
{
	/// <summary>
	/// Moves the entry identified by <paramref name="sourceEntryId"/> to
	/// <paramref name="targetIndex"/> within <paramref name="entries"/>, accounting for the
	/// removal shift. Returns <c>true</c> only when the list actually changed.
	/// </summary>
	public static bool MoveToIndex<T>(
		List<T> entries,
		Func<T, string> idSelector,
		string? sourceEntryId,
		int targetIndex)
	{
		ArgumentNullException.ThrowIfNull(entries);
		ArgumentNullException.ThrowIfNull(idSelector);

		if (string.IsNullOrWhiteSpace(sourceEntryId))
		{
			return false;
		}

		var sourceIndex = entries.FindIndex(entry => idSelector(entry) == sourceEntryId);
		if (sourceIndex < 0 || sourceIndex == targetIndex)
		{
			return false;
		}

		var entry = entries[sourceIndex];
		entries.RemoveAt(sourceIndex);
		if (targetIndex > sourceIndex)
		{
			targetIndex--;
		}

		entries.Insert(Math.Clamp(targetIndex, 0, entries.Count), entry);
		return true;
	}
}
