using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Ui;
using ReVitae.Ui.Quality;

namespace ReVitae;

public partial class MainWindow
{
	private CancellationTokenSource? _aiAdvisorCts;
	private bool _aiAdvisorInFlight;
	private CvImportSectionId? _currentAdvisorSection;

	private static readonly (string Name, CvImportSectionId Section)[] AdvisorSectionBindings =
	[
		("WorkExperienceSection", CvImportSectionId.WorkExperience),
		("EducationSection", CvImportSectionId.Education),
		("SkillsSection", CvImportSectionId.Skills),
		("LanguagesSection", CvImportSectionId.Languages),
		("ProjectsSection", CvImportSectionId.Projects),
	];

	private void InitializeAiAdvisor()
	{
		foreach (var (name, section) in AdvisorSectionBindings)
		{
			if (this.FindControl<Control>(name) is IAiAdvisorSection advisorSection)
			{
				var captured = section;
				advisorSection.ConfigureAdvisor(
					() => _ = OpenAdvisorForSectionAsync(captured),
					_localizer.Get(TranslationKeys.AiCvAskForTips));
			}
		}

		RefreshAdvisorVisibility();
	}

	/// <summary>Shows the advisor button only when an AI backend is active (045 A.2).</summary>
	private void RefreshAdvisorVisibility()
	{
		var active = ActiveBackendService.GetActiveSnapshot().Kind != AiBackendKind.None;
		foreach (var (name, _) in AdvisorSectionBindings)
		{
			if (this.FindControl<Control>(name) is IAiAdvisorSection advisorSection)
			{
				advisorSection.SetAdvisorVisible(active);
			}
		}
	}

	private async Task OpenAdvisorForSectionAsync(CvImportSectionId section, bool forceRefresh = false)
	{
		if (_aiAdvisorInFlight)
		{
			return;
		}

		_currentAdvisorSection = section;
		AiAdvisorModalOverlay.IsVisible = true;
		AiAdvisorTitleTextBlock.Text = _localizer.Format(
			TranslationKeys.AiCvAdvisorTitle,
			SectionDisplayName(section));
		ApplyAdvisorModalLocalization();

		// Online privacy confirm — reuse the shared session flag (045 / 039).
		var backend = ActiveBackendService.GetActiveSnapshot();
		if (backend.Kind == AiBackendKind.Online && !_onlineCvSendConfirmedThisSession)
		{
			ShowAdvisorOnlineConfirm(section, backend);
			return;
		}

		await RunAdvisorAsync(section, forceRefresh).ConfigureAwait(true);
	}

	private async Task RunAdvisorAsync(CvImportSectionId section, bool forceRefresh)
	{
		_aiAdvisorInFlight = true;
		_aiAdvisorCts?.Cancel();
		_aiAdvisorCts?.Dispose();
		_aiAdvisorCts = new CancellationTokenSource();

		ShowAdvisorLoadingState();

		try
		{
			var snapshot = BuildExportSourceData();
			var result = await AiCvCompletion.AdviseSectionAsync(
				snapshot,
				section,
				_localizer.LanguageCode,
				_aiCvTargetContext,
				forceRefresh,
				_aiAdvisorCts.Token).ConfigureAwait(true);

			if (result.Cancelled)
			{
				return;
			}

			if (result.Succeeded && result.Suggestions.Count > 0)
			{
				RenderAdvisorSuggestions(result);
				return;
			}

			ShowAdvisorError(result.ErrorMessageKey ?? TranslationKeys.AiCvAdvisorEmpty, result.BackendUsed?.Label);
		}
		finally
		{
			_aiAdvisorInFlight = false;
		}
	}

	private void ShowAdvisorLoadingState()
	{
		AiAdvisorLoadingPanel.IsVisible = true;
		AiAdvisorSuggestionsPanel.Children.Clear();
		AiAdvisorErrorTextBlock.IsVisible = false;
		AiAdvisorBackendTextBlock.Text = string.Empty;
	}

