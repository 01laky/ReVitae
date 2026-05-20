namespace ReVitae.Core.Cv.Education;

using System.Globalization;

public sealed class EducationEntry
{
    public EducationEntry()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    public EducationEntry(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public string Institution { get; set; } = string.Empty;

    public string Degree { get; set; } = string.Empty;

    public string FieldOfStudy { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DegreeType DegreeType { get; set; } = DegreeType.Bachelor;

    public int? StartMonth { get; set; }

    public int? StartYear { get; set; }

    public int? EndMonth { get; set; }

    public int? EndYear { get; set; }

    public bool IsCurrentlyStudying { get; set; }

    public string Grade { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string InstitutionUrl { get; set; } = string.Empty;

    public bool HasUserInput()
    {
        if (IsCurrentlyStudying)
        {
            return true;
        }

        if (DegreeType != DegreeType.Bachelor)
        {
            return true;
        }

        if (StartMonth.HasValue || StartYear.HasValue || EndMonth.HasValue || EndYear.HasValue)
        {
            return true;
        }

        return HasText(Institution)
            || HasText(Degree)
            || HasText(FieldOfStudy)
            || HasText(Location)
            || HasText(Grade)
            || HasText(Description)
            || HasText(InstitutionUrl);
    }

    public EducationEntry Duplicate()
    {
        return new EducationEntry
        {
            Institution = Institution,
            Degree = Degree,
            FieldOfStudy = FieldOfStudy,
            Location = Location,
            DegreeType = DegreeType,
            StartMonth = StartMonth,
            StartYear = StartYear,
            EndMonth = EndMonth,
            EndYear = EndYear,
            IsCurrentlyStudying = IsCurrentlyStudying,
            Grade = Grade,
            Description = Description,
            InstitutionUrl = InstitutionUrl
        };
    }

    public IReadOnlyDictionary<string, string?> ToFieldValues()
    {
        return new Dictionary<string, string?>
        {
            [EducationFieldKeys.Build(Id, EducationFieldKeys.Institution)] = Institution,
            [EducationFieldKeys.Build(Id, EducationFieldKeys.Degree)] = Degree,
            [EducationFieldKeys.Build(Id, EducationFieldKeys.FieldOfStudy)] = FieldOfStudy,
            [EducationFieldKeys.Build(Id, EducationFieldKeys.Location)] = Location,
            [EducationFieldKeys.Build(Id, EducationFieldKeys.DegreeType)] = DegreeType.ToString(),
            [EducationFieldKeys.Build(Id, EducationFieldKeys.StartMonth)] = StartMonth?.ToString(CultureInfo.InvariantCulture),
            [EducationFieldKeys.Build(Id, EducationFieldKeys.StartYear)] = StartYear?.ToString(CultureInfo.InvariantCulture),
            [EducationFieldKeys.Build(Id, EducationFieldKeys.EndMonth)] = EndMonth?.ToString(CultureInfo.InvariantCulture),
            [EducationFieldKeys.Build(Id, EducationFieldKeys.EndYear)] = EndYear?.ToString(CultureInfo.InvariantCulture),
            [EducationFieldKeys.Build(Id, EducationFieldKeys.IsCurrentlyStudying)] = IsCurrentlyStudying.ToString(),
            [EducationFieldKeys.Build(Id, EducationFieldKeys.Grade)] = Grade,
            [EducationFieldKeys.Build(Id, EducationFieldKeys.Description)] = Description,
            [EducationFieldKeys.Build(Id, EducationFieldKeys.InstitutionUrl)] = InstitutionUrl
        };
    }

    public string BuildHeaderSummary(string presentLabel)
    {
        var degree = string.IsNullOrWhiteSpace(Degree) ? "-" : Degree.Trim();
        var institution = string.IsNullOrWhiteSpace(Institution) ? "-" : Institution.Trim();
        var dateRange = BuildDateRangeLabel(presentLabel);
        return $"{degree} · {institution} · {dateRange}";
    }

    public string BuildDateRangeLabel(string presentLabel)
    {
        var start = FormatPartialDate(StartMonth, StartYear);
        if (IsCurrentlyStudying)
        {
            return string.IsNullOrEmpty(start) ? presentLabel : $"{start} – {presentLabel}";
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
