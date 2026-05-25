using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.Links;

public sealed class LinksCollectionValidator
{
	private readonly FieldValidator _entryValidator = LinksSchema.CreateEntryValidator();

	public FieldValidationResult Validate(IReadOnlyList<LinkEntry> entries)
	{
		var errors = new List<FieldValidationError>();
		var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var entry in entries)
		{
			if (!entry.HasUserInput())
			{
				continue;
			}

			errors.AddRange(ValidateActiveEntry(entry));

			var normalizedUrl = entry.Url.Trim();
			if (normalizedUrl.Length > 0 && !seenUrls.Add(normalizedUrl))
			{
				errors.Add(new FieldValidationError(
					LinksFieldKeys.Build(entry.Id, LinksFieldKeys.Url),
					TranslationKeys.ValidationLinksDuplicateUrl));
			}
		}

		return new FieldValidationResult(errors);
	}

	private IReadOnlyList<FieldValidationError> ValidateActiveEntry(LinkEntry entry)
	{
		var errors = new List<FieldValidationError>();
		var values = entry.ToFieldValues();
		var schemas = LinksSchema.CreateSchemasForEntry(entry.Id);

		foreach (var schema in schemas)
		{
			values.TryGetValue(schema.Key, out var value);
			var fieldName = schema.Key[(schema.Key.LastIndexOf('.') + 1)..];
			var baseSchema = LinksSchema.EntryFields.First(field => field.Key == fieldName);
			var result = _entryValidator.ValidateField(baseSchema.Key, value);

			foreach (var error in result.Errors)
			{
				errors.Add(new FieldValidationError(schema.Key, error.Message));
			}
		}

		return errors;
	}
}
