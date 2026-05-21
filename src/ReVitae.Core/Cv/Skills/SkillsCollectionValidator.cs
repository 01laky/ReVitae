using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.Skills;

public sealed class SkillsCollectionValidator
{
    private readonly FieldValidator _groupValidator = SkillsSchema.CreateGroupValidator();
    private readonly FieldValidator _skillValidator = SkillsSchema.CreateSkillValidator();

    public FieldValidationResult Validate(IReadOnlyList<SkillsGroupEntry> entries)
    {
        var errors = new List<FieldValidationError>();

        foreach (var entry in entries)
        {
            if (!entry.HasUserInput())
            {
                continue;
            }

            errors.AddRange(ValidateActiveGroup(entry));
        }

        return new FieldValidationResult(errors);
    }

    private IReadOnlyList<FieldValidationError> ValidateActiveGroup(SkillsGroupEntry entry)
    {
        var errors = new List<FieldValidationError>();
        var categoryKey = SkillsFieldKeys.BuildGroup(entry.Id, SkillsFieldKeys.Category);
        var categoryResult = _groupValidator.ValidateField(SkillsFieldKeys.Category, entry.Category);
        foreach (var error in categoryResult.Errors)
        {
            errors.Add(new FieldValidationError(categoryKey, error.Message));
        }

        var activeSkills = entry.Skills.Where(skill => skill.HasUserInput()).ToArray();
        if (activeSkills.Length == 0)
        {
            errors.Add(new FieldValidationError(
                SkillsFieldKeys.BuildGroup(entry.Id, SkillsFieldKeys.SkillsCollection),
                TranslationKeys.ValidationSkillsAtLeastOneRequired));
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in activeSkills)
        {
            var nameKey = SkillsFieldKeys.BuildSkill(entry.Id, skill.Id, SkillsFieldKeys.SkillName);
            var proficiencyKey = SkillsFieldKeys.BuildSkill(entry.Id, skill.Id, SkillsFieldKeys.SkillProficiency);
            var yearsKey = SkillsFieldKeys.BuildSkill(entry.Id, skill.Id, SkillsFieldKeys.SkillYearsOfExperience);

            var nameResult = _skillValidator.ValidateField(SkillsFieldKeys.SkillName, skill.Name);
            foreach (var error in nameResult.Errors)
            {
                errors.Add(new FieldValidationError(nameKey, error.Message));
            }

            var proficiencyResult = _skillValidator.ValidateField(
                SkillsFieldKeys.SkillProficiency,
                skill.Proficiency.ToString());
            foreach (var error in proficiencyResult.Errors)
            {
                errors.Add(new FieldValidationError(proficiencyKey, error.Message));
            }

            if (skill.YearsOfExperience is not null
                && (skill.YearsOfExperience < SkillsSchema.MinYearsOfExperience
                    || skill.YearsOfExperience > SkillsSchema.MaxYearsOfExperience))
            {
                errors.Add(new FieldValidationError(
                    yearsKey,
                    TranslationKeys.ValidationSkillsYearsOfExperienceInvalid));
            }

            var normalizedName = skill.Name.Trim();
            if (normalizedName.Length > 0 && !seenNames.Add(normalizedName))
            {
                errors.Add(new FieldValidationError(
                    nameKey,
                    TranslationKeys.ValidationSkillsDuplicateInGroup));
            }
        }

        return errors;
    }
}
