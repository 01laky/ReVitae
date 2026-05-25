using ReVitae.Core.Cv;
using ReVitae.Ui.Validation;

namespace ReVitae.Tests.Ui.Validation;

public sealed class MonthYearDateHelperEdgeCaseTests
{
	[Fact]
	public void ToSelectedDate_ValidMonthYear_ReturnsFirstOfMonthUtc()
	{
		var date = MonthYearDateHelper.ToSelectedDate(3, 2024);

		Assert.NotNull(date);
		Assert.Equal(2024, date!.Value.Year);
		Assert.Equal(3, date.Value.Month);
		Assert.Equal(1, date.Value.Day);
	}

	[Theory]
	[InlineData(null, 2024)]
	[InlineData(5, null)]
	public void ToSelectedDate_PartialInput_ReturnsNull(int? month, int? year)
	{
		Assert.Null(MonthYearDateHelper.ToSelectedDate(month, year));
	}

	[Fact]
	public void ToSelectedDate_InvalidMonth_ReturnsNull()
	{
		Assert.Null(MonthYearDateHelper.ToSelectedDate(13, 2024));
		Assert.Null(MonthYearDateHelper.ToSelectedDate(0, 2024));
	}

	[Fact]
	public void FromSelectedDate_RoundTripsMonthYear()
	{
		var selected = new DateTimeOffset(2022, 7, 1, 0, 0, 0, TimeSpan.Zero);

		var (month, year) = MonthYearDateHelper.FromSelectedDate(selected);

		Assert.Equal(7, month);
		Assert.Equal(2022, year);
		Assert.Equal(selected, MonthYearDateHelper.ToSelectedDate(month, year));
	}

	[Fact]
	public void FromSelectedDate_Null_ReturnsNullTuple()
	{
		var (month, year) = MonthYearDateHelper.FromSelectedDate(null);

		Assert.Null(month);
		Assert.Null(year);
	}

	[Fact]
	public void CreatePicker_ReturnsConfiguredDatePicker()
	{
		var changed = false;
		var picker = MonthYearDateHelper.CreatePicker((_, _) => changed = true);

		Assert.NotNull(picker);
		Assert.False(changed);
	}
}
