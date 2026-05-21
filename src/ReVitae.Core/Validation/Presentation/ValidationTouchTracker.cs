namespace ReVitae.Core.Validation.Presentation;

public sealed class ValidationTouchTracker
{
    private readonly HashSet<string> _touchedKeys = new(StringComparer.Ordinal);

    public bool HasAttemptedExportWithInvalidForm { get; private set; }

    public void MarkTouched(string fieldKey)
    {
        if (!string.IsNullOrWhiteSpace(fieldKey))
        {
            _touchedKeys.Add(fieldKey);
        }
    }

    public void MarkManyTouched(IEnumerable<string> fieldKeys)
    {
        foreach (var fieldKey in fieldKeys)
        {
            MarkTouched(fieldKey);
        }
    }

    public bool IsTouched(string fieldKey) =>
        !string.IsNullOrWhiteSpace(fieldKey) && _touchedKeys.Contains(fieldKey);

    public void MarkExportAttemptWithInvalidForm(IEnumerable<string> invalidFieldKeys)
    {
        HasAttemptedExportWithInvalidForm = true;
        MarkManyTouched(invalidFieldKeys);
    }

    public bool ShouldDisplayErrors(string fieldKey, bool hasErrors) => hasErrors;

    public void Reset()
    {
        _touchedKeys.Clear();
        HasAttemptedExportWithInvalidForm = false;
    }

    public void ClearField(string fieldKey) => _touchedKeys.Remove(fieldKey);
}
