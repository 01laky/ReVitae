using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.WorkExperience;

public static class WorkExperienceSchema
{
	public const int JobTitleMaxLength = 120;
	public const int CompanyMaxLength = 160;
	public const int LocationMaxLength = 120;
	public const int DescriptionMaxLength = 2000;
	public const int AchievementsMaxLength = 2000;
	public const int TechnologiesMaxLength = 500;
	public const int CompanyUrlMaxLength = 240;

	public static readonly IReadOnlyList<FieldSchema> EntryFields = Array.AsReadOnly(
		new[]
		{
			FieldSchemaFactory.RequiredText(WorkExperienceFieldKeys.JobTitle, "Job title", JobTitleMaxLength, TranslationKeys.ValidationWorkExperienceJobTitleRequired, TranslationKeys.ValidationWorkExperienceJobTitleMax),
			FieldSchemaFactory.RequiredText(WorkExperienceFieldKeys.Company, "Company", CompanyMaxLength, TranslationKeys.ValidationWorkExperienceCompanyRequired, TranslationKeys.ValidationWorkExperienceCompanyMax),
			FieldSchemaFactory.OptionalText(WorkExperienceFieldKeys.Location, "Location", LocationMaxLength, TranslationKeys.ValidationWorkExperienceLocationMax),
			FieldSchemaFactory.RequiredEmploymentType(),
			FieldSchemaFactory.RequiredMonth(WorkExperienceFieldKeys.StartMonth, TranslationKeys.ValidationWorkExperienceStartMonthRequired, TranslationKeys.ValidationWorkExperienceStartMonthInvalid),
			FieldSchemaFactory.RequiredYear(WorkExperienceFieldKeys.StartYear, TranslationKeys.ValidationWorkExperienceStartYearRequired, TranslationKeys.ValidationWorkExperienceStartYearInvalid),
			FieldSchemaFactory.OptionalMonth(WorkExperienceFieldKeys.EndMonth, TranslationKeys.ValidationWorkExperienceEndMonthInvalid),
			FieldSchemaFactory.OptionalYear(WorkExperienceFieldKeys.EndYear, TranslationKeys.ValidationWorkExperienceEndYearInvalid),
			FieldSchemaFactory.OptionalText(WorkExperienceFieldKeys.Description, "Description", DescriptionMaxLength, TranslationKeys.ValidationWorkExperienceDescriptionMax),
			FieldSchemaFactory.OptionalText(WorkExperienceFieldKeys.Achievements, "Achievements", AchievementsMaxLength, TranslationKeys.ValidationWorkExperienceAchievementsMax),
			FieldSchemaFactory.OptionalText(WorkExperienceFieldKeys.Technologies, "Technologies", TechnologiesMaxLength, TranslationKeys.ValidationWorkExperienceTechnologiesMax),
			FieldSchemaFactory.OptionalUrl(WorkExperienceFieldKeys.CompanyUrl, "Company URL", CompanyUrlMaxLength, TranslationKeys.ValidationWorkExperienceCompanyUrlMax, TranslationKeys.ValidationWorkExperienceCompanyUrlFormat)
		});

	public static FieldValidator CreateEntryValidator()
	{
		return new FieldValidator(EntryFields);
	}

	public static IReadOnlyList<FieldSchema> CreateSchemasForEntry(string entryId)
	{
		return EntryFields
			.Select(field => field with { Key = WorkExperienceFieldKeys.Build(entryId, field.Key) })
			.ToArray();
	}
}
