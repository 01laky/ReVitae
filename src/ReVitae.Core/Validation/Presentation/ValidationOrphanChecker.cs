namespace ReVitae.Core.Validation.Presentation;

public static class ValidationOrphanChecker
{
    public static IReadOnlyList<string> FindOrphanErrors(
        IReadOnlyList<FieldValidationError> errors,
        IReadOnlyCollection<string> registeredFieldKeys,
        Func<FieldValidationError, string>? resolveTargetKey = null)
    {
        resolveTargetKey ??= error => error.FieldKey;
        var registered = registeredFieldKeys.ToHashSet(StringComparer.Ordinal);
        var orphans = new List<string>();

        foreach (var error in errors)
        {
            var targetKey = resolveTargetKey(error);
            if (string.IsNullOrWhiteSpace(targetKey))
            {
                orphans.Add(error.FieldKey);
                continue;
            }

            if (!registered.Contains(targetKey))
            {
                orphans.Add(error.FieldKey);
            }
        }

        return orphans;
    }
}
