namespace ReVitae.Core.Cv.Certificates;

public static class CertificateSorter
{
	public static IReadOnlyList<CertificateEntry> SortByDateNewestFirst(IReadOnlyList<CertificateEntry> entries)
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
			.OrderByDescending(item => ToSortValue(item.Entry.IssueYear, item.Entry.IssueMonth))
			.ThenBy(item => item.Index)
			.Select(item => item.Entry)
			.ToList();

		active.AddRange(drafts);
		return active;
	}

	private static long ToSortValue(int? year, int? month)
	{
		if (year is null || month is null)
		{
			return -1;
		}

		return (year.Value * 100L) + month.Value;
	}

	private sealed record IndexedEntry(CertificateEntry Entry, int Index);
}
