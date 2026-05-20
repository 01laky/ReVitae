namespace ReVitae.Core.Validation;

public sealed class FieldValidationResult
{
    public FieldValidationResult(IReadOnlyList<FieldValidationError> errors)
    {
        Errors = errors;
    }

    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<FieldValidationError> Errors { get; }
}
