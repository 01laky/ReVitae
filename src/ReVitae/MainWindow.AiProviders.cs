using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Download;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReVitae;

public partial class MainWindow
{
	private readonly AiProviderConfigService _aiProviderConfigService = new();
	private AiActiveBackendService? _aiActiveBackendService;
	private readonly Dictionary<string, AiProviderUiRow> _aiProviderUiRows = new(StringComparer.Ordinal);
	private readonly Dictionary<string, AiProviderUiRow> _wizardProviderUiRows = new(StringComparer.Ordinal);
	private string? _openProviderFormId;
	private string? _pendingProviderSwitchTargetId;
	private AiBackendKind _pendingProviderSwitchTargetKind;
	private string? _pendingProviderSwitchTargetLocalModelId;
	private string? _pendingUntestedProviderId;

	private AiActiveBackendService ActiveBackendService =>
		_aiActiveBackendService ??= new AiActiveBackendService(_aiProviderConfigService);

	private void InitializeAiProviders()
	{
		_aiProviderConfigService.Load();
		_aiProviderConfigService.SettingsChanged += OnAiProviderSettingsChanged;
		ActiveBackendService.ActiveBackendChanged += OnAiActiveBackendChanged;
		RenderAiProviderRows();
		UpdateAiActiveBackendStrip();
		UpdateAiHeaderBackendBadges();
	}

	private void OnAiProviderSettingsChanged()
	{
		Dispatcher.UIThread.Post(() =>
		{
			RefreshAiProviderRowStates();
			UpdateAiActiveBackendStrip();
			UpdateAiHeaderBackendBadges();
			RefreshAiSetupModelCardsIfVisible();
		});
	}

	private void OnAiActiveBackendChanged()
	{
		_appPreferencesService.ClearHideAiPromotionsOnBackendActivated();
		OnFirstLaunchAiWizardBackendActivated();
		OnAiProviderSettingsChanged();
	}

	private void RenderAiProviderRows()
	{
		AiSetupOnlineProvidersPanel.Children.Clear();
		_aiProviderUiRows.Clear();

		foreach (var provider in AiOnlineProviderCatalog.GetAll())
		{
			var row = BuildProviderRow(provider);
			_aiProviderUiRows[provider.Id] = row;
			AiSetupOnlineProvidersPanel.Children.Add(row.Container);
		}

		RefreshAiProviderRowStates();
	}

	private AiProviderUiRow BuildProviderRow(AiOnlineProviderDefinition provider)
	{
		var container = new StackPanel { Spacing = 0 };
		var card = new Border { Classes = { "re-vitae-app-card" }, Padding = new Thickness(20) };
		var cardContent = new StackPanel { Spacing = 6 };

		var headerRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };
		var titleStack = new StackPanel { Spacing = 2 };
		titleStack.Children.Add(new TextBlock
		{
			Text = _localizer.Get(provider.DisplayNameKey),
			Classes = { "re-vitae-app-title" },
			FontSize = 16,
		});
		titleStack.Children.Add(new TextBlock
		{
			Text = _localizer.Get(provider.DescriptionKey),
			Classes = { "re-vitae-secondary" },
			TextWrapping = TextWrapping.Wrap,
			FontSize = 12,
		});
		Grid.SetColumn(titleStack, 0);
		headerRow.Children.Add(titleStack);

		var editButton = new Button
		{
			Content = _localizer.Get(TranslationKeys.AiSetupProviderEdit),
			Classes = { "re-vitae-secondary" },
			Margin = new Thickness(0, 0, 8, 0),
			IsVisible = false,
		};
		editButton.Click += (_, _) => ToggleProviderForm(provider.Id, true);
		Grid.SetColumn(editButton, 1);
		headerRow.Children.Add(editButton);

		var primaryButton = new Button { Classes = { "re-vitae-primary" } };
		primaryButton.Click += async (_, _) => await OnProviderPrimaryActionClicked(provider.Id).ConfigureAwait(true);
		Grid.SetColumn(primaryButton, 2);
		headerRow.Children.Add(primaryButton);

		cardContent.Children.Add(headerRow);

