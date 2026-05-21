using ReVitae.Core.Cv;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.Projects;

public sealed class ProjectsCollectionValidator
{
    private readonly FieldValidator _entryValidator = ProjectsSchema.CreateEntryValidator();
    private readonly FieldValidator _technologyValidator = ProjectsSchema.CreateTechnologyValidator();

    public FieldValidationResult Validate(IReadOnlyList<ProjectEntry> entries)
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

    private IReadOnlyList<FieldValidationError> ValidateActiveEntry(ProjectEntry entry)
    {
        var errors = new List<FieldValidationError>();
        var values = entry.ToFieldValues();
        var schemas = ProjectsSchema.CreateSchemasForEntry(entry.Id);

        foreach (var schema in schemas)
        {
            values.TryGetValue(schema.Key, out var value);
            var fieldName = schema.Key[(schema.Key.LastIndexOf('.') + 1)..];
            var baseSchema = ProjectsSchema.EntryFields.First(field => field.Key == fieldName);

            if ((fieldName == ProjectsFieldKeys.EndMonth || fieldName == ProjectsFieldKeys.EndYear)
                && entry.IsCurrentlyActive)
            {
                continue;
            }

            var result = _entryValidator.ValidateField(baseSchema.Key, value);
            foreach (var error in result.Errors)
            {
                errors.Add(new FieldValidationError(schema.Key, error.Message));
            }
        }

        var activeTechnologies = entry.Technologies.Where(technology => technology.HasUserInput()).ToArray();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var technology in activeTechnologies)
        {
            var nameKey = ProjectsFieldKeys.BuildTechnology(
                entry.Id,
                technology.Id,
                ProjectsFieldKeys.TechnologyName);
            var nameResult = _technologyValidator.ValidateField(
                ProjectsFieldKeys.TechnologyName,
                technology.Name);

            foreach (var error in nameResult.Errors)
            {
                errors.Add(new FieldValidationError(nameKey, error.Message));
            }

            var normalizedName = technology.Name.Trim();
            if (normalizedName.Length > 0 && !seenNames.Add(normalizedName))
            {
                errors.Add(new FieldValidationError(
                    nameKey,
                    TranslationKeys.ValidationProjectsDuplicateTechnology));
            }
        }

        if (MonthYearValue.TryParse(entry.StartMonth, entry.StartYear, out var startDate)
            && MonthYearValue.TryParse(entry.EndMonth, entry.EndYear, out var endDate)
            && !entry.IsCurrentlyActive
            && startDate!.CompareTo(endDate) > 0)
        {
            errors.Add(new FieldValidationError(
                ProjectsFieldKeys.Build(entry.Id, ProjectsFieldKeys.DateRange),
                TranslationKeys.ValidationProjectsStartAfterEnd));
        }

        return errors;
    }
}
