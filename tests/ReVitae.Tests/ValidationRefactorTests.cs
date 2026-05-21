using ReVitae.Core.Cv;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Validation;

namespace ReVitae.Tests;

public sealed class MonthYearValueTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public void IsValidMonth_AcceptsCalendarMonths(int month)
    {
        Assert.True(MonthYearValue.IsValidMonth(month));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void IsValidMonth_RejectsOutOfRange(int month)
    {
        Assert.False(MonthYearValue.IsValidMonth(month));
    }

    [Theory]
    [InlineData(1950)]
    [InlineData(2024)]
    [InlineData(2100)]
    public void IsValidYear_AcceptsSupportedRange(int year)
    {
        Assert.True(MonthYearValue.IsValidYear(year));
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(2101)]
    public void IsValidYear_RejectsOutOfRange(int year)
    {
        Assert.False(MonthYearValue.IsValidYear(year));
    }

    [Fact]
    public void TryParse_ReturnsFalseWhenMonthOrYearMissing()
    {
        Assert.False(MonthYearValue.TryParse(null, 2020, out _));
        Assert.False(MonthYearValue.TryParse(5, null, out _));
    }

    [Fact]
    public void TryParse_ReturnsFalseForInvalidComponents()
    {
        Assert.False(MonthYearValue.TryParse(0, 2020, out _));
        Assert.False(MonthYearValue.TryParse(5, 1800, out _));
    }

    [Fact]
    public void TryParse_ReturnsValueForValidInput()
    {
        Assert.True(MonthYearValue.TryParse(3, 2021, out var value));
        Assert.NotNull(value);
        Assert.Equal(3, value!.Month);
        Assert.Equal(2021, value.Year);
    }

    [Fact]
    public void CompareTo_OrdersByYearThenMonth()
    {
        Assert.True(MonthYearValue.TryParse(12, 2020, out var earlier));
        Assert.True(MonthYearValue.TryParse(1, 2021, out var later));

        Assert.True(earlier!.CompareTo(later) < 0);
        Assert.True(later!.CompareTo(earlier) > 0);
    }

    [Fact]
    public void Format_UsesZeroPaddedMonth()
    {
        var value = new MonthYearValue(4, 2022);

        Assert.Equal("04 / 2022", value.Format());
    }
}

public sealed class FieldValidatorFormatEdgeCaseTests
{
    private static FieldValidator CreateValidator(params FieldSchema[] schemas) => new(schemas);

    [Theory]
    [InlineData("1")]
    [InlineData("12")]
    public void ValidateField_Month_AcceptsValidMonths(string month)
    {
        var validator = CreateValidator(new FieldSchema(
            "month", "Month", false, 2, FieldFormat.Month, string.Empty, "max", "invalid"));

        Assert.True(validator.ValidateField("month", month).IsValid);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("13")]
    [InlineData("abc")]
    public void ValidateField_Month_RejectsInvalidMonths(string month)
    {
        var validator = CreateValidator(new FieldSchema(
            "month", "Month", false, 2, FieldFormat.Month, string.Empty, "max", "invalid"));

        Assert.False(validator.ValidateField("month", month).IsValid);
    }

    [Theory]
    [InlineData("1950")]
    [InlineData("2100")]
    public void ValidateField_Year_AcceptsSupportedYears(string year)
    {
        var validator = CreateValidator(new FieldSchema(
            "year", "Year", false, 4, FieldFormat.Year, string.Empty, "max", "invalid"));

        Assert.True(validator.ValidateField("year", year).IsValid);
    }

    [Fact]
    public void ValidateField_EmploymentType_IsCaseSensitive()
    {
        var validator = CreateValidator(new FieldSchema(
            "type", "Type", false, 32, FieldFormat.EmploymentType, string.Empty, "max", "invalid"));

        Assert.True(validator.ValidateField("type", EmploymentType.FullTime.ToString()).IsValid);
        Assert.False(validator.ValidateField("type", EmploymentType.FullTime.ToString().ToLowerInvariant()).IsValid);
    }

    [Fact]
    public void ValidateField_DegreeType_RejectsUnknownValue()
    {
        var validator = CreateValidator(new FieldSchema(
            "degree", "Degree", false, 32, FieldFormat.DegreeType, string.Empty, "max", "invalid"));

        Assert.False(validator.ValidateField("degree", "DoctoratePlus").IsValid);
    }

    [Fact]
    public void ValidateField_CefrLevel_AcceptsKnownLevels()
    {
        var validator = CreateValidator(new FieldSchema(
            "cefr", "CEFR", false, 4, FieldFormat.CefrLevel, string.Empty, "max", "invalid"));

        Assert.True(validator.ValidateField("cefr", "B2").IsValid);
        Assert.False(validator.ValidateField("cefr", "Z9").IsValid);
    }
}

public sealed class CollectionEntryValidationHelperTests
{
    [Fact]
    public void ValidateStartBeforeEnd_AddsErrorWhenStartIsAfterEnd()
    {
        var errors = new List<FieldValidationError>();

        CollectionEntryValidationHelper.ValidateStartBeforeEnd(
            errors,
            "entry.dateRange",
            "start.after.end",
            12,
            2022,
            1,
            2022,
            skipEndValidation: false);

        Assert.Single(errors);
        Assert.Equal("start.after.end", errors[0].Message);
    }

    [Fact]
    public void ValidateStartBeforeEnd_SkipsWhenCurrentlyActive()
    {
        var errors = new List<FieldValidationError>();

        CollectionEntryValidationHelper.ValidateStartBeforeEnd(
            errors,
            "entry.dateRange",
            "start.after.end",
            12,
            2022,
            1,
            2022,
            skipEndValidation: true);

        Assert.Empty(errors);
    }
}
