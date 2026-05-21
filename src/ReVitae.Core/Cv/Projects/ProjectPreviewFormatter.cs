using System.Globalization;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Cv.Projects;

public static class ProjectPreviewFormatter
{
    public static string FormatMainLine(ProjectEntry entry, AppLocalizer localizer)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(entry.Name))
        {
            parts.Add(entry.Name.Trim());
        }

        if (!string.IsNullOrWhiteSpace(entry.Role))
        {
            parts.Add(entry.Role.Trim());
        }

        if (!string.IsNullOrWhiteSpace(entry.Organization))
        {
            parts.Add(entry.Organization.Trim());
        }

        var dateRange = FormatPreviewDateRange(entry, localizer);
        if (!string.IsNullOrEmpty(dateRange))
        {
            parts.Add(dateRange);
        }

        return string.Join(" · ", parts);
    }

    public static IReadOnlyList<string> FormatDetailLines(ProjectEntry entry, AppLocalizer localizer)
    {
        var lines = new List<string>();
        var technologies = entry.Technologies
            .Where(technology => technology.HasUserInput())
            .Select(technology => technology.Name.Trim())
            .ToArray();

        if (technologies.Length > 0)
        {
            lines.Add($"{localizer.Get(TranslationKeys.PreviewTechnologies)}: {string.Join(", ", technologies)}");
        }

        if (!string.IsNullOrWhiteSpace(entry.ProjectUrl))
        {
            lines.Add($"{localizer.Get(TranslationKeys.ProjectsProjectUrl)}: {entry.ProjectUrl.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(entry.Highlights))
        {
            lines.Add($"{localizer.Get(TranslationKeys.PreviewHighlights)}: {entry.Highlights.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(entry.Description))
        {
            lines.Add(entry.Description.Trim());
        }

        return lines;
    }

    private static string FormatPreviewDateRange(ProjectEntry entry, AppLocalizer localizer)
    {
        var presentLabel = localizer.Get(TranslationKeys.ProjectsPresent);
        var start = FormatPreviewDate(entry.StartMonth, entry.StartYear);
        if (entry.IsCurrentlyActive)
        {
            return string.IsNullOrEmpty(start) ? presentLabel : $"{start} – {presentLabel}";
        }

        var end = FormatPreviewDate(entry.EndMonth, entry.EndYear);
        if (string.IsNullOrEmpty(start) && string.IsNullOrEmpty(end))
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(start))
        {
            return end;
        }

        if (string.IsNullOrEmpty(end))
        {
            return start;
        }

        return $"{start} – {end}";
    }

    private static string FormatPreviewDate(int? month, int? year)
    {
        if (month is null || year is null)
        {
            return string.Empty;
        }

        var date = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return date.ToString("MMM yyyy", CultureInfo.CurrentCulture);
    }
}
