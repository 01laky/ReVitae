namespace ReVitae.Core.Cv.Projects;

public static class ProjectSorter
{
    public static IReadOnlyList<ProjectEntry> SortByDateNewestFirst(IReadOnlyList<ProjectEntry> entries)
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
            .OrderByDescending(item => ToSortValue(item.Entry.StartYear, item.Entry.StartMonth))
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

    private sealed record IndexedEntry(ProjectEntry Entry, int Index);
}
