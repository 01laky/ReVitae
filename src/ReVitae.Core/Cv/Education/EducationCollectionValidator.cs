using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.Education;

public sealed class EducationCollectionValidator
{
    private readonly FieldValidator _entryValidator = EducationSchema.CreateEntryValidator();

    public FieldValidationResult Validate(IReadOnlyList<EducationEntry> entries)
    {
        var errors = new List<FieldValidationError>();

        foreach (var entry in entries)
        {
            if (!entry.HasUserInput())
            {
                continue;
            }

            errors.AddRange(ValidateActiveEntry(entry));
        }

        return new FieldValidationResult(errors);
    }

    private IReadOnlyList<FieldValidationError> ValidateActiveEntry(EducationEntry entry)
    {
        var errors = new List<FieldValidationError>();
        var values = entry.ToFieldValues();
        var schemas = EducationSchema.CreateSchemasForEntry(entry.Id);

        foreach (var schema in schemas)
        {
            values.TryGetValue(schema.Key, out var value);

            if (schema.Key.EndsWith("." + EducationFieldKeys.EndMonth, StringComparison.Ordinal)
                || schema.Key.EndsWith("." + EducationFieldKeys.EndYear, StringComparison.Ordinal))
            {
                if (entry.IsCurrentlyStudying)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    errors.Add(new FieldValidationError(
                        schema.Key,
                        schema.Key.EndsWith("." + EducationFieldKeys.EndMonth, StringComparison.Ordinal)
                            ? TranslationKeys.ValidationEducationEndMonthRequired
                            : TranslationKeys.ValidationEducationEndYearRequired));
                    continue;
                }
            }

            var fieldName = schema.Key[(schema.Key.LastIndexOf('.') + 1)..];
            var baseSchema = EducationSchema.EntryFields.First(field => field.Key == fieldName);
            var result = _entryValidator.ValidateField(baseSchema.Key, value);

            foreach (var error in result.Errors)
            {
                errors.Add(new FieldValidationError(schema.Key, error.Message));
            }
        }

        if (MonthYearValue.TryParse(entry.StartMonth, entry.StartYear, out var startDate)
            && MonthYearValue.TryParse(entry.EndMonth, entry.EndYear, out var endDate)
            && !entry.IsCurrentlyStudying
            && startDate!.CompareTo(endDate) > 0)
        {
            errors.Add(new FieldValidationError(
                EducationFieldKeys.Build(entry.Id, EducationFieldKeys.DateRange),
                TranslationKeys.ValidationEducationStartAfterEnd));
        }

        return errors;
    }
}
