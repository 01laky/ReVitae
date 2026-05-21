namespace ReVitae.Core.Cv;

public sealed record MonthYearValue(int Month, int Year)
{
    public static bool TryParse(int? month, int? year, out MonthYearValue? value)
    {
        value = null;
        if (month is null || year is null)
        {
            return false;
        }

        if (!IsValidMonth(month.Value) || !IsValidYear(year.Value))
        {
            return false;
        }

        value = new MonthYearValue(month.Value, year.Value);
        return true;
    }

    public static bool IsValidMonth(int month) => month is >= 1 and <= 12;

    public static bool IsValidYear(int year) => year is >= 1950 and <= 2100;

    public int CompareTo(MonthYearValue? other)
    {
        if (other is null)
        {
            return 1;
        }

        var yearComparison = Year.CompareTo(other.Year);
        return yearComparison != 0 ? yearComparison : Month.CompareTo(other.Month);
    }

    public string Format() => $"{Month:D2} / {Year}";
}
