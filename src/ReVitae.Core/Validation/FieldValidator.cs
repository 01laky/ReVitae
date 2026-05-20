using System.Net.Mail;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.WorkExperience;

namespace ReVitae.Core.Validation;

public sealed class FieldValidator
{
    private readonly IReadOnlyDictionary<string, FieldSchema> _schemaByKey;

    public FieldValidator(IEnumerable<FieldSchema> schemas)
    {
        _schemaByKey = schemas.ToDictionary(schema => schema.Key, StringComparer.Ordinal);
    }

    public FieldValidationResult ValidateField(string fieldKey, string? value)
    {
        if (!_schemaByKey.TryGetValue(fieldKey, out var schema))
        {
            throw new ArgumentException($"Unknown field key '{fieldKey}'.", nameof(fieldKey));
        }

        var errors = ValidateValue(schema, value);
        return new FieldValidationResult(errors);
    }

    public FieldValidationResult Validate(IReadOnlyDictionary<string, string?> values)
    {
        var errors = new List<FieldValidationError>();

        foreach (var schema in _schemaByKey.Values)
        {
            values.TryGetValue(schema.Key, out var value);
            errors.AddRange(ValidateValue(schema, value));
        }

        return new FieldValidationResult(errors);
    }

    private static IReadOnlyList<FieldValidationError> ValidateValue(FieldSchema schema, string? value)
    {
        var normalizedValue = value?.Trim() ?? string.Empty;
        var errors = new List<FieldValidationError>();

        if (schema.IsRequired && normalizedValue.Length == 0)
        {
            errors.Add(new FieldValidationError(schema.Key, schema.RequiredMessage));
            return errors;
        }

        if (normalizedValue.Length == 0)
        {
            return errors;
        }

        if (normalizedValue.Length > schema.MaximumLength)
        {
            errors.Add(new FieldValidationError(schema.Key, schema.MaximumLengthMessage));
        }

        if (!IsFormatValid(schema.Format, normalizedValue) && schema.FormatMessage is not null)
        {
            errors.Add(new FieldValidationError(schema.Key, schema.FormatMessage));
        }

        return errors;
    }

    private static bool IsFormatValid(FieldFormat format, string value)
    {
        return format switch
        {
            FieldFormat.Text => true,
            FieldFormat.Email => IsEmailValid(value),
            FieldFormat.Url => IsUrlValid(value),
            FieldFormat.Month => IsMonthValid(value),
            FieldFormat.Year => IsYearValid(value),
            FieldFormat.EmploymentType => IsEmploymentTypeValid(value),
            FieldFormat.DegreeType => IsDegreeTypeValid(value),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    private static bool IsMonthValid(string value)
    {
        return int.TryParse(value, out var month) && MonthYearValue.IsValidMonth(month);
    }

    private static bool IsYearValid(string value)
    {
        return int.TryParse(value, out var year) && MonthYearValue.IsValidYear(year);
    }

    private static bool IsEmploymentTypeValid(string value)
    {
        return Enum.TryParse<EmploymentType>(value, ignoreCase: false, out _);
    }

    private static bool IsDegreeTypeValid(string value)
    {
        return Enum.TryParse<DegreeType>(value, ignoreCase: false, out _);
    }

    private static bool IsEmailValid(string value)
    {
        if (value.Count(character => character == '@') != 1)
        {
            return false;
        }

        try
        {
            var address = new MailAddress(value);
            return string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase)
                && address.Host.Contains('.', StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsUrlValid(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrWhiteSpace(uri.Host)
            && uri.Host.Contains('.', StringComparison.Ordinal);
    }
}
