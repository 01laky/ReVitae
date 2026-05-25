namespace ReVitae.Core.Cv.WorkExperience;

public static class WorkExperienceFieldKeys
{
	public const string Prefix = "workExperience";

	public const string JobTitle = "jobTitle";
	public const string Company = "company";
	public const string Location = "location";
	public const string EmploymentType = "employmentType";
	public const string StartMonth = "startMonth";
	public const string StartYear = "startYear";
	public const string EndMonth = "endMonth";
	public const string EndYear = "endYear";
	public const string IsCurrentlyWorking = "isCurrentlyWorking";
	public const string Description = "description";
	public const string Achievements = "achievements";
	public const string Technologies = "technologies";
	public const string CompanyUrl = "companyUrl";
	public const string DateRange = "dateRange";

	public static string Build(string entryId, string fieldName)
	{
		return $"{Prefix}.{entryId}.{fieldName}";
	}

	public static bool TryParseEntryId(string fieldKey, out string entryId, out string fieldName)
	{
		entryId = string.Empty;
		fieldName = string.Empty;

		if (!fieldKey.StartsWith(Prefix + ".", StringComparison.Ordinal))
		{
			return false;
		}

		var remainder = fieldKey[(Prefix.Length + 1)..];
		var separatorIndex = remainder.IndexOf('.', StringComparison.Ordinal);
		if (separatorIndex <= 0 || separatorIndex >= remainder.Length - 1)
		{
			return false;
		}

		entryId = remainder[..separatorIndex];
		fieldName = remainder[(separatorIndex + 1)..];
		return true;
	}
}
