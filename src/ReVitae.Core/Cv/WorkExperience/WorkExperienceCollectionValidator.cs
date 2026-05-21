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

            if (CollectionEntryValidationHelper.IsEndDateField(
                    schema.Key,
                    WorkExperienceFieldKeys.EndMonth,
                    WorkExperienceFieldKeys.EndYear))
            {
                if (entry.IsCurrentlyWorking)
                {
                    continue;
                }

                var endDateError = CollectionEntryValidationHelper.ValidateRequiredEndDateWhenInactive(
                    schema.Key,
                    value,
                    WorkExperienceFieldKeys.EndMonth,
                    TranslationKeys.ValidationWorkExperienceEndMonthRequired,
                    TranslationKeys.ValidationWorkExperienceEndYearRequired);

                if (endDateError is not null)
                {
                    errors.Add(endDateError);
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

        CollectionEntryValidationHelper.ValidateStartBeforeEnd(
            errors,
            WorkExperienceFieldKeys.Build(entry.Id, WorkExperienceFieldKeys.DateRange),
            TranslationKeys.ValidationWorkExperienceStartAfterEnd,
            entry.StartMonth,
            entry.StartYear,
            entry.EndMonth,
            entry.EndYear,
            entry.IsCurrentlyWorking);

        return errors;
    }
}
