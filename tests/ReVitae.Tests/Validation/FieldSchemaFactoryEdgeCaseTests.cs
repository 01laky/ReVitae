using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Tests.Validation;

public sealed class FieldSchemaFactoryEdgeCaseTests
{
	[Fact]
	public void RequiredEmail_HasEmailFormatAndRequiredMessages()
	{
		var schema = FieldSchemaFactory.RequiredEmail(
			"email",
			"Email",
			120,
			TranslationKeys.ValidationEmailRequired,
			TranslationKeys.ValidationEmailMax,
			TranslationKeys.ValidationEmailFormat);

		Assert.True(schema.IsRequired);
		Assert.Equal(FieldFormat.Email, schema.Format);
		Assert.Equal(TranslationKeys.ValidationEmailFormat, schema.FormatMessage);
	}

	[Fact]
	public void OptionalUrl_IsNotRequired()
	{
		var schema = FieldSchemaFactory.OptionalUrl(
			"url",
			"URL",
			200,
			TranslationKeys.ValidationLinkedInUrlMax,
			TranslationKeys.ValidationLinkedInUrlFormat);

		Assert.False(schema.IsRequired);
		Assert.Equal(FieldFormat.Url, schema.Format);
	}

	[Fact]
	public void RequiredMonthYearSchemas_UseMonthAndYearFormats()
	{
		var month = FieldSchemaFactory.RequiredMonth(
			"startMonth",
			TranslationKeys.ValidationWorkExperienceStartMonthRequired,
			TranslationKeys.ValidationWorkExperienceStartMonthInvalid);
		var year = FieldSchemaFactory.RequiredYear(
			"startYear",
			TranslationKeys.ValidationWorkExperienceStartYearRequired,
			TranslationKeys.ValidationWorkExperienceStartYearInvalid);

		Assert.Equal(FieldFormat.Month, month.Format);
		Assert.Equal(FieldFormat.Year, year.Format);
		Assert.Equal(2, month.MaximumLength);
		Assert.Equal(4, year.MaximumLength);
	}

	[Fact]
	public void RequiredText_EnforcesMaximumLengthMessage()
	{
		var schema = FieldSchemaFactory.RequiredText(
			"title",
			"Title",
			64,
			TranslationKeys.ValidationEmailRequired,
			TranslationKeys.ValidationProfessionalTitleMax);

		Assert.Equal(64, schema.MaximumLength);
		Assert.Equal(TranslationKeys.ValidationProfessionalTitleMax, schema.MaximumLengthMessage);
	}

	[Fact]
	public void RequiredEmploymentType_UsesWorkExperienceKeys()
	{
		var schema = FieldSchemaFactory.RequiredEmploymentType();

		Assert.Equal(WorkExperienceFieldKeys.EmploymentType, schema.Key);
		Assert.Equal(FieldFormat.EmploymentType, schema.Format);
	}

	[Fact]
	public void RequiredDegreeType_UsesEducationKeys()
	{
		var schema = FieldSchemaFactory.RequiredDegreeType();

		Assert.Equal(EducationFieldKeys.DegreeType, schema.Key);
		Assert.Equal(FieldFormat.DegreeType, schema.Format);
	}

	[Fact]
	public void OptionalCefrLevel_AllowsEmptyValue()
	{
		var schema = FieldSchemaFactory.OptionalCefrLevel(
			"cefr",
			TranslationKeys.ValidationLanguagesCefrInvalid);

		Assert.False(schema.IsRequired);
		Assert.Equal(FieldFormat.CefrLevel, schema.Format);
	}

	[Fact]
	public void RequiredProficiencyLevel_IsRequiredWithProficiencyFormat()
	{
		var schema = FieldSchemaFactory.RequiredProficiencyLevel(
			"proficiency",
			TranslationKeys.ValidationSkillsProficiencyRequired,
			TranslationKeys.ValidationSkillsProficiencyInvalid);

		Assert.True(schema.IsRequired);
		Assert.Equal(FieldFormat.ProficiencyLevel, schema.Format);
	}
}