	private void RenderAdvisorSuggestions(AiCvAdvisorResult result)
	{
		AiAdvisorLoadingPanel.IsVisible = false;
		AiAdvisorErrorTextBlock.IsVisible = false;
		AiAdvisorSuggestionsPanel.Children.Clear();

		var index = 1;
		foreach (var suggestion in result.Suggestions)
		{
			AiAdvisorSuggestionsPanel.Children.Add(BuildSuggestionRow(index++, suggestion));
		}

		var backendLabel = result.BackendUsed?.Label ?? string.Empty;
		if (result.FromCache)
		{
			var cached = _localizer.Get(TranslationKeys.AiCvAdvisorCached);
			backendLabel = string.IsNullOrWhiteSpace(backendLabel) ? cached : $"{backendLabel} · {cached}";
		}

		AiAdvisorBackendTextBlock.Text = backendLabel;
	}

	private Control BuildSuggestionRow(int index, AiCvAdvisorSuggestion suggestion)
	{
		var textStack = new StackPanel { Spacing = 4 };
		textStack.Children.Add(new TextBlock
		{
			Text = $"{index}. {suggestion.Text}",
			TextWrapping = TextWrapping.Wrap,
			FontSize = 15,
		});

		if (!string.IsNullOrWhiteSpace(suggestion.Rationale))
		{
			var why = new TextBlock
			{
				Text = $"{_localizer.Get(TranslationKeys.AiCvAdvisorRationalePrefix)} {suggestion.Rationale}",
				TextWrapping = TextWrapping.Wrap,
				FontSize = 13,
			};
			why.Classes.Add(UiClasses.SecondaryText);
			textStack.Children.Add(why);
		}

		var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
		row.Children.Add(textStack);

		if (suggestion.ApplyTarget is not null && !string.IsNullOrWhiteSpace(suggestion.ApplyValue))
		{
			var applyButton = new Button
			{
				Content = _localizer.Get(TranslationKeys.AiCvAdvisorApply),
				VerticalAlignment = VerticalAlignment.Top,
			};
			applyButton.Classes.Add(UiClasses.SecondaryButton);
			Grid.SetColumn(applyButton, 1);
			var target = suggestion.ApplyTarget;
			var value = suggestion.ApplyValue!;
			applyButton.Click += (_, _) => ApplyAdvisorSuggestion(target, value);
			row.Children.Add(applyButton);
		}

		return row;
	}

	private void ApplyAdvisorSuggestion(AiCvFieldTarget target, string value)
	{
		// Advisor suggestions are advice-only in v1 (no concrete ApplyTarget), so this path
		// is reserved for future apply-capable tips; prior value capture is best-effort.
		ApplyAiSuggestionToField(target, value);
		_aiCvUndoBuffer.CaptureSingle(target, string.Empty);
		ShowAiCvUndoBar();
		UpdateValidationState();
		UpdateQualityHints();
	}

	private void ShowAdvisorError(string errorKey, string? backendLabel)
	{
		AiAdvisorLoadingPanel.IsVisible = false;
		AiAdvisorSuggestionsPanel.Children.Clear();
		AiAdvisorErrorTextBlock.IsVisible = true;
		AiAdvisorErrorTextBlock.Text = _localizer.Get(errorKey);
		AiAdvisorBackendTextBlock.Text = backendLabel ?? string.Empty;
	}

