using System.Globalization;
using System.Text.RegularExpressions;

namespace ReVitae.Core.Import.Patterns;

public static class CvImportPatterns
{
    public static readonly Regex Email = new(
        @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static readonly Regex Phone = new(
        @"(?:\+|\(\+)?[0-9][0-9 \t().-]{7,}[0-9]",
        RegexOptions.Compiled);

    public static readonly Regex Url = new(
        @"https?://[^\s<>()]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static readonly Regex DateRange = new(
        @"(?<start>(?:\d{1,2}/\d{4}|[A-Za-z]{3,9}\s+\d{4}|\d{4}))(?:\s*[-–]\s*(?<end>(?:\d{1,2}/\d{4}|[A-Za-z]{3,9}\s+\d{4}|\d{4}|present|current|súčasnosť|sucasnost)))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static readonly Regex LabeledValue = new(
        @"^(?<label>[A-Za-zÀ-ž\s]+):\s*(?<value>.+)$",
        RegexOptions.Compiled);

    public static bool TryParseDateToken(string token, out int? month, out int? year)
    {
        month = null;
        year = null;
        token = token.Trim();

        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var yearOnly)
            && yearOnly is >= 1950 and <= 2100)
        {
            year = yearOnly;
            return true;
        }

        var slashMatch = Regex.Match(token, @"^(?<month>\d{1,2})/(?<year>\d{4})$");
        if (slashMatch.Success
            && int.TryParse(slashMatch.Groups["month"].Value, out var parsedMonth)
            && int.TryParse(slashMatch.Groups["year"].Value, out var parsedYear)
            && parsedMonth is >= 1 and <= 12)
        {
            month = parsedMonth;
            year = parsedYear;
            return true;
        }

        if (DateTime.TryParseExact(
                token,
                "MMM yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var monthYear))
        {
            month = monthYear.Month;
            year = monthYear.Year;
            return true;
        }

        return false;
    }

    public static bool IsPresentToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        return token.Equals("present", StringComparison.OrdinalIgnoreCase)
            || token.Equals("current", StringComparison.OrdinalIgnoreCase)
            || token.Equals("súčasnosť", StringComparison.OrdinalIgnoreCase)
            || token.Equals("sucasnost", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record ParsedDateRange(
    int? StartMonth,
    int? StartYear,
    int? EndMonth,
    int? EndYear,
    bool IsPresent);

public static class DateRangeParser
{
    public static bool TryParse(string? line, out ParsedDateRange range)
    {
        range = new ParsedDateRange(null, null, null, null, false);
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var match = CvImportPatterns.DateRange.Match(line);
        if (!match.Success)
        {
            return false;
        }

        var hasStart = CvImportPatterns.TryParseDateToken(match.Groups["start"].Value, out var startMonth, out var startYear);
        var endToken = match.Groups["end"].Success ? match.Groups["end"].Value : null;
        var isPresent = CvImportPatterns.IsPresentToken(endToken);
        var hasEnd = !string.IsNullOrWhiteSpace(endToken)
            && !isPresent
            && CvImportPatterns.TryParseDateToken(endToken!, out var endMonth, out var endYear);

        if (!hasStart && !hasEnd && !isPresent)
        {
            return false;
        }

        int? parsedEndMonth = null;
        int? parsedEndYear = null;
        if (hasEnd)
        {
            CvImportPatterns.TryParseDateToken(endToken!, out parsedEndMonth, out parsedEndYear);
        }

        range = new ParsedDateRange(
            hasStart ? startMonth : null,
            hasStart ? startYear : null,
            parsedEndMonth,
            parsedEndYear,
            isPresent);
        return true;
    }
}
