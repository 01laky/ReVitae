using ReVitae.Core.Cv;
using ReVitae.Core.Cv.AdditionalInformation;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Validation;
using ReVitae.Core.Validation.Presentation;

namespace ReVitae.Tests.Ui.Validation;

public sealed class ValidationNavigationEdgeCaseTests
{
	private static readonly string[] FullFormOrder =
	[
		MainPersonalInformationFieldKeys.FirstName,
		MainPersonalInformationFieldKeys.LastName,
		MainPersonalInformationFieldKeys.Email,
		WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.JobTitle),
		WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.Company),
		WorkExperienceFieldKeys.Build("work-2", WorkExperienceFieldKeys.JobTitle),
		"education.edu-1.institution",
		"skills.group-1.category",
		"languages.lang-1.language",
		"certificates.cert-1.name",
		"projects.proj-1.name",
		"links.link-1.url",
		AdditionalInformationFieldKeys.Content
	];

	[Fact]
	public void GetFirstInvalidFieldKey_NoInvalidFields_ReturnsNull()
	{
		var first = ValidationNavigationPlanner.GetFirstInvalidFieldKey(FullFormOrder, []);

		Assert.Null(first);
	}

	[Fact]
	public void GetFirstInvalidFieldKey_FirstErrorInPersonalInfo_WhenLaterSectionsInvalidToo()
	{
		var invalidKeys = new HashSet<string>(StringComparer.Ordinal)
		{
			MainPersonalInformationFieldKeys.Email,
			WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.Company),
			"projects.proj-1.name"
		};

		var first = ValidationNavigationPlanner.GetFirstInvalidFieldKey(FullFormOrder, invalidKeys);

		Assert.Equal(MainPersonalInformationFieldKeys.Email, first);
	}

	[Fact]
	public void GetFirstInvalidFieldKey_FirstErrorInLaterSection_WhenPersonalInfoValid()
	{
		var invalidKeys = new HashSet<string>(StringComparer.Ordinal)
		{
			WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.Company),
			"projects.proj-1.name"
		};

		var first = ValidationNavigationPlanner.GetFirstInvalidFieldKey(FullFormOrder, invalidKeys);

		Assert.Equal(WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.Company), first);
	}

	[Fact]
	public void GetFirstInvalidFieldKey_SecondEntryCard_WinsWithinRepeatableSection()
	{
		var orderedKeys = new[]
		{
			WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.JobTitle),
			WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.Company),
			WorkExperienceFieldKeys.Build("work-2", WorkExperienceFieldKeys.JobTitle),
			WorkExperienceFieldKeys.Build("work-2", WorkExperienceFieldKeys.Company)
		};

		var invalidKeys = new HashSet<string>(StringComparer.Ordinal)
		{
			WorkExperienceFieldKeys.Build("work-2", WorkExperienceFieldKeys.JobTitle)
		};

		var first = ValidationNavigationPlanner.GetFirstInvalidFieldKey(orderedKeys, invalidKeys);

		Assert.Equal(WorkExperienceFieldKeys.Build("work-2", WorkExperienceFieldKeys.JobTitle), first);
	}

	[Fact]
	public void GetFirstInvalidFieldKey_CollapsedSectionField_ResolvesToFieldKeyBeforeExpand()
	{
		const string collapsedField = "education.edu-1.institution";
		var invalidKeys = new HashSet<string>(StringComparer.Ordinal) { collapsedField };

		var first = ValidationNavigationPlanner.GetFirstInvalidFieldKey(FullFormOrder, invalidKeys);

		Assert.Equal(collapsedField, first);
	}

	[Fact]
	public void GetFirstInvalidFieldKey_DeterministicOrderingAcrossFullFormOrder()
	{
		var invalidKeys = new HashSet<string>(StringComparer.Ordinal)
		{
			"projects.proj-1.name",
			MainPersonalInformationFieldKeys.LastName,
			WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.JobTitle),
			"links.link-1.url"
		};

		var first = ValidationNavigationPlanner.GetFirstInvalidFieldKey(FullFormOrder, invalidKeys);

		Assert.Equal(MainPersonalInformationFieldKeys.LastName, first);
	}

	[Fact]
	public void GetFirstInvalidFieldKey_MultipleErrorsSameSection_FirstInVisualOrderWins()
	{
		var sectionOrder = new[]
		{
			WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.JobTitle),
			WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.Company),
			WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.Location)
		};

		var invalidKeys = new HashSet<string>(StringComparer.Ordinal)
		{
			WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.Location),
			WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.Company)
		};

		var first = ValidationNavigationPlanner.GetFirstInvalidFieldKey(sectionOrder, invalidKeys);

		Assert.Equal(WorkExperienceFieldKeys.Build("work-1", WorkExperienceFieldKeys.Company), first);
	}

	[Fact]
	public void GetFirstInvalidFieldKey_InvalidKeyNotInOrder_FallsBackToFirstInvalidKey()
	{
		var invalidKeys = new[] { "orphan.field", "another.orphan" };

		var first = ValidationNavigationPlanner.GetFirstInvalidFieldKey(FullFormOrder, invalidKeys);

		Assert.Equal("orphan.field", first);
	}

	[Fact]
	public void CollectInvalidKeys_EmptyErrors_ReturnsEmptyList()
	{
		var keys = ValidationNavigationPlanner.CollectInvalidKeys([]);

		Assert.Empty(keys);
	}

	[Fact]
	public void CollectInvalidKeys_DeduplicatesRepeatedFieldKeys()
	{
		var errors = new[]
		{
			new FieldValidationError("email", "validation.email.required"),
			new FieldValidationError("email", "validation.email.format"),
			new FieldValidationError("firstName", "validation.firstName.required")
		};

		var keys = ValidationNavigationPlanner.CollectInvalidKeys(errors);

		Assert.Equal(["email", "firstName"], keys);
	}

	[Fact]
	public void CollectInvalidKeys_PreservesFirstOccurrenceOrder()
	{
		var errors = new[]
		{
			new FieldValidationError("lastName", "a"),
			new FieldValidationError("firstName", "b"),
			new FieldValidationError("email", "c"),
			new FieldValidationError("firstName", "d")
		};

		var keys = ValidationNavigationPlanner.CollectInvalidKeys(errors);

		Assert.Equal(["lastName", "firstName", "email"], keys);
	}

	[Theory]
	[InlineData("Email")]
	[InlineData("email")]
	public void GetFirstInvalidFieldKey_UsesOrdinalCaseSensitiveMatching(string invalidKey)
	{
		var orderedKeys = new[] { "email", "firstName" };
		var invalidKeys = new HashSet<string>(StringComparer.Ordinal) { invalidKey };

		var first = ValidationNavigationPlanner.GetFirstInvalidFieldKey(orderedKeys, invalidKeys);

		Assert.Equal(invalidKey, first);
	}
}
