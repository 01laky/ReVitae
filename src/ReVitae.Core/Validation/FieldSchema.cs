namespace ReVitae.Core.Validation;

public sealed record FieldSchema(
    string Key,
    string Label,
    bool IsRequired,
    int MaximumLength,
    FieldFormat Format,
    string RequiredMessage,
    string MaximumLengthMessage,
    string? FormatMessage = null);
