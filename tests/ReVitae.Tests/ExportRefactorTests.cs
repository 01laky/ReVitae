using ReVitae.Core.Cv;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Export;
using ReVitae.Core.Localization;

namespace ReVitae.Tests;

public sealed class MonthYearSelectionTests
{
	[Fact]
	public void ToDateTimeOffset_ReturnsNullWhenMonthOrYearMissing()
	{
		Assert.Null(MonthYearSelection.ToDateTimeOffset(null, 2020));
		Assert.Null(MonthYearSelection.ToDateTimeOffset(5, null));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(13)]
	public void ToDateTimeOffset_ReturnsNullForInvalidMonth(int month)
	{
		Assert.Null(MonthYearSelection.ToDateTimeOffset(month, 2020));
	}

	[Fact]
	public void ToDateTimeOffset_ReturnsFirstDayOfMonth()
	{
		var date = MonthYearSelection.ToDateTimeOffset(3, 2021);

		Assert.NotNull(date);
		Assert.Equal(2021, date!.Value.Year);
		Assert.Equal(3, date.Value.Month);
		Assert.Equal(1, date.Value.Day);
	}

	[Fact]
	public void FromDateTimeOffset_ReturnsNullPairWhenMissing()
	{
		var (month, year) = MonthYearSelection.FromDateTimeOffset(null);

		Assert.Null(month);
		Assert.Null(year);
	}

	[Fact]
	public void FromDateTimeOffset_RoundTripsWithToDateTimeOffset()
	{
		var selected = MonthYearSelection.ToDateTimeOffset(8, 2019);
		var (month, year) = MonthYearSelection.FromDateTimeOffset(selected);

		Assert.Equal(8, month);
		Assert.Equal(2019, year);
	}

	[Theory]
	[InlineData(1, 2020, true)]
	[InlineData(12, 2100, true)]
	[InlineData(0, 2020, false)]
	[InlineData(5, 1949, false)]
	public void IsValid_ReflectsMonthYearRules(int month, int year, bool expected)
	{
		Assert.Equal(expected, MonthYearSelection.IsValid(month, year));
	}
}

public sealed class CvExportDocumentMapperTests
{
	private readonly AppLocalizer _localizer = new(AppLocalizer.FallbackLanguageCode);

	[Fact]
	public void NormalizeRequired_UsesDashForBlankValues()
	{
		Assert.Equal("-", CvExportDocumentMapper.NormalizeRequired(null));
		Assert.Equal("-", CvExportDocumentMapper.NormalizeRequired("   "));
		Assert.Equal("Jane", CvExportDocumentMapper.NormalizeRequired("  Jane  "));
	}

	[Fact]
	public void NormalizeOptional_UsesEmptyStringForBlankValues()
	{
		Assert.Equal(string.Empty, CvExportDocumentMapper.NormalizeOptional(null));
		Assert.Equal("Remote", CvExportDocumentMapper.NormalizeOptional(" Remote "));
	}

	[Fact]
	public void MapWorkExperience_BuildsExportEntryWithPresentLabel()
	{
		var entry = new ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry("work-1")
		{
			JobTitle = "Engineer",
			Company = "Acme",
			Location = "Bratislava",
			IsCurrentlyWorking = true,
			StartMonth = 1,
			StartYear = 2020
		};

		var mapped = CvExportDocumentMapper.MapWorkExperience(entry, _localizer);

		Assert.Equal("Engineer", mapped.JobTitle);
		Assert.Equal("Acme", mapped.Company);
		Assert.Equal("Bratislava", mapped.Location);
		Assert.Contains(_localizer.Get(TranslationKeys.WorkExperiencePresent), mapped.DateRange, StringComparison.Ordinal);
	}

	[Fact]
	public void MapSkillsGroup_SkipsEmptySkillNames()
	{
		var group = new ReVitae.Core.Cv.Skills.SkillsGroupEntry("skills-1")
		{
			Category = "Languages",
			Skills =
			{
				new ReVitae.Core.Cv.Skills.SkillItem("skill-1")
				{
					Name = "C#",
					Proficiency = ReVitae.Core.Cv.Skills.ProficiencyLevel.Advanced
				},
				new ReVitae.Core.Cv.Skills.SkillItem("skill-2") { Name = "   " }
			}
		};

		var mapped = CvExportDocumentMapper.MapSkillsGroup(group, _localizer);

		Assert.Single(mapped.Skills);
		Assert.Equal("C#", mapped.Skills[0].Name);
	}
}
