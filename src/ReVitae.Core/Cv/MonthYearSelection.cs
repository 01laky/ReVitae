namespace ReVitae.Core.Cv;

public static class MonthYearSelection
{
	public static DateTimeOffset? ToDateTimeOffset(int? month, int? year)
	{
		if (!month.HasValue || !year.HasValue)
		{
			return null;
		}

		if (!MonthYearValue.IsValidMonth(month.Value))
		{
			return null;
		}

		return new DateTimeOffset(year.Value, month.Value, 1, 0, 0, 0, TimeSpan.Zero);
	}

	public static (int? Month, int? Year) FromDateTimeOffset(DateTimeOffset? selectedDate)
	{
		if (!selectedDate.HasValue)
		{
			return (null, null);
		}

		return (selectedDate.Value.Month, selectedDate.Value.Year);
	}

	public static bool IsValid(int? month, int? year) =>
		month is >= 1 and <= 12 && MonthYearValue.IsValidYear(year ?? 0);
}
