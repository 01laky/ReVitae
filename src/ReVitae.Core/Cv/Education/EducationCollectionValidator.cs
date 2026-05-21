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

            if (CollectionEntryValidationHelper.IsEndDateField(
                    schema.Key,
                    EducationFieldKeys.EndMonth,
                    EducationFieldKeys.EndYear))
            {
                if (entry.IsCurrentlyStudying)
                {
                    continue;
                }

                var endDateError = CollectionEntryValidationHelper.ValidateRequiredEndDateWhenInactive(
                    schema.Key,
                    value,
                    EducationFieldKeys.EndMonth,
                    TranslationKeys.ValidationEducationEndMonthRequired,
                    TranslationKeys.ValidationEducationEndYearRequired);

                if (endDateError is not null)
                {
                    errors.Add(endDateError);
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

        CollectionEntryValidationHelper.ValidateStartBeforeEnd(
            errors,
            EducationFieldKeys.Build(entry.Id, EducationFieldKeys.DateRange),
            TranslationKeys.ValidationEducationStartAfterEnd,
            entry.StartMonth,
            entry.StartYear,
            entry.EndMonth,
            entry.EndYear,
            entry.IsCurrentlyStudying);

        return errors;
    }
}
