using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.Education;

public static class EducationSchema
{
	public const int InstitutionMaxLength = 160;
	public const int DegreeMaxLength = 160;
	public const int FieldOfStudyMaxLength = 160;
	public const int LocationMaxLength = 120;
	public const int GradeMaxLength = 80;
	public const int DescriptionMaxLength = 2000;
	public const int InstitutionUrlMaxLength = 240;

	public static readonly IReadOnlyList<FieldSchema> EntryFields = Array.AsReadOnly(
		new[]
		{
			FieldSchemaFactory.RequiredText(EducationFieldKeys.Institution, "Institution", InstitutionMaxLength, TranslationKeys.ValidationEducationInstitutionRequired, TranslationKeys.ValidationEducationInstitutionMax),
			FieldSchemaFactory.RequiredText(EducationFieldKeys.Degree, "Degree", DegreeMaxLength, TranslationKeys.ValidationEducationDegreeRequired, TranslationKeys.ValidationEducationDegreeMax),
			FieldSchemaFactory.OptionalText(EducationFieldKeys.FieldOfStudy, "Field of study", FieldOfStudyMaxLength, TranslationKeys.ValidationEducationFieldOfStudyMax),
			FieldSchemaFactory.OptionalText(EducationFieldKeys.Location, "Location", LocationMaxLength, TranslationKeys.ValidationEducationLocationMax),
			FieldSchemaFactory.RequiredDegreeType(),
			FieldSchemaFactory.RequiredMonth(EducationFieldKeys.StartMonth, TranslationKeys.ValidationEducationStartMonthRequired, TranslationKeys.ValidationEducationStartMonthInvalid),
			FieldSchemaFactory.RequiredYear(EducationFieldKeys.StartYear, TranslationKeys.ValidationEducationStartYearRequired, TranslationKeys.ValidationEducationStartYearInvalid),
			FieldSchemaFactory.OptionalMonth(EducationFieldKeys.EndMonth, TranslationKeys.ValidationEducationEndMonthInvalid),
			FieldSchemaFactory.OptionalYear(EducationFieldKeys.EndYear, TranslationKeys.ValidationEducationEndYearInvalid),
			FieldSchemaFactory.OptionalText(EducationFieldKeys.Grade, "Grade", GradeMaxLength, TranslationKeys.ValidationEducationGradeMax),
			FieldSchemaFactory.OptionalText(EducationFieldKeys.Description, "Description", DescriptionMaxLength, TranslationKeys.ValidationEducationDescriptionMax),
			FieldSchemaFactory.OptionalUrl(EducationFieldKeys.InstitutionUrl, "Institution URL", InstitutionUrlMaxLength, TranslationKeys.ValidationEducationInstitutionUrlMax, TranslationKeys.ValidationEducationInstitutionUrlFormat)
		});

	public static FieldValidator CreateEntryValidator()
	{
		return new FieldValidator(EntryFields);
	}

	public static IReadOnlyList<FieldSchema> CreateSchemasForEntry(string entryId)
	{
		return EntryFields
			.Select(field => field with { Key = EducationFieldKeys.Build(entryId, field.Key) })
			.ToArray();
	}
}
