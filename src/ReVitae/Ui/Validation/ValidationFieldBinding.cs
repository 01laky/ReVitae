using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Automation;
using ReVitae.Core.Validation.Presentation;
using ReVitae.Ui;

namespace ReVitae.Ui.Validation;

public sealed class ValidationFieldBinding
{
    private readonly Control _input;
    private readonly TextBlock _errorTextBlock;
    private readonly string _fieldKey;

    public ValidationFieldBinding(string fieldKey, Control input, TextBlock errorTextBlock)
    {
        _fieldKey = fieldKey;
        _input = input;
        _errorTextBlock = errorTextBlock;
    }

    public string FieldKey => _fieldKey;

    public Control Input => _input;

    public void Apply(IReadOnlyList<string> messages, bool shouldDisplay)
    {
        if (!shouldDisplay || messages.Count == 0)
        {
            ClearPresentation();
            return;
        }

        var text = string.Join(Environment.NewLine, messages);
        _errorTextBlock.Text = text;
        _errorTextBlock.IsVisible = true;
        _errorTextBlock.Foreground = ValidationUiFactory.ErrorForeground;
        _input.Classes.Remove(UiClasses.ImportHint);
        _input.Classes.Add(UiClasses.FieldInvalid);
        AutomationProperties.SetHelpText(_input, text);
    }

    public void ClearPresentation()
    {
        _errorTextBlock.Text = string.Empty;
        _errorTextBlock.IsVisible = false;
        _errorTextBlock.Foreground = ValidationUiFactory.ErrorForeground;
        _input.Classes.Remove(UiClasses.FieldInvalid);
        AutomationProperties.SetHelpText(_input, null);
    }

    public void WireTouchTracking(ValidationTouchTracker touchTracker, Action<string>? onTouched = null)
    {
        void MarkTouched()
        {
            touchTracker.MarkTouched(_fieldKey);
            onTouched?.Invoke(_fieldKey);
        }

        switch (_input)
        {
            case TextBox textBox:
                textBox.LostFocus += (_, _) => MarkTouched();
                textBox.TextChanged += (_, _) =>
                {
                    if (touchTracker.HasAttemptedExportWithInvalidForm)
                    {
                        MarkTouched();
                    }
                };
                break;
            case ComboBox comboBox:
                comboBox.LostFocus += (_, _) => MarkTouched();
                comboBox.SelectionChanged += (_, _) =>
                {
                    if (touchTracker.HasAttemptedExportWithInvalidForm)
                    {
                        MarkTouched();
                    }
                };
                break;
            case AutoCompleteBox autoComplete:
                autoComplete.LostFocus += (_, _) => MarkTouched();
                autoComplete.TextChanged += (_, _) =>
                {
                    if (touchTracker.HasAttemptedExportWithInvalidForm)
                    {
                        MarkTouched();
                    }
                };
                break;
            case DatePicker datePicker:
                datePicker.SelectedDateChanged += (_, _) => MarkTouched();
                break;
        }
    }
}
