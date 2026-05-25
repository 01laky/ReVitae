namespace ReVitae.Core.Cv.Education;

public static class EducationFieldKeys
{
	public const string Prefix = "education";

	public const string Institution = "institution";
	public const string Degree = "degree";
	public const string FieldOfStudy = "fieldOfStudy";
	public const string Location = "location";
	public const string DegreeType = "degreeType";
	public const string StartMonth = "startMonth";
	public const string StartYear = "startYear";
	public const string EndMonth = "endMonth";
	public const string EndYear = "endYear";
	public const string IsCurrentlyStudying = "isCurrentlyStudying";
	public const string Grade = "grade";
	public const string Description = "description";
	public const string InstitutionUrl = "institutionUrl";
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
