using System;
using System.Collections.Generic;
using Avalonia.Controls;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Core.Validation.Presentation;

namespace ReVitae.Ui.Validation;

public static class FormValidationService
{
    public static void ApplyExportFailure(
        FieldValidationResult validationResult,
        ValidationTouchTracker touchTracker)
    {
        touchTracker.MarkExportAttemptWithInvalidForm(
            ValidationNavigationPlanner.CollectInvalidKeys(validationResult.Errors));
    }

    public static string? GetFirstInvalidFieldKey(
        IReadOnlyList<string> orderedFieldKeys,
        FieldValidationResult validationResult) =>
        ValidationNavigationPlanner.GetFirstInvalidFieldKey(
            orderedFieldKeys,
            ValidationNavigationPlanner.CollectInvalidKeys(validationResult.Errors));

    public static void UpdateSectionErrorBadge(
        StackPanel badgePanel,
        TextBlock badgeTextBlock,
        int errorCount,
        bool isSectionCollapsed,
        AppLocalizer localizer,
        string countMessageKey,
        Action? expandSection = null)
    {
        ValidationErrorBadgeFactory.Update(
            badgePanel,
            badgeTextBlock,
            errorCount,
            isSectionCollapsed,
            localizer.Format(countMessageKey, errorCount),
            expandSection);
    }
}
