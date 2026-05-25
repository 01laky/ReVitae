using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Core.Validation.Presentation;
using ReVitae.Ui;

namespace ReVitae.Ui.Validation;

public sealed class ValidationFieldRegistry
{
	private readonly Dictionary<string, ValidationFieldBinding> _bindings = new(StringComparer.Ordinal);
	private readonly Dictionary<string, ValidatedDateRangeBinding> _dateBindings = new(StringComparer.Ordinal);
	private readonly Dictionary<string, ChipValidationTarget> _chipTargets = new(StringComparer.Ordinal);

	public void Register(ValidationFieldBinding binding) => _bindings[binding.FieldKey] = binding;

	public void Register(ValidatedDateRangeBinding binding) => _dateBindings[binding.RegistryKey] = binding;

	public void RegisterChip(string fieldKey, ChipValidationTarget target) => _chipTargets[fieldKey] = target;

	public IReadOnlyCollection<string> RegisteredFieldKeys
	{
		get
		{
			var keys = new HashSet<string>(_bindings.Keys, StringComparer.Ordinal);
			foreach (var dateBinding in _dateBindings.Values)
			{
				keys.Add(dateBinding.MonthFieldKey);
				keys.Add(dateBinding.YearFieldKey);
				keys.Add(dateBinding.DateRangeFieldKey);
			}

			keys.UnionWith(_chipTargets.Keys);
			return keys;
		}
	}

	public void ApplyErrors(
		IReadOnlyList<FieldValidationError> errors,
		AppLocalizer localizer,
		ValidationTouchTracker touchTracker,
		Func<FieldValidationError, string>? resolveTargetKey = null)
	{
		var messageMap = ValidationFieldPresenter.BuildMessageMap(
			errors,
			localizer.Get,
			resolveTargetKey);

		foreach (var binding in _bindings.Values)
		{
			messageMap.TryGetValue(binding.FieldKey, out var messages);
			messages ??= [];
			var shouldDisplay = touchTracker.ShouldDisplayErrors(binding.FieldKey, messages.Count > 0);
			binding.Apply(messages, shouldDisplay);
		}

		foreach (var dateBinding in _dateBindings.Values)
		{
			dateBinding.Apply(errors, localizer, touchTracker);
		}

		foreach (var (fieldKey, target) in _chipTargets)
		{
			messageMap.TryGetValue(fieldKey, out var messages);
			messages ??= [];
			var shouldDisplay = touchTracker.ShouldDisplayErrors(fieldKey, messages.Count > 0);
			target.Apply(messages, shouldDisplay);
		}
	}

	public void ClearAll()
	{
		foreach (var binding in _bindings.Values)
		{
			binding.ClearPresentation();
		}

		foreach (var dateBinding in _dateBindings.Values)
		{
			dateBinding.ClearPresentation();
		}

		foreach (var chip in _chipTargets.Values)
		{
			chip.ClearPresentation();
		}
	}

	public Control? FindControlForFieldKey(string fieldKey)
	{
		if (_bindings.TryGetValue(fieldKey, out var binding))
		{
			return binding.Input;
		}

		foreach (var dateBinding in _dateBindings.Values)
		{
			var control = dateBinding.FindControlForFieldKey(fieldKey);
			if (control is not null)
			{
				return control;
			}
		}

		return _chipTargets.TryGetValue(fieldKey, out var chip) ? chip.ChipBorder : null;
	}

	public static StackPanel CreateFieldPanel(
		string labelText,
		Control input,
		string fieldKey,
		ValidationFieldRegistry registry,
		ValidationTouchTracker touchTracker,
		TextBlock? counter = null)
	{
		var label = new TextBlock { Text = labelText };
		var error = ValidationUiFactory.CreateErrorTextBlock();
		var binding = new ValidationFieldBinding(fieldKey, input, error);
		binding.WireTouchTracking(touchTracker);
		registry.Register(binding);

		var panel = new StackPanel
		{
			Spacing = 6,
			Children = { label, input }
		};
		if (counter is not null)
		{
			panel.Children.Add(counter);
		}

		panel.Children.Add(error);
		panel.Classes.Add(UiClasses.FormField);
		return panel;
	}
}
