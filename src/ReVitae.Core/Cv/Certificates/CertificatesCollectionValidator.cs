using ReVitae.Core.Cv;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.Certificates;

public sealed class CertificatesCollectionValidator
{
	private readonly FieldValidator _entryValidator = CertificatesSchema.CreateEntryValidator();

	public FieldValidationResult Validate(IReadOnlyList<CertificateEntry> entries)
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

	private IReadOnlyList<FieldValidationError> ValidateActiveEntry(CertificateEntry entry)
	{
		var errors = new List<FieldValidationError>();
		var values = entry.ToFieldValues();
		var schemas = CertificatesSchema.CreateSchemasForEntry(entry.Id);

		foreach (var schema in schemas)
		{
			values.TryGetValue(schema.Key, out var value);
			var fieldName = schema.Key[(schema.Key.LastIndexOf('.') + 1)..];
			var baseSchema = CertificatesSchema.EntryFields.First(field => field.Key == fieldName);
			var result = _entryValidator.ValidateField(baseSchema.Key, value);

			foreach (var error in result.Errors)
			{
				errors.Add(new FieldValidationError(schema.Key, error.Message));
			}
		}

		if (MonthYearValue.TryParse(entry.IssueMonth, entry.IssueYear, out var issueDate)
			&& MonthYearValue.TryParse(entry.ExpirationMonth, entry.ExpirationYear, out var expirationDate)
			&& issueDate!.CompareTo(expirationDate) > 0)
		{
			errors.Add(new FieldValidationError(
				CertificatesFieldKeys.Build(entry.Id, CertificatesFieldKeys.DateRange),
				TranslationKeys.ValidationCertificatesIssueAfterExpiration));
		}

		return errors;
	}
}
