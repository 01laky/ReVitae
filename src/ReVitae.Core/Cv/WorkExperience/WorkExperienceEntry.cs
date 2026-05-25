namespace ReVitae.Core.Cv.WorkExperience;

using System.Globalization;

public sealed class WorkExperienceEntry
{
	public WorkExperienceEntry()
	{
		Id = Guid.NewGuid().ToString("N");
	}

	public WorkExperienceEntry(string id)
	{
		Id = id;
	}

	public string Id { get; }

	public string JobTitle { get; set; } = string.Empty;

	public string Company { get; set; } = string.Empty;

	public string Location { get; set; } = string.Empty;

	public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;

	public int? StartMonth { get; set; }

	public int? StartYear { get; set; }

	public int? EndMonth { get; set; }

	public int? EndYear { get; set; }

	public bool IsCurrentlyWorking { get; set; }

	public string Description { get; set; } = string.Empty;

	public string Achievements { get; set; } = string.Empty;

	public string Technologies { get; set; } = string.Empty;

	public string CompanyUrl { get; set; } = string.Empty;

	public bool HasUserInput()
	{
		if (IsCurrentlyWorking)
		{
			return true;
		}

		if (EmploymentType != EmploymentType.FullTime)
		{
			return true;
		}

		if (StartMonth.HasValue || StartYear.HasValue || EndMonth.HasValue || EndYear.HasValue)
		{
			return true;
		}

		return HasText(JobTitle)
			|| HasText(Company)
			|| HasText(Location)
			|| HasText(Description)
			|| HasText(Achievements)
			|| HasText(Technologies)
			|| HasText(CompanyUrl);
	}

	public WorkExperienceEntry Duplicate()
	{
		var duplicate = new WorkExperienceEntry
		{
			JobTitle = JobTitle,
			Company = Company,
			Location = Location,
			EmploymentType = EmploymentType,
			StartMonth = StartMonth,
			StartYear = StartYear,
			EndMonth = EndMonth,
			EndYear = EndYear,
			IsCurrentlyWorking = IsCurrentlyWorking,
			Description = Description,
			Achievements = Achievements,
			Technologies = Technologies,
			CompanyUrl = CompanyUrl
		};

		return duplicate;
	}

	public IReadOnlyDictionary<string, string?> ToFieldValues()
	{
		return new Dictionary<string, string?>
		{
			[WorkExperienceFieldKeys.Build(Id, WorkExperienceFieldKeys.JobTitle)] = JobTitle,
			[WorkExperienceFieldKeys.Build(Id, WorkExperienceFieldKeys.Company)] = Company,
			[WorkExperienceFieldKeys.Build(Id, WorkExperienceFieldKeys.Location)] = Location,
			[WorkExperienceFieldKeys.Build(Id, WorkExperienceFieldKeys.EmploymentType)] = EmploymentType.ToString(),
			[WorkExperienceFieldKeys.Build(Id, WorkExperienceFieldKeys.StartMonth)] = StartMonth?.ToString(CultureInfo.InvariantCulture),
			[WorkExperienceFieldKeys.Build(Id, WorkExperienceFieldKeys.StartYear)] = StartYear?.ToString(CultureInfo.InvariantCulture),
			[WorkExperienceFieldKeys.Build(Id, WorkExperienceFieldKeys.EndMonth)] = EndMonth?.ToString(CultureInfo.InvariantCulture),
			[WorkExperienceFieldKeys.Build(Id, WorkExperienceFieldKeys.EndYear)] = EndYear?.ToString(CultureInfo.InvariantCulture),
			[WorkExperienceFieldKeys.Build(Id, WorkExperienceFieldKeys.IsCurrentlyWorking)] = IsCurrentlyWorking.ToString(),
			[WorkExperienceFieldKeys.Build(Id, WorkExperienceFieldKeys.Description)] = Description,
			[WorkExperienceFieldKeys.Build(Id, WorkExperienceFieldKeys.Achievements)] = Achievements,
			[WorkExperienceFieldKeys.Build(Id, WorkExperienceFieldKeys.Technologies)] = Technologies,
			[WorkExperienceFieldKeys.Build(Id, WorkExperienceFieldKeys.CompanyUrl)] = CompanyUrl
		};
	}

	public string BuildHeaderSummary(string presentLabel)
	{
		var title = string.IsNullOrWhiteSpace(JobTitle) ? "-" : JobTitle.Trim();
		var company = string.IsNullOrWhiteSpace(Company) ? "-" : Company.Trim();
		var dateRange = BuildDateRangeLabel(presentLabel);
		return $"{title} · {company} · {dateRange}";
	}

	public string BuildDateRangeLabel(string presentLabel)
	{
		var start = FormatPartialDate(StartMonth, StartYear);
		if (IsCurrentlyWorking)
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
