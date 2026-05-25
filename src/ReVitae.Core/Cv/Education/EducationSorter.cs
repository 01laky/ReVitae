namespace ReVitae.Core.Cv.Education;

public static class EducationSorter
{
	public static IReadOnlyList<EducationEntry> SortByDateNewestFirst(IReadOnlyList<EducationEntry> entries)
	{
		var indexedEntries = entries
			.Select((entry, index) => new IndexedEntry(entry, index))
			.ToArray();

		var drafts = indexedEntries
			.Where(item => !item.Entry.HasUserInput())
			.OrderBy(item => item.Index)
			.Select(item => item.Entry)
			.ToList();

		var active = indexedEntries
			.Where(item => item.Entry.HasUserInput())
			.OrderByDescending(item => BuildSortScore(item.Entry, item.Index))
			.Select(item => item.Entry)
			.ToList();

		active.AddRange(drafts);
		return active;
	}

	private static SortScore BuildSortScore(EducationEntry entry, int originalIndex)
	{
		if (entry.IsCurrentlyStudying)
		{
			return new SortScore(true, long.MaxValue, long.MaxValue, originalIndex);
		}

		var start = ToSortValue(entry.StartYear, entry.StartMonth);
		var end = ToSortValue(entry.EndYear, entry.EndMonth);
		return new SortScore(false, start, end, originalIndex);
	}

	private static long ToSortValue(int? year, int? month)
	{
		if (year is null || month is null)
		{
			return -1;
		}

		return (year.Value * 100L) + month.Value;
	}

	private sealed record IndexedEntry(EducationEntry Entry, int Index);

	private readonly record struct SortScore(bool IsCurrent, long Start, long End, int OriginalIndex)
		: IComparable<SortScore>
	{
		public int CompareTo(SortScore other)
		{
			if (IsCurrent != other.IsCurrent)
			{
				return IsCurrent.CompareTo(other.IsCurrent);
			}

			var startComparison = Start.CompareTo(other.Start);
			if (startComparison != 0)
			{
				return startComparison;
			}

			var endComparison = End.CompareTo(other.End);
			return endComparison != 0 ? endComparison : OriginalIndex.CompareTo(other.OriginalIndex);
		}
	}
}
