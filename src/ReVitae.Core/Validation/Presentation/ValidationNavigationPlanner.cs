namespace ReVitae.Core.Validation.Presentation;

public static class ValidationNavigationPlanner
{
	public static string? GetFirstInvalidFieldKey(
		IReadOnlyList<string> orderedFieldKeys,
		IReadOnlyCollection<string> invalidFieldKeys)
	{
		if (invalidFieldKeys.Count == 0)
		{
			return null;
		}

		var invalidSet = invalidFieldKeys.ToHashSet(StringComparer.Ordinal);
		foreach (var fieldKey in orderedFieldKeys)
		{
			if (invalidSet.Contains(fieldKey))
			{
				return fieldKey;
			}
		}

		return invalidFieldKeys.FirstOrDefault();
	}

	public static IReadOnlyList<string> CollectInvalidKeys(
		IReadOnlyList<FieldValidationError> errors) =>
		errors.Select(error => error.FieldKey).Distinct(StringComparer.Ordinal).ToArray();
}
