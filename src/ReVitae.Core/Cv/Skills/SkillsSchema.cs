using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.Skills;

public static class SkillsSchema
{
	public const int CategoryMaxLength = 120;
	public const int SkillNameMaxLength = 80;
	public const int BulkSkillsMaxLength = 1000;
	public const int MinYearsOfExperience = 0;
	public const int MaxYearsOfExperience = 60;

	public static readonly FieldSchema CategoryField = new(
		SkillsFieldKeys.Category,
		"Category",
		IsRequired: true,
		CategoryMaxLength,
		FieldFormat.Text,
		RequiredMessage: TranslationKeys.ValidationSkillsCategoryRequired,
		MaximumLengthMessage: TranslationKeys.ValidationSkillsCategoryMax);

	public static readonly FieldSchema SkillNameField = new(
		SkillsFieldKeys.SkillName,
		"Skill name",
		IsRequired: true,
		SkillNameMaxLength,
		FieldFormat.Text,
		RequiredMessage: TranslationKeys.ValidationSkillsSkillNameRequired,
		MaximumLengthMessage: TranslationKeys.ValidationSkillsSkillNameMax);

	public static readonly FieldSchema SkillProficiencyField = new(
		SkillsFieldKeys.SkillProficiency,
		"Proficiency",
		IsRequired: true,
		MaximumLength: 32,
		Format: FieldFormat.ProficiencyLevel,
		RequiredMessage: TranslationKeys.ValidationSkillsProficiencyRequired,
		MaximumLengthMessage: TranslationKeys.ValidationSkillsProficiencyInvalid,
		FormatMessage: TranslationKeys.ValidationSkillsProficiencyInvalid);

	public static FieldValidator CreateGroupValidator()
	{
		return new FieldValidator([CategoryField]);
	}

	public static FieldValidator CreateSkillValidator()
	{
		return new FieldValidator([SkillNameField, SkillProficiencyField]);
	}
}
