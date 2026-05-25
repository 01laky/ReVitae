using ReVitae.Core.Cv;

namespace ReVitae.Core.Validation;

public static class CollectionEntryValidationHelper
{
	public static bool IsEndDateField(string schemaKey, string endMonthFieldName, string endYearFieldName)
	{
		return schemaKey.EndsWith("." + endMonthFieldName, StringComparison.Ordinal)
			|| schemaKey.EndsWith("." + endYearFieldName, StringComparison.Ordinal);
	}

	public static FieldValidationError? ValidateRequiredEndDateWhenInactive(
		string schemaKey,
		string? value,
		string endMonthFieldName,
		string endMonthRequiredKey,
		string endYearRequiredKey)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var message = schemaKey.EndsWith("." + endMonthFieldName, StringComparison.Ordinal)
			? endMonthRequiredKey
			: endYearRequiredKey;

		return new FieldValidationError(schemaKey, message);
	}

	public static void ValidateStartBeforeEnd(
		ICollection<FieldValidationError> errors,
		string dateRangeFieldKey,
		string startAfterEndMessageKey,
		int? startMonth,
		int? startYear,
		int? endMonth,
		int? endYear,
		bool skipEndValidation)
	{
		if (skipEndValidation)
		{
			return;
		}

		if (MonthYearValue.TryParse(startMonth, startYear, out var startDate)
			&& MonthYearValue.TryParse(endMonth, endYear, out var endDate)
			&& startDate!.CompareTo(endDate) > 0)
		{
			errors.Add(new FieldValidationError(dateRangeFieldKey, startAfterEndMessageKey));
		}
	}
}
