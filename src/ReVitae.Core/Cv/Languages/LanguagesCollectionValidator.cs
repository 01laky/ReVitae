using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.Languages;

public sealed class LanguagesCollectionValidator
{
    private readonly FieldValidator _entryValidator = LanguagesSchema.CreateEntryValidator();

    public FieldValidationResult Validate(IReadOnlyList<LanguageEntry> entries)
    {
        var errors = new List<FieldValidationError>();
        var seenLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (!entry.HasUserInput())
            {
                continue;
            }

            errors.AddRange(ValidateActiveEntry(entry));

            var normalizedLanguage = entry.Language.Trim();
            if (normalizedLanguage.Length > 0 && !seenLanguages.Add(normalizedLanguage))
            {
                errors.Add(new FieldValidationError(
                    LanguagesFieldKeys.Build(entry.Id, LanguagesFieldKeys.Language),
                    TranslationKeys.ValidationLanguagesDuplicateLanguage));
            }
        }

        return new FieldValidationResult(errors);
    }

    private IReadOnlyList<FieldValidationError> ValidateActiveEntry(LanguageEntry entry)
    {
        var errors = new List<FieldValidationError>();
        var values = entry.ToFieldValues();
        var schemas = LanguagesSchema.CreateSchemasForEntry(entry.Id);

        foreach (var schema in schemas)
        {
            values.TryGetValue(schema.Key, out var value);
            var fieldName = schema.Key[(schema.Key.LastIndexOf('.') + 1)..];
            var baseSchema = LanguagesSchema.EntryFields.First(field => field.Key == fieldName);
            var result = _entryValidator.ValidateField(baseSchema.Key, value);

            foreach (var error in result.Errors)
            {
                errors.Add(new FieldValidationError(schema.Key, error.Message));
            }
        }

        return errors;
    }
}
