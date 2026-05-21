using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Core.Validation.Presentation;
using ReVitae.Ui;

namespace ReVitae.Ui.Validation;

public sealed class ValidatedDateRangeBinding
{
    private readonly DatePicker _datePicker;
    private readonly TextBlock _errorTextBlock;

    public ValidatedDateRangeBinding(
        string registryKey,
        string monthFieldKey,
        string yearFieldKey,
        string dateRangeFieldKey,
        DatePicker datePicker,
        TextBlock errorTextBlock)
    {
        RegistryKey = registryKey;
        MonthFieldKey = monthFieldKey;
        YearFieldKey = yearFieldKey;
        DateRangeFieldKey = dateRangeFieldKey;
        _datePicker = datePicker;
        _errorTextBlock = errorTextBlock;
    }

    public string RegistryKey { get; }

    public string MonthFieldKey { get; }

    public string YearFieldKey { get; }

    public string DateRangeFieldKey { get; }

    public void WireTouchTracking(ValidationTouchTracker touchTracker)
    {
        new ValidationFieldBinding(MonthFieldKey, _datePicker, _errorTextBlock)
            .WireTouchTracking(touchTracker);
    }

    public void Apply(
        IReadOnlyList<FieldValidationError> errors,
        AppLocalizer localizer,
        ValidationTouchTracker touchTracker)
    {
        var messages = new List<string>();
        AppendMessages(messages, ValidationFieldPresenter.GetMessagesForExactKey(errors, MonthFieldKey, localizer.Get));
        AppendMessages(messages, ValidationFieldPresenter.GetMessagesForExactKey(errors, YearFieldKey, localizer.Get));
        AppendMessages(messages, ValidationFieldPresenter.GetMessagesForExactKey(errors, DateRangeFieldKey, localizer.Get));

        var shouldDisplay = touchTracker.ShouldDisplayErrors(MonthFieldKey, messages.Count > 0)
            || touchTracker.ShouldDisplayErrors(YearFieldKey, messages.Count > 0)
            || touchTracker.ShouldDisplayErrors(DateRangeFieldKey, messages.Count > 0);

        ApplyPresentation(_datePicker, _errorTextBlock, messages, shouldDisplay && messages.Count > 0);
    }

    public void ClearPresentation()
    {
        ApplyPresentation(_datePicker, _errorTextBlock, [], false);
    }

    public Control? FindControlForFieldKey(string fieldKey)
    {
        if (string.Equals(fieldKey, MonthFieldKey, StringComparison.Ordinal)
            || string.Equals(fieldKey, YearFieldKey, StringComparison.Ordinal)
            || string.Equals(fieldKey, DateRangeFieldKey, StringComparison.Ordinal))
        {
            return _datePicker;
        }

        return null;
    }

    public static StackPanel CreatePanel(
        string labelText,
        DatePicker datePicker,
        string registryKey,
        string monthFieldKey,
        string yearFieldKey,
        string dateRangeFieldKey,
        ValidationFieldRegistry registry,
        ValidationTouchTracker touchTracker)
    {
        var error = ValidationUiFactory.CreateErrorTextBlock();

        var binding = new ValidatedDateRangeBinding(
            registryKey,
            monthFieldKey,
            yearFieldKey,
            dateRangeFieldKey,
            datePicker,
            error);
        binding.WireTouchTracking(touchTracker);
        registry.Register(binding);

        var label = new TextBlock { Text = labelText };
        var panel = new StackPanel
        {
            Spacing = 6,
            Children = { label, datePicker, error }
        };
        panel.Classes.Add(UiClasses.FormField);
        return panel;
    }

    private static void AppendMessages(List<string> target, IReadOnlyList<string> messages)
    {
        foreach (var message in messages)
        {
            if (!target.Exists(existing => string.Equals(existing, message, StringComparison.Ordinal)))
            {
                target.Add(message);
            }
        }
    }

    private static void ApplyPresentation(
        Control input,
        TextBlock errorTextBlock,
        IReadOnlyList<string> messages,
        bool shouldDisplay)
    {
        if (!shouldDisplay || messages.Count == 0)
        {
            errorTextBlock.Text = string.Empty;
            errorTextBlock.IsVisible = false;
            errorTextBlock.Foreground = ValidationUiFactory.ErrorForeground;
            input.Classes.Remove(UiClasses.FieldInvalid);
            return;
        }

        errorTextBlock.Text = string.Join(Environment.NewLine, messages);
        errorTextBlock.IsVisible = true;
        errorTextBlock.Foreground = ValidationUiFactory.ErrorForeground;
        input.Classes.Remove(UiClasses.ImportHint);
        input.Classes.Add(UiClasses.FieldInvalid);
    }
}
