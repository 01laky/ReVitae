using ReVitae.Core.Validation.Presentation;

namespace ReVitae.Tests.Ui.Validation;

public sealed class ValidationTouchTrackingEdgeCaseTests
{
    private readonly ValidationTouchTracker _tracker = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void MarkTouched_IgnoresNullOrWhitespaceKeys(string? fieldKey)
    {
        _tracker.MarkTouched(fieldKey!);

        Assert.False(_tracker.IsTouched("validKey"));
    }

    [Fact]
    public void ShouldDisplayErrors_UntouchedInvalidField_ReturnsTrue()
    {
        Assert.True(_tracker.ShouldDisplayErrors("email", hasErrors: true));
    }

    [Fact]
    public void ShouldDisplayErrors_TouchedInvalidField_ReturnsTrue()
    {
        _tracker.MarkTouched("email");

        Assert.True(_tracker.ShouldDisplayErrors("email", hasErrors: true));
    }

    [Fact]
    public void ShouldDisplayErrors_ValidFieldAfterTouch_ReturnsFalse()
    {
        _tracker.MarkTouched("email");

        Assert.False(_tracker.ShouldDisplayErrors("email", hasErrors: false));
    }

    [Fact]
    public void ShouldDisplayErrors_FieldWithoutErrors_ReturnsFalse_EvenWhenTouched()
    {
        _tracker.MarkTouched("email");

        Assert.False(_tracker.ShouldDisplayErrors("email", hasErrors: false));
    }

    [Fact]
    public void MarkExportAttemptWithInvalidForm_TouchesAllInvalidKeysAndSetsExportFlag()
    {
        _tracker.MarkExportAttemptWithInvalidForm(["firstName", "email", "   "]);

        Assert.True(_tracker.HasAttemptedExportWithInvalidForm);
        Assert.True(_tracker.IsTouched("firstName"));
        Assert.True(_tracker.IsTouched("email"));
        Assert.False(_tracker.IsTouched("   "));
    }

    [Fact]
    public void ShouldDisplayErrors_AfterFailedExport_ShowsAnyFieldWithErrorsWithoutIndividualTouch()
    {
        _tracker.MarkExportAttemptWithInvalidForm(["firstName", "workExperience.entry.company"]);

        Assert.True(_tracker.ShouldDisplayErrors("firstName", hasErrors: true));
        Assert.True(_tracker.ShouldDisplayErrors("workExperience.entry.company", hasErrors: true));
        Assert.True(_tracker.ShouldDisplayErrors("lastName", hasErrors: true));
        Assert.False(_tracker.ShouldDisplayErrors("lastName", hasErrors: false));
    }

    [Fact]
    public void MarkTouched_AfterFailedExport_AllowsPerFieldTouchForEditedField()
    {
        _tracker.MarkExportAttemptWithInvalidForm(["email"]);
        _tracker.MarkTouched("phone");

        Assert.True(_tracker.ShouldDisplayErrors("email", hasErrors: true));
        Assert.True(_tracker.ShouldDisplayErrors("phone", hasErrors: true));
    }

    [Fact]
    public void MarkManyTouched_MarksAllNonBlankKeys()
    {
        _tracker.MarkManyTouched(["firstName", "", "lastName", "  "]);

        Assert.True(_tracker.IsTouched("firstName"));
        Assert.True(_tracker.IsTouched("lastName"));
        Assert.False(_tracker.IsTouched(""));
    }

    [Fact]
    public void ClearField_RemovesTouchStateForSingleField()
    {
        _tracker.MarkTouched("email");
        _tracker.MarkTouched("phone");
        _tracker.ClearField("email");

        Assert.False(_tracker.IsTouched("email"));
        Assert.True(_tracker.IsTouched("phone"));
        Assert.True(_tracker.ShouldDisplayErrors("email", hasErrors: true));
    }

    [Fact]
    public void Reset_ClearsTouchStateAndExportAttemptFlag()
    {
        _tracker.MarkExportAttemptWithInvalidForm(["firstName", "email"]);
        _tracker.Reset();

        Assert.False(_tracker.HasAttemptedExportWithInvalidForm);
        Assert.False(_tracker.IsTouched("firstName"));
        Assert.True(_tracker.ShouldDisplayErrors("firstName", hasErrors: true));
    }

    [Fact]
    public void IsTouched_ReturnsFalseForBlankKey_EvenAfterExportAttempt()
    {
        _tracker.MarkExportAttemptWithInvalidForm(["email"]);

        Assert.False(_tracker.IsTouched("   "));
    }

    [Fact]
    public void ClearField_OnExportTouchedField_ClearsTouchStateButStillDisplaysWhenInvalid()
    {
        _tracker.MarkExportAttemptWithInvalidForm(["workExperience.entry.endMonth"]);
        _tracker.ClearField("workExperience.entry.endMonth");

        Assert.False(_tracker.IsTouched("workExperience.entry.endMonth"));
        Assert.True(_tracker.HasAttemptedExportWithInvalidForm);
        Assert.True(_tracker.ShouldDisplayErrors("workExperience.entry.endMonth", hasErrors: true));
    }

    [Fact]
    public void SimulatedCurrentlyWorkingToggle_ClearsEndDateTouch_StartDateRemainsTouched()
    {
        const string startKey = "workExperience.entry.startMonth";
        const string endKey = "workExperience.entry.endMonth";

        _tracker.MarkTouched(startKey);
        _tracker.MarkTouched(endKey);
        _tracker.ClearField(endKey);

        Assert.True(_tracker.IsTouched(startKey));
        Assert.False(_tracker.IsTouched(endKey));
        Assert.False(_tracker.ShouldDisplayErrors(endKey, hasErrors: false));
        Assert.True(_tracker.ShouldDisplayErrors(startKey, hasErrors: true));
    }

    [Fact]
    public void SimulatedReimport_ResetClearsStaleTouchForReplacedControls()
    {
        _tracker.MarkTouched("skills.group-1.category");
        _tracker.MarkExportAttemptWithInvalidForm(["languages.entry.language"]);
        _tracker.Reset();

        Assert.False(_tracker.IsTouched("skills.group-1.category"));
        Assert.False(_tracker.HasAttemptedExportWithInvalidForm);
    }

    [Fact]
    public void SimulatedImportPopulatedLowConfidenceField_TreatedAsTouchedForDisplay()
    {
        const string importedKey = "email";

        _tracker.MarkTouched(importedKey);

        Assert.True(_tracker.ShouldDisplayErrors(importedKey, hasErrors: true));
    }

    [Fact]
    public void DraftEntryWithoutValidatorErrors_DoesNotShowErrors()
    {
        const string draftEntryKey = "workExperience.draft-entry.company";

        Assert.False(_tracker.ShouldDisplayErrors(draftEntryKey, hasErrors: false));
    }
}
