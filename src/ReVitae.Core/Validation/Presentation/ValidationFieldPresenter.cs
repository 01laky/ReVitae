namespace ReVitae.Core.Validation.Presentation;

public static class ValidationFieldPresenter
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildMessageMap(
        IReadOnlyList<FieldValidationError> errors,
        Func<string, string> localizeMessage,
        Func<FieldValidationError, string>? resolveTargetKey = null)
    {
        resolveTargetKey ??= error => error.FieldKey;
        var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var error in errors)
        {
            var targetKey = resolveTargetKey(error);
            if (string.IsNullOrWhiteSpace(targetKey))
            {
                continue;
            }

            var message = localizeMessage(error.Message);
            if (string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            if (!map.TryGetValue(targetKey, out var messages))
            {
                messages = [];
                map[targetKey] = messages;
            }

            if (!messages.Contains(message, StringComparer.Ordinal))
            {
                messages.Add(message);
            }
        }

        return map.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value.ToArray(),
            StringComparer.Ordinal);
    }

    public static IReadOnlyList<string> GetMessagesForSuffix(
        IReadOnlyList<FieldValidationError> errors,
        string fieldSuffix,
        Func<string, string> localizeMessage)
    {
        return errors
            .Where(error => error.FieldKey.EndsWith("." + fieldSuffix, StringComparison.Ordinal)
                || string.Equals(error.FieldKey, fieldSuffix, StringComparison.Ordinal))
            .Select(error => localizeMessage(error.Message))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<string> GetMessagesForExactKey(
        IReadOnlyList<FieldValidationError> errors,
        string fieldKey,
        Func<string, string> localizeMessage)
    {
        return errors
            .Where(error => string.Equals(error.FieldKey, fieldKey, StringComparison.Ordinal))
            .Select(error => localizeMessage(error.Message))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
