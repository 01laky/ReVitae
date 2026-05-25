using ReVitae.Core.Cv;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Core.Validation.Presentation;
using ReVitae.Ui.Validation;

namespace ReVitae.Tests.Ui.Validation;

public sealed class FormValidationServiceEdgeCaseTests
{
	[Fact]
	public void ApplyExportFailure_MarksInvalidKeysAsTouched()
	{
		var touchTracker = new ValidationTouchTracker();
		var errors = new[]
		{
			new FieldValidationError(MainPersonalInformationFieldKeys.FirstName, TranslationKeys.ValidationFirstNameRequired),
			new FieldValidationError(MainPersonalInformationFieldKeys.Email, TranslationKeys.ValidationEmailFormat),
		};

		FormValidationService.ApplyExportFailure(new FieldValidationResult(errors), touchTracker);

		Assert.True(touchTracker.IsTouched(MainPersonalInformationFieldKeys.FirstName));
		Assert.True(touchTracker.IsTouched(MainPersonalInformationFieldKeys.Email));
	}

	[Fact]
	public void GetFirstInvalidFieldKey_ReturnsFirstOrderedInvalidKey()
	{
		var orderedKeys = new[]
		{
			MainPersonalInformationFieldKeys.LastName,
			MainPersonalInformationFieldKeys.FirstName,
			MainPersonalInformationFieldKeys.Email,
		};
		var errors = new[]
		{
			new FieldValidationError(MainPersonalInformationFieldKeys.Email, TranslationKeys.ValidationEmailFormat),
			new FieldValidationError(MainPersonalInformationFieldKeys.FirstName, TranslationKeys.ValidationFirstNameRequired),
		};

		var first = FormValidationService.GetFirstInvalidFieldKey(orderedKeys, new FieldValidationResult(errors));

		Assert.Equal(MainPersonalInformationFieldKeys.FirstName, first);
	}

	[Fact]
	public void GetFirstInvalidFieldKey_FallsBackToFirstInvalidWhenOrderedMisses()
	{
		var orderedKeys = new[] { MainPersonalInformationFieldKeys.Phone };
		var errors = new[]
		{
			new FieldValidationError(MainPersonalInformationFieldKeys.Email, TranslationKeys.ValidationEmailFormat),
		};

		var first = FormValidationService.GetFirstInvalidFieldKey(orderedKeys, new FieldValidationResult(errors));

		Assert.Equal(MainPersonalInformationFieldKeys.Email, first);
	}

	[Fact]
	public void GetFirstInvalidFieldKey_SkipsValidOrderedKeys()
	{
		var orderedKeys = new[]
		{
			MainPersonalInformationFieldKeys.ProfessionalTitle,
			MainPersonalInformationFieldKeys.Email,
		};
		var errors = new[]
		{
			new FieldValidationError(MainPersonalInformationFieldKeys.Email, TranslationKeys.ValidationEmailFormat),
		};

		var first = FormValidationService.GetFirstInvalidFieldKey(orderedKeys, new FieldValidationResult(errors));

		Assert.Equal(MainPersonalInformationFieldKeys.Email, first);
	}

	[Fact]
	public void ApplyExportFailure_WithEmptyErrors_DoesNotMarkTouches()
	{
		var touchTracker = new ValidationTouchTracker();

		FormValidationService.ApplyExportFailure(new FieldValidationResult([]), touchTracker);

		Assert.False(touchTracker.IsTouched(MainPersonalInformationFieldKeys.FirstName));
	}

	[Fact]
	public void GetFirstInvalidFieldKey_WithEmptyErrors_ReturnsNull()
	{
		var first = FormValidationService.GetFirstInvalidFieldKey(
			[MainPersonalInformationFieldKeys.FirstName],
			new FieldValidationResult([]));

		Assert.Null(first);
	}
}
