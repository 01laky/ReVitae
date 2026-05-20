using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.WorkExperience;

public sealed class WorkExperienceCollectionValidator
{
    private readonly FieldValidator _entryValidator = WorkExperienceSchema.CreateEntryValidator();

    public FieldValidationResult Validate(IReadOnlyList<WorkExperienceEntry> entries)
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

    private IReadOnlyList<FieldValidationError> ValidateActiveEntry(WorkExperienceEntry entry)
    {
        var errors = new List<FieldValidationError>();
        var values = entry.ToFieldValues();
        var schemas = WorkExperienceSchema.CreateSchemasForEntry(entry.Id);

        foreach (var schema in schemas)
        {
            values.TryGetValue(schema.Key, out var value);

            if (schema.Key.EndsWith("." + WorkExperienceFieldKeys.EndMonth, StringComparison.Ordinal)
                || schema.Key.EndsWith("." + WorkExperienceFieldKeys.EndYear, StringComparison.Ordinal))
            {
                if (entry.IsCurrentlyWorking)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    errors.Add(new FieldValidationError(
                        schema.Key,
                        schema.Key.EndsWith("." + WorkExperienceFieldKeys.EndMonth, StringComparison.Ordinal)
                            ? TranslationKeys.ValidationWorkExperienceEndMonthRequired
                            : TranslationKeys.ValidationWorkExperienceEndYearRequired));
                    continue;
                }
            }

            var fieldName = schema.Key[(schema.Key.LastIndexOf('.') + 1)..];
            var baseSchema = WorkExperienceSchema.EntryFields.First(field => field.Key == fieldName);
            var result = _entryValidator.ValidateField(baseSchema.Key, value);

            foreach (var error in result.Errors)
            {
                errors.Add(new FieldValidationError(schema.Key, error.Message));
            }
        }

        if (MonthYearValue.TryParse(entry.StartMonth, entry.StartYear, out var startDate)
            && MonthYearValue.TryParse(entry.EndMonth, entry.EndYear, out var endDate)
            && !entry.IsCurrentlyWorking
            && startDate!.CompareTo(endDate) > 0)
        {
            errors.Add(new FieldValidationError(
                WorkExperienceFieldKeys.Build(entry.Id, WorkExperienceFieldKeys.DateRange),
                TranslationKeys.ValidationWorkExperienceStartAfterEnd));
        }

        return errors;
    }
}