	private void ShowAdvisorOnlineConfirm(CvImportSectionId section, ActiveAiBackendSnapshot backend)
	{
		AiAdvisorLoadingPanel.IsVisible = false;
		AiAdvisorSuggestionsPanel.Children.Clear();
		AiAdvisorErrorTextBlock.IsVisible = false;

		var providerName = backend.DisplayNameKey is not null
			? _localizer.Get(backend.DisplayNameKey)
			: _localizer.Get(TranslationKeys.AiSetupActiveAiNone);

		var prompt = new TextBlock
		{
			Text = _localizer.Format(TranslationKeys.AiCvOnlineSendConfirm, providerName),
			TextWrapping = TextWrapping.Wrap,
		};
		var actions = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 12,
			HorizontalAlignment = HorizontalAlignment.Right,
		};
		var cancel = new Button { Content = _localizer.Get(TranslationKeys.AiCvSuggestionCancel) };
		cancel.Classes.Add(UiClasses.SecondaryButton);
		cancel.Click += (_, _) => SetAiAdvisorModalVisible(false);
		var ok = new Button { Content = _localizer.Get(TranslationKeys.Confirm) };
		ok.Classes.Add(UiClasses.PrimaryButton);
		ok.Click += (_, _) =>
		{
			_onlineCvSendConfirmedThisSession = true;
			_ = RunAdvisorAsync(section, forceRefresh: false);
		};
		actions.Children.Add(cancel);
		actions.Children.Add(ok);

		AiAdvisorSuggestionsPanel.Children.Add(new StackPanel { Spacing = 12, Children = { prompt, actions } });
	}

	private void ApplyAdvisorModalLocalization()
	{
		AiAdvisorRefreshButton.Content = _localizer.Get(TranslationKeys.AiCvAdvisorRefresh);
		AiAdvisorCloseButton.Content = _localizer.Get(TranslationKeys.AiCvAdvisorClose);
		AiAdvisorTargetRoleTextBox.PlaceholderText = _localizer.Get(TranslationKeys.AiCvTargetRoleLabel);
		AiAdvisorTargetJobDescTextBox.PlaceholderText = _localizer.Get(TranslationKeys.AiCvTargetJobDescLabel);
	}

	private void SetAiAdvisorModalVisible(bool visible)
	{
		AiAdvisorModalOverlay.IsVisible = visible;
		if (!visible)
		{
			_aiAdvisorCts?.Cancel();
			_currentAdvisorSection = null;
		}
	}

	private void OnCloseAiAdvisorModalClicked(object? sender, RoutedEventArgs e) =>
		SetAiAdvisorModalVisible(false);

	private async void OnAiAdvisorRefreshClicked(object? sender, RoutedEventArgs e)
	{
		if (_currentAdvisorSection is { } section)
		{
			await RunAdvisorAsync(section, forceRefresh: true).ConfigureAwait(true);
		}
	}

	private void OnAiAdvisorTargetContextChanged(object? sender, TextChangedEventArgs e) =>
		SetAiCvTargetContext(AiAdvisorTargetRoleTextBox.Text, AiAdvisorTargetJobDescTextBox.Text);

	// 045 C.6 — undo bar wiring.
	private void ShowAiCvUndoBar()
	{
		if (!_aiCvUndoBuffer.CanUndo)
		{
			return;
		}

		AiCvUndoTextBlock.Text = _localizer.Get(TranslationKeys.AiCvSuggestionAccept);
		AiCvUndoButton.Content = _localizer.Get(TranslationKeys.AiCvApplyUndo);
		AiCvUndoBar.IsVisible = true;
	}

	private void OnAiCvUndoClicked(object? sender, RoutedEventArgs e)
	{
		foreach (var snapshot in _aiCvUndoBuffer.Restore())
		{
			ApplyAiSuggestionToField(snapshot.Target, snapshot.PriorValue);
		}

		AiCvUndoBar.IsVisible = false;
		UpdateValidationState();
		UpdateQualityHints();
	}

	private string SectionDisplayName(CvImportSectionId section) =>
		section switch
		{
			CvImportSectionId.WorkExperience => _localizer.Get(TranslationKeys.WorkExperience),
			CvImportSectionId.Education => _localizer.Get(TranslationKeys.Education),
			CvImportSectionId.Skills => _localizer.Get(TranslationKeys.Skills),
			CvImportSectionId.Languages => _localizer.Get(TranslationKeys.Languages),
			CvImportSectionId.Projects => _localizer.Get(TranslationKeys.Projects),
			_ => section.ToString(),
		};
}