		var badgesRow = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8,
		};
		var freeTierBadge = new TextBlock
		{
			Text = _localizer.Get(TranslationKeys.AiSetupProviderFreeTier),
			Classes = { "re-vitae-primary" },
			FontSize = 11,
			FontWeight = FontWeight.SemiBold,
			IsVisible = provider.HasFreeTierBadge,
		};
		var statusText = new TextBlock { FontSize = 12, FontWeight = FontWeight.SemiBold };
		var lastTestText = new TextBlock
		{
			Classes = { "re-vitae-secondary" },
			FontSize = 11,
			IsVisible = false,
		};
		badgesRow.Children.Add(freeTierBadge);
		badgesRow.Children.Add(statusText);
		badgesRow.Children.Add(lastTestText);
		cardContent.Children.Add(badgesRow);

		card.Child = cardContent;
		container.Children.Add(card);

		var formPanel = BuildProviderForm(provider);
		formPanel.IsVisible = false;
		container.Children.Add(formPanel);

		return new AiProviderUiRow(
			provider.Id,
			container,
			card,
			primaryButton,
			editButton,
			statusText,
			lastTestText,
			formPanel);
	}

	private StackPanel BuildProviderForm(AiOnlineProviderDefinition provider)
	{
		var panel = new StackPanel
		{
			Spacing = 10,
			Margin = new Thickness(0, 8, 0, 0),
			Classes = { "re-vitae-app-card" },
		};
		panel.Margin = new Thickness(0, 8, 0, 0);

		var fields = new Dictionary<string, Control>(StringComparer.Ordinal);
		foreach (var field in provider.Fields.Where(field => !field.Advanced))
		{
			panel.Children.Add(BuildProviderField(provider, field, fields));
		}

		var advancedExpander = new Expander
		{
			Header = _localizer.Get(TranslationKeys.AiSetupProviderAdvanced),
			IsExpanded = false,
		};
		var advancedPanel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
		foreach (var field in provider.Fields.Where(field => field.Advanced))
		{
			advancedPanel.Children.Add(BuildProviderField(provider, field, fields));
		}

		if (provider.Id == "custom-openai")
		{
			advancedPanel.Children.Insert(0, new TextBlock
			{
				Text = _localizer.Get(TranslationKeys.AiSetupCustomBaseUrlHint),
				Classes = { "re-vitae-secondary" },
				TextWrapping = TextWrapping.Wrap,
				FontSize = 12,
			});
		}

		advancedExpander.Content = advancedPanel;
		if (provider.Fields.Any(field => field.Advanced))
		{
			panel.Children.Add(advancedExpander);
		}

		if (!string.IsNullOrWhiteSpace(provider.ModelUseCaseHintKey))
		{
			panel.Children.Add(new TextBlock
			{
				Text = _localizer.Get(provider.ModelUseCaseHintKey!),
				Classes = { "re-vitae-secondary" },
				FontSize = 12,
			});
		}

		var feedbackText = new TextBlock { TextWrapping = TextWrapping.Wrap, IsVisible = false };
		panel.Children.Add(feedbackText);

		var buttonsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		var saveButton = new Button
		{
			Content = _localizer.Get(TranslationKeys.AiSetupProviderSave),
			Classes = { "re-vitae-primary" },
			IsEnabled = false,
		};
		var testButton = new Button
		{
			Content = _localizer.Get(TranslationKeys.AiSetupProviderTest),
			Classes = { "re-vitae-secondary" },
			IsEnabled = false,
		};
		var resetButton = new Button
		{
			Content = _localizer.Get(TranslationKeys.AiSetupProviderReset),
			Classes = { "re-vitae-secondary" },
		};
		var cancelButton = new Button
		{
			Content = _localizer.Get(TranslationKeys.Cancel),
			Classes = { "re-vitae-secondary" },
		};

		saveButton.Click += async (_, _) => await OnProviderSaveClicked(provider.Id).ConfigureAwait(true);
		testButton.Click += async (_, _) => await OnProviderTestClicked(provider.Id).ConfigureAwait(true);
		resetButton.Click += (_, _) => OnProviderResetClicked(provider.Id);
		cancelButton.Click += (_, _) => ToggleProviderForm(provider.Id, false);

		void RefreshButtons()
		{
			var draft = ReadProviderDraft(provider.Id, fields);
			var valid = AiProviderConfigValidator.IsValid(provider, draft);
			saveButton.IsEnabled = valid;
			testButton.IsEnabled = valid;
		}

		foreach (var control in fields.Values)
		{
			switch (control)
			{
				case TextBox textBox:
					textBox.TextChanged += (_, _) => RefreshButtons();
					break;
				case ComboBox comboBox:
					comboBox.SelectionChanged += (_, _) => RefreshButtons();
					break;
				case StackPanel modelPanel:
					WireModelSelectorRefresh(modelPanel, RefreshButtons);
					break;
			}
		}

		buttonsRow.Children.Add(saveButton);
		buttonsRow.Children.Add(testButton);
		buttonsRow.Children.Add(resetButton);
		buttonsRow.Children.Add(cancelButton);
		panel.Children.Add(buttonsRow);

		panel.Tag = new AiProviderFormState(fields, feedbackText, saveButton, testButton, RefreshButtons);
		return panel;
	}

	private Control BuildProviderField(
		AiOnlineProviderDefinition provider,
		AiProviderFieldDefinition field,
		IDictionary<string, Control> fields)
	{
		var stack = new StackPanel { Spacing = 4 };
		stack.Children.Add(new TextBlock
		{
			Text = _localizer.Get(field.LabelKey),
			Classes = { "re-vitae-secondary" },
			FontSize = 12,
		});

		Control input = field.Kind switch
		{
			AiProviderFieldKind.Password => CreateSecretTextBox(),
			AiProviderFieldKind.ModelSelect => BuildModelSelector(provider, fields),
			_ => new TextBox(),
		};

		fields[field.Id] = input;
		stack.Children.Add(input);
		return stack;
	}

	private static TextBox CreateSecretTextBox() => new() { PasswordChar = '●' };

	private Control BuildModelSelector(
		AiOnlineProviderDefinition provider,
		IDictionary<string, Control> fields)
	{
		var container = new StackPanel { Spacing = 6 };
		var combo = new ComboBox
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			PlaceholderText = _localizer.Get(TranslationKeys.AiSetupProviderFieldModelId),
		};

		foreach (var option in provider.SuggestedModels)
		{
			combo.Items.Add(option.ModelId);
		}

		var customLabel = _localizer.Get(TranslationKeys.AiSetupProviderFieldCustomModel);
		combo.Items.Add(customLabel);

		var customModelBox = new TextBox
		{
			PlaceholderText = _localizer.Get(TranslationKeys.AiSetupProviderFieldCustomModel),
			IsVisible = false,
		};
		fields[AiProviderFieldIds.ModelId + "_custom"] = customModelBox;
		container.Children.Add(combo);
		container.Children.Add(customModelBox);

		combo.SelectionChanged += (_, _) =>
		{
			customModelBox.IsVisible = string.Equals(combo.SelectedItem as string, customLabel, StringComparison.Ordinal);
		};

		return container;
	}

	private void RenderWizardCuratedProviderRows()
	{
		FirstLaunchAiWizardOnlineProvidersPanel.Children.Clear();
		_wizardProviderUiRows.Clear();

		foreach (var provider in FirstLaunchAiWizardCuratedProviders.GetDefinitions())
		{
			var row = BuildProviderRow(provider);
			_wizardProviderUiRows[provider.Id] = row;
			FirstLaunchAiWizardOnlineProvidersPanel.Children.Add(row.Container);
		}

		RefreshWizardProviderRowStates();
	}

	private void RefreshWizardProviderRowStates()
	{
		foreach (var providerId in FirstLaunchAiWizardCuratedProviders.ProviderIds)
		{
			if (!_wizardProviderUiRows.TryGetValue(providerId, out var row))
			{
				continue;
			}

			var provider = AiOnlineProviderCatalog.TryGetById(providerId);
			if (provider is null)
			{
				continue;
			}

			ApplyProviderRowPresentation(provider, row);
		}
	}

	private void RefreshAiProviderRowStates()
	{
		foreach (var provider in AiOnlineProviderCatalog.GetAll())
		{
			if (!_aiProviderUiRows.TryGetValue(provider.Id, out var row))
			{
				continue;
			}

			ApplyProviderRowPresentation(provider, row);
		}

		RefreshWizardProviderRowStates();
	}

	private void ApplyProviderRowPresentation(AiOnlineProviderDefinition provider, AiProviderUiRow row)
	{
		var presentation = AiProviderRowStateMapper.Map(
			provider,
			_aiProviderConfigService.CurrentSettings,
			_aiProviderConfigService.SecretStorage);

		row.PrimaryButton.Content = presentation.PrimaryAction switch
		{
			AiProviderUiAction.Configure => _localizer.Get(TranslationKeys.AiSetupProviderConfigure),
			AiProviderUiAction.Activate => _localizer.Get(TranslationKeys.AiSetupProviderActivate),
			AiProviderUiAction.Deactivate => _localizer.Get(TranslationKeys.AiSetupProviderDeactivate),
			_ => _localizer.Get(TranslationKeys.AiSetupProviderConfigure),
		};

		row.EditButton.IsVisible = presentation.ShowEditLink;
		row.StatusText.Text = presentation.IsActive
			? _localizer.Get(TranslationKeys.AiSetupProviderActive)
			: presentation.IsConfigured
				? _localizer.Get(TranslationKeys.AiSetupProviderConfigured)
				: _localizer.Get(TranslationKeys.AiSetupProviderNotConfigured);
		row.StatusText.Classes.Clear();
		row.StatusText.Classes.Add(presentation.IsActive ? "re-vitae-primary" : "re-vitae-secondary");

		if (presentation.LastTestSucceeded is true)
		{
			row.LastTestText.Text = _localizer.Get(TranslationKeys.AiSetupProviderLastTestOk);
			row.LastTestText.Classes.Clear();
			row.LastTestText.Classes.Add("re-vitae-primary");
			row.LastTestText.IsVisible = true;
		}
		else if (presentation.LastTestSucceeded is false)
		{
			row.LastTestText.Text = _localizer.Get(TranslationKeys.AiSetupProviderLastTestFailed);
			row.LastTestText.Classes.Clear();
			row.LastTestText.Classes.Add("re-vitae-error");
			row.LastTestText.IsVisible = true;
		}
		else
		{
			row.LastTestText.IsVisible = false;
		}

		row.CardBorder.BorderBrush = presentation.IsActive
			? new SolidColorBrush(Color.Parse("#2563EB"))
			: Brushes.Transparent;
	}

	private void ToggleProviderForm(string providerId, bool open)
	{
		if (open)
		{
			CloseAllProviderFormsExcept(providerId);
			_openProviderFormId = providerId;
			PopulateProviderForm(providerId);
		}
		else
		{
			if (TryGetProviderRow(providerId, out var row))
			{
				row.FormPanel.IsVisible = false;
			}

			if (string.Equals(_openProviderFormId, providerId, StringComparison.Ordinal))
			{
				_openProviderFormId = null;
			}
		}
	}

	private bool TryGetProviderRow(string providerId, out AiProviderUiRow row)
	{
		if (_aiProviderUiRows.TryGetValue(providerId, out row!))
		{
			return true;
		}

		return _wizardProviderUiRows.TryGetValue(providerId, out row!);
	}

	private void CloseAllProviderFormsExcept(string providerId)
	{
		foreach (var pair in _aiProviderUiRows)
		{
			((StackPanel)pair.Value.FormPanel).IsVisible =
				string.Equals(pair.Key, providerId, StringComparison.Ordinal);
		}

		foreach (var pair in _wizardProviderUiRows)
		{
			((StackPanel)pair.Value.FormPanel).IsVisible =
				string.Equals(pair.Key, providerId, StringComparison.Ordinal);
		}
	}

	private void PopulateProviderForm(string providerId)
	{
		if (!TryGetProviderRow(providerId, out var row))
		{
			return;
		}

		var provider = AiOnlineProviderCatalog.TryGetById(providerId)!;
		var draft = _aiProviderConfigService.GetDraft(providerId);
		var state = (AiProviderFormState)row.FormPanel.Tag!;

		foreach (var field in provider.Fields)
		{
			if (!state.Fields.TryGetValue(field.Id, out var control))
			{
				continue;
			}

			var value = draft.Values.TryGetValue(field.Id, out var fieldValue) ? fieldValue : null;
			switch (control)
			{
				case TextBox textBox when field.Id == AiProviderFieldIds.ApiKey:
					textBox.Text = draft.ApiKey ?? string.Empty;
					break;
				case TextBox textBox:
					textBox.Text = value ?? string.Empty;
					break;
				case StackPanel modelPanel when field.Kind == AiProviderFieldKind.ModelSelect:
					PopulateModelSelector(modelPanel, state.Fields, value, provider);
					break;
			}
		}

		state.RefreshButtons();
		row.FormPanel.IsVisible = true;
	}

	private AiProviderConnectionDraft ReadProviderDraft(string providerId, IReadOnlyDictionary<string, Control> fields)
	{
		var provider = AiOnlineProviderCatalog.TryGetById(providerId)!;
		var values = new Dictionary<string, string?>(StringComparer.Ordinal);
		string? apiKey = null;

		foreach (var field in provider.Fields)
		{
			if (!fields.TryGetValue(field.Id, out var control))
			{
				continue;
			}

			values[field.Id] = control switch
			{
				TextBox textBox => textBox.Text,
				StackPanel modelPanel when field.Kind == AiProviderFieldKind.ModelSelect =>
					ResolveModelComboValue(TryGetModelCombo(modelPanel)!, fields),
				_ => null,
			};

			if (field.Id == AiProviderFieldIds.ApiKey && control is TextBox passwordBox)
			{
				apiKey = string.IsNullOrWhiteSpace(passwordBox.Text)
					? _aiProviderConfigService.SecretStorage.TryGetApiKey(providerId)
					: passwordBox.Text;
			}
		}

		if (string.IsNullOrWhiteSpace(values.GetValueOrDefault(AiProviderFieldIds.BaseUrl)) &&
			!string.IsNullOrWhiteSpace(provider.DefaultBaseUrl))
		{
			values[AiProviderFieldIds.BaseUrl] = provider.DefaultBaseUrl;
		}

		return new AiProviderConnectionDraft(providerId, values, apiKey);
	}

	private string? ResolveModelComboValue(ComboBox comboBox, IReadOnlyDictionary<string, Control> fields)
	{
		if (comboBox.SelectedItem is string selected &&
			!string.Equals(selected, _localizer.Get(TranslationKeys.AiSetupProviderFieldCustomModel), StringComparison.Ordinal))
		{
			return selected;
		}

		if (fields.TryGetValue(AiProviderFieldIds.ModelId + "_custom", out var custom) && custom is TextBox customBox)
		{
			return customBox.Text;
		}

		return comboBox.SelectedItem as string;
	}

	private static ComboBox? TryGetModelCombo(StackPanel modelPanel) =>
		modelPanel.Children.OfType<ComboBox>().FirstOrDefault();

	private void PopulateModelSelector(
		StackPanel modelPanel,
		IReadOnlyDictionary<string, Control> fields,
		string? modelId,
		AiOnlineProviderDefinition provider)
	{
		var comboBox = TryGetModelCombo(modelPanel);
		if (comboBox is null)
		{
			return;
		}

		var resolvedModelId = modelId ?? provider.SuggestedModels.FirstOrDefault()?.ModelId;
		if (resolvedModelId is not null && comboBox.Items.Contains(resolvedModelId))
		{
			comboBox.SelectedItem = resolvedModelId;
			return;
		}

		if (!string.IsNullOrWhiteSpace(resolvedModelId))
		{
			comboBox.SelectedItem = _localizer.Get(TranslationKeys.AiSetupProviderFieldCustomModel);
			if (fields.TryGetValue(AiProviderFieldIds.ModelId + "_custom", out var customControl) &&
				customControl is TextBox customBox)
			{
				customBox.Text = resolvedModelId;
				customBox.IsVisible = true;
			}
		}
	}

	private static void WireModelSelectorRefresh(StackPanel modelPanel, Action refresh)
	{
		var combo = TryGetModelCombo(modelPanel);
		if (combo is not null)
		{
			combo.SelectionChanged += (_, _) => refresh();
		}

		foreach (var textBox in modelPanel.Children.OfType<TextBox>())
		{
			textBox.TextChanged += (_, _) => refresh();
		}
	}

	private async Task OnProviderPrimaryActionClicked(string providerId)
	{
		if (!TryGetProviderRow(providerId, out var row))
		{
			return;
		}

		var presentation = AiProviderRowStateMapper.Map(
			AiOnlineProviderCatalog.TryGetById(providerId)!,
			_aiProviderConfigService.CurrentSettings,
			_aiProviderConfigService.SecretStorage);

		switch (presentation.PrimaryAction)
		{
			case AiProviderUiAction.Configure:
				ToggleProviderForm(providerId, true);
				break;
			case AiProviderUiAction.Activate:
				await ActivateOnlineProviderAsync(providerId, skipUntestedWarning: false).ConfigureAwait(true);
				break;
			case AiProviderUiAction.Deactivate:
				ActiveBackendService.Deactivate();
				break;
		}
	}

	private async Task ActivateOnlineProviderAsync(string providerId, bool skipUntestedWarning)
	{
		if (ActiveBackendService.RequiresSwitchConfirmation(AiBackendKind.Online, providerId))
		{
			ShowProviderSwitchConfirm(AiBackendKind.Online, providerId, null);
			return;
		}

		if (!skipUntestedWarning && ActiveBackendService.NeedsUntestedActivationWarning(providerId))
		{
			_pendingUntestedProviderId = providerId;
			AiSetupProviderUntestedConfirmTextBlock.Text =
				_localizer.Get(TranslationKeys.AiSetupProviderActivateUntested);
			AiSetupProviderUntestedConfirmPanel.IsVisible = true;
			return;
		}

		if (ActiveBackendService.TryActivateOnline(providerId))
		{
			RefreshAiProviderRowStates();
			UpdateAiActiveBackendStrip();
			UpdateAiHeaderBackendBadges();
		}
	}

	private async Task OnProviderSaveClicked(string providerId)
	{
		if (!TryGetProviderRow(providerId, out var row))
		{
			return;
		}

		var state = (AiProviderFormState)row.FormPanel.Tag!;
		var draft = ReadProviderDraft(providerId, state.Fields);
		_aiProviderConfigService.SaveProviderConfig(providerId, draft);
		ShowProviderFeedback(row, _localizer.Get(TranslationKeys.AiSetupProviderSaved), success: true);
		RefreshAiProviderRowStates();
	}

	private async Task OnProviderTestClicked(string providerId)
	{
		if (!TryGetProviderRow(providerId, out var row))
		{
			return;
		}

		var state = (AiProviderFormState)row.FormPanel.Tag!;
		state.TestButton.IsEnabled = false;
		var draft = ReadProviderDraft(providerId, state.Fields);
		var result = await _aiProviderConfigService.TestAndPersistAsync(draft).ConfigureAwait(true);
		state.TestButton.IsEnabled = true;

		if (result.Succeeded)
		{
			ShowProviderFeedback(row, _localizer.Get(TranslationKeys.AiSetupProviderTestSuccess), success: true);
		}
		else
		{
			var messageKey = result.ErrorMessageKey ?? TranslationKeys.AiSetupProviderTestFailed;
			ShowProviderFeedback(row, _localizer.Get(messageKey), success: false);
		}

		RefreshAiProviderRowStates();
	}

	private void OnProviderResetClicked(string providerId)
	{
		var provider = AiOnlineProviderCatalog.TryGetById(providerId);
		if (provider is null)
		{
			return;
		}

		AiSetupProviderSwitchConfirmTextBlock.Text =
			_localizer.Format(TranslationKeys.AiSetupProviderResetConfirm, _localizer.Get(provider.DisplayNameKey));
		_pendingProviderSwitchTargetId = providerId;
		_pendingProviderSwitchTargetKind = AiBackendKind.None;
		AiSetupProviderSwitchConfirmPanel.IsVisible = true;
	}

	private void ShowProviderFeedback(AiProviderUiRow row, string message, bool success)
	{
		var state = (AiProviderFormState)row.FormPanel.Tag!;
		state.FeedbackText.Text = message;
		state.FeedbackText.Classes.Clear();
		state.FeedbackText.Classes.Add(success ? "re-vitae-primary" : "re-vitae-error");
		state.FeedbackText.IsVisible = true;
	}

	private void ShowProviderSwitchConfirm(AiBackendKind targetKind, string? providerId, string? localModelId)
	{
		var active = ActiveBackendService.GetActiveSnapshot();
		var fromLabel = FormatBackendLabel(active);
		var toLabel = targetKind switch
		{
			AiBackendKind.Local => FormatBackendLabel(new ActiveAiBackendSnapshot(
				AiBackendKind.Local,
				localModelId,
				null,
				AiModelCatalog.TryGetById(localModelId!)?.DisplayNameKey,
				AiModelCatalog.TryGetById(localModelId!)?.OllamaModelTag)),
			AiBackendKind.Online => FormatBackendLabel(new ActiveAiBackendSnapshot(
				AiBackendKind.Online,
				null,
				providerId,
				AiOnlineProviderCatalog.TryGetById(providerId!)?.DisplayNameKey,
				AiActiveBackendPresentation.GetOnlineModelLabel(_aiProviderConfigService.CurrentSettings, providerId!))),
			_ => string.Empty,
		};

		AiSetupProviderSwitchConfirmTextBlock.Text =
			_localizer.Format(TranslationKeys.AiSetupProviderSwitchConfirm, fromLabel, toLabel);
		_pendingProviderSwitchTargetKind = targetKind;
		_pendingProviderSwitchTargetId = providerId;
		_pendingProviderSwitchTargetLocalModelId = localModelId;
		AiSetupProviderSwitchConfirmPanel.IsVisible = true;
	}

	private string FormatBackendLabel(ActiveAiBackendSnapshot snapshot) =>
		snapshot.Kind switch
		{
			AiBackendKind.Local when snapshot.DisplayNameKey is not null =>
				_localizer.Get(snapshot.DisplayNameKey),
			AiBackendKind.Online when snapshot.DisplayNameKey is not null =>
				_localizer.Get(snapshot.DisplayNameKey),
			_ => _localizer.Get(TranslationKeys.AiSetupActiveAiNone),
		};

	private void OnAiSetupProviderSwitchConfirmCancelClicked(object? sender, RoutedEventArgs e)
	{
		AiSetupProviderSwitchConfirmPanel.IsVisible = false;
		_pendingProviderSwitchTargetId = null;
		_pendingProviderSwitchTargetLocalModelId = null;
	}

	private void OnAiSetupProviderSwitchConfirmOkClicked(object? sender, RoutedEventArgs e)
	{
		AiSetupProviderSwitchConfirmPanel.IsVisible = false;

		if (_pendingProviderSwitchTargetKind == AiBackendKind.None &&
			!string.IsNullOrWhiteSpace(_pendingProviderSwitchTargetId))
		{
			_aiProviderConfigService.ResetProviderConfig(_pendingProviderSwitchTargetId);
			ToggleProviderForm(_pendingProviderSwitchTargetId, false);
			RefreshAiProviderRowStates();
			_pendingProviderSwitchTargetId = null;
			return;
		}

		if (_pendingProviderSwitchTargetKind == AiBackendKind.Local &&
			!string.IsNullOrWhiteSpace(_pendingProviderSwitchTargetLocalModelId))
		{
			ActiveBackendService.TryActivateLocal(_pendingProviderSwitchTargetLocalModelId);
		}
		else if (_pendingProviderSwitchTargetKind == AiBackendKind.Online &&
				 !string.IsNullOrWhiteSpace(_pendingProviderSwitchTargetId))
		{
			_ = ActivateOnlineProviderAsync(_pendingProviderSwitchTargetId, skipUntestedWarning: true);
		}

		_pendingProviderSwitchTargetId = null;
		_pendingProviderSwitchTargetLocalModelId = null;
	}

	private void OnAiSetupProviderUntestedConfirmCancelClicked(object? sender, RoutedEventArgs e)
	{
		AiSetupProviderUntestedConfirmPanel.IsVisible = false;
		_pendingUntestedProviderId = null;
	}

	private async void OnAiSetupProviderUntestedConfirmOkClicked(object? sender, RoutedEventArgs e)
	{
		AiSetupProviderUntestedConfirmPanel.IsVisible = false;
		if (!string.IsNullOrWhiteSpace(_pendingUntestedProviderId))
		{
			await ActivateOnlineProviderAsync(_pendingUntestedProviderId, skipUntestedWarning: true).ConfigureAwait(true);
			_pendingUntestedProviderId = null;
		}
	}

	private void OnAiSetupActiveBackendActionClicked(object? sender, RoutedEventArgs e)
	{
		var snapshot = ActiveBackendService.GetActiveSnapshot();
		switch (snapshot.Kind)
		{
			case AiBackendKind.Local:
				AiSetupLocalModelsExpander.IsExpanded = true;
				break;
			case AiBackendKind.Online when snapshot.OnlineProviderId is not null:
				ToggleProviderForm(snapshot.OnlineProviderId, true);
				break;
			default:
				AiSetupOnlineProvidersExpander.IsExpanded = true;
				break;
		}
	}

	private void UpdateAiActiveBackendStrip()
	{
		var hasDownloadBanner = AiSetupDownloadJobBanner.IsVisible;
		AiSetupActiveBackendStrip.IsVisible = !hasDownloadBanner && AiSetupModalOverlay.IsVisible;

		var snapshot = ActiveBackendService.GetActiveSnapshot();
		AiSetupActiveBackendTextBlock.Text = snapshot.Kind switch
		{
			AiBackendKind.Local when snapshot.DisplayNameKey is not null =>
				_localizer.Format(TranslationKeys.AiSetupActiveAiLocal, _localizer.Get(snapshot.DisplayNameKey)),
			AiBackendKind.Online when snapshot.DisplayNameKey is not null =>
				_localizer.Format(
					TranslationKeys.AiSetupActiveAiOnline,
					_localizer.Get(snapshot.DisplayNameKey),
					snapshot.ModelLabel ?? string.Empty),
			_ => _localizer.Get(TranslationKeys.AiSetupActiveAiNone),
		};

		AiSetupActiveBackendActionButton.Content = snapshot.Kind switch
		{
			AiBackendKind.None => _localizer.Get(TranslationKeys.AiSetupActiveAiChange),
			AiBackendKind.Local => _localizer.Get(TranslationKeys.AiSetupActiveAiChange),
			AiBackendKind.Online => _localizer.Get(TranslationKeys.AiSetupActiveAiEdit),
			_ => _localizer.Get(TranslationKeys.AiSetupActiveAiChange),
		};
	}

	public void TryActivateLocalModel(string modelId, bool installed)
	{
		if (!installed)
		{
			return;
		}

		if (ActiveBackendService.RequiresSwitchConfirmation(AiBackendKind.Local, modelId))
		{
			ShowProviderSwitchConfirm(AiBackendKind.Local, null, modelId);
			return;
		}

		ActiveBackendService.TryActivateLocal(modelId);
	}

	public void DeactivateLocalModelIfActive(string modelId)
	{
		var snapshot = ActiveBackendService.GetActiveSnapshot();
		if (snapshot.Kind == AiBackendKind.Local &&
			string.Equals(snapshot.LocalModelId, modelId, StringComparison.Ordinal))
		{
			ActiveBackendService.Deactivate();
		}
	}

	public bool IsLocalModelActive(string modelId) =>
		ActiveBackendService.GetActiveSnapshot() is { Kind: AiBackendKind.Local, LocalModelId: var active } &&
		string.Equals(active, modelId, StringComparison.Ordinal);

	private void UpdateAiHeaderBackendBadges()
	{
		if (AiSetupModalOverlay.IsVisible)
		{
			AiOnlineActiveHeaderBadge.IsVisible = false;
			AiLocalActiveHeaderBadge.IsVisible = false;
			return;
		}

		var downloadActive = AiDownloadUiStateMapper.ShouldShowHeaderBadge(
			_aiDownloadCoordinator.CurrentSnapshot.State,
			false);
		if (downloadActive)
		{
			AiOnlineActiveHeaderBadge.IsVisible = false;
			AiLocalActiveHeaderBadge.IsVisible = false;
			return;
		}

		var snapshot = ActiveBackendService.GetActiveSnapshot();
		AiOnlineActiveHeaderBadge.IsVisible = snapshot.Kind == AiBackendKind.Online;
		AiLocalActiveHeaderBadge.IsVisible = snapshot.Kind == AiBackendKind.Local;

		if (snapshot.Kind == AiBackendKind.Online && snapshot.DisplayNameKey is not null)
		{
			ToolTip.SetTip(
				OpenAiSetupButton,
				_localizer.Format(TranslationKeys.AiSetupHeaderActiveOnline, _localizer.Get(snapshot.DisplayNameKey)));
		}
		else if (snapshot.Kind == AiBackendKind.Local && snapshot.DisplayNameKey is not null)
		{
			ToolTip.SetTip(
				OpenAiSetupButton,
				_localizer.Format(TranslationKeys.AiSetupHeaderActiveLocal, _localizer.Get(snapshot.DisplayNameKey)));
		}
		else if (!AiDownloadUiStateMapper.ShouldShowHeaderBadge(
					 _aiDownloadCoordinator.CurrentSnapshot.State,
					 AiSetupModalOverlay.IsVisible))
		{
			ToolTip.SetTip(OpenAiSetupButton, _localizer.Get(TranslationKeys.OpenAiSetup));
		}
	}

	private void ApplyAiProviderLocalization()
	{
		AiSetupLocalModelsSectionHeader.Text = _localizer.Get(TranslationKeys.AiSetupLocalModelsSection);
		AiSetupOnlineProvidersSectionHeader.Text = _localizer.Get(TranslationKeys.AiSetupOnlineProvidersSection);
		AiSetupOnlinePrivacyNoteTextBlock.Text = _localizer.Get(TranslationKeys.AiSetupOnlinePrivacyNote);
		AiSetupProviderUntestedConfirmOkButton.Content =
			_localizer.Get(TranslationKeys.AiSetupProviderActivateAnyway);
		AiSetupProviderSwitchConfirmCancelButton.Content = _localizer.Get(TranslationKeys.Cancel);
		AiSetupProviderSwitchConfirmOkButton.Content = _localizer.Get(TranslationKeys.Confirm);
		AiSetupProviderUntestedConfirmCancelButton.Content = _localizer.Get(TranslationKeys.Cancel);
		RenderAiProviderRows();
		UpdateAiActiveBackendStrip();
	}

	private sealed record AiProviderUiRow(
		string ProviderId,
		StackPanel Container,
		Border CardBorder,
		Button PrimaryButton,
		Button EditButton,
		TextBlock StatusText,
		TextBlock LastTestText,
		StackPanel FormPanel);

	private sealed record AiProviderFormState(
		Dictionary<string, Control> Fields,
		TextBlock FeedbackText,
		Button SaveButton,
		Button TestButton,
		Action RefreshButtons);
}
