namespace ReVitae.Core.Cv.Projects;

using System.Globalization;

public sealed class ProjectEntry
{
    public ProjectEntry()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    public ProjectEntry(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Organization { get; set; } = string.Empty;

    public int? StartMonth { get; set; }

    public int? StartYear { get; set; }

    public int? EndMonth { get; set; }

    public int? EndYear { get; set; }

    public bool IsCurrentlyActive { get; set; }

    public string ProjectUrl { get; set; } = string.Empty;

    public List<ProjectTechnologyItem> Technologies { get; } = [];

    public string Highlights { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool HasUserInput()
    {
        if (IsCurrentlyActive)
        {
            return true;
        }

        if (StartMonth.HasValue || StartYear.HasValue || EndMonth.HasValue || EndYear.HasValue)
        {
            return true;
        }

        if (Technologies.Any(technology => technology.HasUserInput()))
        {
            return true;
        }

        return HasText(Name)
            || HasText(Role)
            || HasText(Organization)
            || HasText(ProjectUrl)
            || HasText(Highlights)
            || HasText(Description);
    }

    public ProjectEntry Duplicate()
    {
        var duplicate = new ProjectEntry
        {
            Name = Name,
            Role = Role,
            Organization = Organization,
            StartMonth = StartMonth,
            StartYear = StartYear,
            EndMonth = EndMonth,
            EndYear = EndYear,
            IsCurrentlyActive = IsCurrentlyActive,
            ProjectUrl = ProjectUrl,
            Highlights = Highlights,
            Description = Description
        };

        foreach (var technology in Technologies)
        {
            duplicate.Technologies.Add(technology.Duplicate());
        }

        return duplicate;
    }

    public IReadOnlyDictionary<string, string?> ToFieldValues()
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [ProjectsFieldKeys.Build(Id, ProjectsFieldKeys.Name)] = Name,
            [ProjectsFieldKeys.Build(Id, ProjectsFieldKeys.Role)] = Role,
            [ProjectsFieldKeys.Build(Id, ProjectsFieldKeys.Organization)] = Organization,
            [ProjectsFieldKeys.Build(Id, ProjectsFieldKeys.StartMonth)] = StartMonth?.ToString(CultureInfo.InvariantCulture),
            [ProjectsFieldKeys.Build(Id, ProjectsFieldKeys.StartYear)] = StartYear?.ToString(CultureInfo.InvariantCulture),
            [ProjectsFieldKeys.Build(Id, ProjectsFieldKeys.EndMonth)] = EndMonth?.ToString(CultureInfo.InvariantCulture),
            [ProjectsFieldKeys.Build(Id, ProjectsFieldKeys.EndYear)] = EndYear?.ToString(CultureInfo.InvariantCulture),
            [ProjectsFieldKeys.Build(Id, ProjectsFieldKeys.IsCurrentlyActive)] = IsCurrentlyActive.ToString(),
            [ProjectsFieldKeys.Build(Id, ProjectsFieldKeys.ProjectUrl)] = ProjectUrl,
            [ProjectsFieldKeys.Build(Id, ProjectsFieldKeys.Highlights)] = Highlights,
            [ProjectsFieldKeys.Build(Id, ProjectsFieldKeys.Description)] = Description
        };

        foreach (var technology in Technologies)
        {
            values[ProjectsFieldKeys.BuildTechnology(Id, technology.Id, ProjectsFieldKeys.TechnologyName)] =
                technology.Name;
        }

        return values;
    }

    public string BuildHeaderSummary(string presentLabel)
    {
        var name = string.IsNullOrWhiteSpace(Name) ? "-" : Name.Trim();
        var dateRange = BuildDateRangeLabel(presentLabel);
        return dateRange == "-"
            ? name
            : $"{name} · {dateRange}";
    }

    public string BuildDateRangeLabel(string presentLabel)
    {
        var start = FormatPartialDate(StartMonth, StartYear);
        if (IsCurrentlyActive)
        {
            if (string.IsNullOrEmpty(start))
            {
                return presentLabel;
            }

            return $"{start} – {presentLabel}";
        }

        var end = FormatPartialDate(EndMonth, EndYear);
        if (string.IsNullOrEmpty(start) && string.IsNullOrEmpty(end))
        {
            return "-";
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

    private static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);

    private static string FormatPartialDate(int? month, int? year)
    {
        if (month is null || year is null)
        {
            return string.Empty;
        }

        return $"{month.Value:D2} / {year.Value}";
    }
}
