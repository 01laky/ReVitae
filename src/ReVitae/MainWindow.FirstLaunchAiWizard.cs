using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.AppPreferences;
using ReVitae.Core.Localization;
using ReVitae.Core.Projects;
using ReVitae.Ui.Quality;
using System;
using System.Linq;

namespace ReVitae;

public partial class MainWindow
{
	private readonly AppPreferencesService _appPreferencesService = new();

	private enum FirstLaunchAiWizardStep
	{
		Welcome,
		ChoosePath,
		LocalSetup,
		OnlineSetup,
		Complete,
	}

	private enum FirstLaunchAiWizardCompleteKind
	{
		RemindLater,
		DeclinedOffline,
		ActiveLocal,
		ActiveOnline,
		DownloadInProgress,
	}

	private FirstLaunchAiWizardStep _firstLaunchAiWizardStep = FirstLaunchAiWizardStep.Welcome;
	private FirstLaunchAiWizardCompleteKind _firstLaunchAiWizardCompleteKind = FirstLaunchAiWizardCompleteKind.RemindLater;
	private bool _wizardSuspendedForSubModal;
	private bool _wizardReturnToWelcomeAfterSetup;
	private bool _wizardManualRerun;
	private bool _isUpdatingWizardLanguageSelection;

	private void InitializeFirstLaunchAiWizard()
	{
		FirstLaunchAiWizardLanguageComboBox.ItemsSource = AppLocalizer.SupportedLanguages;
		Opened += OnMainWindowOpenedForFirstLaunchAiWizard;
	}

	private void OnMainWindowOpenedForFirstLaunchAiWizard(object? sender, EventArgs e)
	{
		TryShowFirstLaunchAiWizardOnOpened();
	}

	internal void TryShowFirstLaunchAiWizardOnOpened()
	{
		if (_wizardManualRerun)
		{
			return;
		}

		if (CvProjectService.RecoveryExists())
		{
			return;
		}

		if (!_appPreferencesService.ShouldShowFirstLaunchAiWizard(_aiProviderConfigService.CurrentSettings))
		{
			ScheduleAiDownloadRecoveryWhenStartupUnblocked();
			return;
		}

		OpenFirstLaunchAiWizard(manualRerun: false);
	}

	private void OpenFirstLaunchAiWizard(bool manualRerun)
	{
		_wizardManualRerun = manualRerun;
		_wizardSuspendedForSubModal = false;
		_wizardReturnToWelcomeAfterSetup = false;
		_firstLaunchAiWizardStep = FirstLaunchAiWizardStep.Welcome;
		HideOtherContentModals(FirstLaunchAiWizardOverlay);
		SetIntroModalVisible(false);
		SetFirstLaunchAiWizardVisible(true);
	}

	private void SetFirstLaunchAiWizardVisible(bool isVisible)
	{
		if (!isVisible)
		{
			CancelAiDetectionOnly();
		}

		if (isVisible)
		{
			HideOtherContentModals(FirstLaunchAiWizardOverlay);
			ApplyFirstLaunchAiWizardLocalization();
			UpdateFirstLaunchAiWizardStepUi();
			FirstLaunchAiWizardPanel.Focus();
		}

		FirstLaunchAiWizardOverlay.IsVisible = isVisible;
		UpdateModalSizes();
		UpdateAiPromotionsUiVisibility();

		if (!isVisible && !_wizardSuspendedForSubModal)
		{
			_wizardManualRerun = false;
			if (!HasActiveCvSession() && !CvProjectService.RecoveryExists())
			{
				SetIntroModalVisible(true);
			}

			ScheduleAiDownloadRecoveryWhenStartupUnblocked();
		}
	}

	private bool HasActiveCvSession() =>
		!string.IsNullOrWhiteSpace(_projectFilePath) || HasCvFormData();

	private void FinalizeFirstLaunchAiWizardAndClose()
	{
		_wizardSuspendedForSubModal = false;
		SetFirstLaunchAiWizardVisible(false);
	}

	private void OnFirstLaunchAiWizardNextClicked(object? sender, RoutedEventArgs e)
	{
		if (_firstLaunchAiWizardStep == FirstLaunchAiWizardStep.Welcome)
		{
			NavigateFirstLaunchAiWizardTo(FirstLaunchAiWizardStep.ChoosePath);
		}
	}

	private void OnFirstLaunchAiWizardBackClicked(object? sender, RoutedEventArgs e)
	{
		switch (_firstLaunchAiWizardStep)
		{
			case FirstLaunchAiWizardStep.ChoosePath:
				NavigateFirstLaunchAiWizardTo(FirstLaunchAiWizardStep.Welcome);
				break;
			case FirstLaunchAiWizardStep.LocalSetup:
			case FirstLaunchAiWizardStep.OnlineSetup:
				NavigateFirstLaunchAiWizardTo(FirstLaunchAiWizardStep.ChoosePath);
				break;
			case FirstLaunchAiWizardStep.Complete:
				NavigateFirstLaunchAiWizardTo(FirstLaunchAiWizardStep.ChoosePath);
				break;
		}
	}

	private void OnFirstLaunchAiWizardSkipFooterClicked(object? sender, RoutedEventArgs e) =>
		ConfirmAndSkipFirstLaunchAiWizard();

	private void OnFirstLaunchAiWizardGetStartedClicked(object? sender, RoutedEventArgs e)
	{
		if (_firstLaunchAiWizardCompleteKind is FirstLaunchAiWizardCompleteKind.ActiveLocal
			or FirstLaunchAiWizardCompleteKind.ActiveOnline
			or FirstLaunchAiWizardCompleteKind.DownloadInProgress)
		{
			_appPreferencesService.MarkCompleted();
		}

		FinalizeFirstLaunchAiWizardAndClose();
	}

	private void OnFirstLaunchAiWizardPathLocalClicked(object? sender, RoutedEventArgs e)
	{
		NavigateFirstLaunchAiWizardTo(FirstLaunchAiWizardStep.LocalSetup);
		StartFirstLaunchAiWizardDetection();
	}

	private void OnFirstLaunchAiWizardPathOnlineClicked(object? sender, RoutedEventArgs e)
	{
		NavigateFirstLaunchAiWizardTo(FirstLaunchAiWizardStep.OnlineSetup);
		RenderWizardCuratedProviderRows();
	}

	private void OnFirstLaunchAiWizardPathSkipClicked(object? sender, RoutedEventArgs e)
	{
		_appPreferencesService.MarkRemindLater();
		ShowFirstLaunchAiWizardComplete(FirstLaunchAiWizardCompleteKind.RemindLater);
	}

	private void OnFirstLaunchAiWizardPathOfflineClicked(object? sender, RoutedEventArgs e)
	{
		_appPreferencesService.MarkDeclinedOffline();
		ShowFirstLaunchAiWizardComplete(FirstLaunchAiWizardCompleteKind.DeclinedOffline);
	}

	private void OnFirstLaunchAiWizardLocalRetryClicked(object? sender, RoutedEventArgs e) =>
		StartFirstLaunchAiWizardDetection();

	private void OnFirstLaunchAiWizardLocalDownloadClicked(object? sender, RoutedEventArgs e) =>
		OnAiSetupDownloadClicked(sender, e);

	private void OnFirstLaunchAiWizardMoreProvidersClicked(object? sender, RoutedEventArgs e)
	{
		_wizardSuspendedForSubModal = true;
		FirstLaunchAiWizardOverlay.IsVisible = false;
		SetAiSetupModalVisible(true);
		AiSetupOnlineProvidersExpander.IsExpanded = true;
		AiSetupLocalModelsExpander.IsExpanded = false;
	}

	private void OnFirstLaunchAiWizardChangeLanguageClicked(object? sender, RoutedEventArgs e)
	{
		_wizardSuspendedForSubModal = true;
		_wizardReturnToWelcomeAfterSetup = true;
		FirstLaunchAiWizardOverlay.IsVisible = false;
		SetSetupModalVisible(true);
	}

	private void OnFirstLaunchAiWizardLanguageSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (_isUpdatingWizardLanguageSelection ||
			FirstLaunchAiWizardLanguageComboBox.SelectedItem is not SupportedLanguage language)
		{
			return;
		}

		_localizer = new AppLocalizer(language.Code);
		ApplyLocalization();
	}

	private void OnSetupShowAiWizardAgainClicked(object? sender, RoutedEventArgs e)
	{
		SetSetupModalVisible(false);
		OpenFirstLaunchAiWizard(manualRerun: true);
	}

	private void ResumeFirstLaunchAiWizardAfterSubModal(FirstLaunchAiWizardStep step)
	{
		if (!_wizardSuspendedForSubModal)
		{
			return;
		}

		_wizardSuspendedForSubModal = false;
		_firstLaunchAiWizardStep = step;
		HideOtherContentModals(FirstLaunchAiWizardOverlay);
		SetIntroModalVisible(false);
		FirstLaunchAiWizardOverlay.IsVisible = true;
		ApplyFirstLaunchAiWizardLocalization();
		UpdateFirstLaunchAiWizardStepUi();
		if (step == FirstLaunchAiWizardStep.OnlineSetup)
		{
			RenderWizardCuratedProviderRows();
		}
		else if (step == FirstLaunchAiWizardStep.LocalSetup && _aiDetectionResult is null)
		{
			StartFirstLaunchAiWizardDetection();
		}
	}

	private void ConfirmAndSkipFirstLaunchAiWizard()
	{
		if (_firstLaunchAiWizardStep == FirstLaunchAiWizardStep.Complete)
		{
			return;
		}

		_appPreferencesService.MarkRemindLater();
		ShowFirstLaunchAiWizardComplete(FirstLaunchAiWizardCompleteKind.RemindLater);
	}

	private void NavigateFirstLaunchAiWizardTo(FirstLaunchAiWizardStep step)
	{
		_firstLaunchAiWizardStep = step;
		UpdateFirstLaunchAiWizardStepUi();
	}

	private void ShowFirstLaunchAiWizardComplete(FirstLaunchAiWizardCompleteKind kind)
	{
		_firstLaunchAiWizardCompleteKind = kind;
		NavigateFirstLaunchAiWizardTo(FirstLaunchAiWizardStep.Complete);
		UpdateFirstLaunchAiWizardCompleteSummary();
	}

	private void UpdateFirstLaunchAiWizardCompleteSummary()
	{
		var snapshot = ActiveBackendService.GetActiveSnapshot();
		var summary = _firstLaunchAiWizardCompleteKind switch
		{
			FirstLaunchAiWizardCompleteKind.ActiveLocal when snapshot.Kind == AiBackendKind.Local =>
				_localizer.Format(
					TranslationKeys.FirstLaunchAiWizardCompleteActiveLocal,
					_localizer.Get(snapshot.DisplayNameKey ?? TranslationKeys.AiSetupActiveAiLocal)),
			FirstLaunchAiWizardCompleteKind.ActiveOnline when snapshot.Kind == AiBackendKind.Online =>
				_localizer.Format(
					TranslationKeys.FirstLaunchAiWizardCompleteActiveOnline,
					$"{_localizer.Get(snapshot.DisplayNameKey ?? TranslationKeys.AiSetupActiveAiOnline)} · {snapshot.ModelLabel ?? string.Empty}"),
			FirstLaunchAiWizardCompleteKind.DownloadInProgress =>
				_localizer.Get(TranslationKeys.FirstLaunchAiWizardLocalDownloadInProgress),
			FirstLaunchAiWizardCompleteKind.DeclinedOffline =>
				_localizer.Get(TranslationKeys.FirstLaunchAiWizardCompleteOfflineOnly),
			_ => _localizer.Get(TranslationKeys.FirstLaunchAiWizardCompleteRemindLater),
		};

		FirstLaunchAiWizardCompleteSummaryTextBlock.Text = summary;
		FirstLaunchAiWizardCompleteHintTextBlock.Text =
			_localizer.Get(TranslationKeys.FirstLaunchAiWizardCompleteChangeAiHint);
	}

	private void UpdateFirstLaunchAiWizardStepUi()
	{
		var stepNumber = _firstLaunchAiWizardStep switch
		{
			FirstLaunchAiWizardStep.Welcome => 1,
			FirstLaunchAiWizardStep.ChoosePath => 2,
			FirstLaunchAiWizardStep.LocalSetup => 3,
			FirstLaunchAiWizardStep.OnlineSetup => 3,
			FirstLaunchAiWizardStep.Complete => 4,
			_ => 1,
		};

		FirstLaunchAiWizardStepIndicatorTextBlock.Text =
			_localizer.Format(TranslationKeys.FirstLaunchAiWizardStepIndicator, stepNumber, 4);

		FirstLaunchAiWizardWelcomePanel.IsVisible = _firstLaunchAiWizardStep == FirstLaunchAiWizardStep.Welcome;
		FirstLaunchAiWizardChoosePathPanel.IsVisible = _firstLaunchAiWizardStep == FirstLaunchAiWizardStep.ChoosePath;
		FirstLaunchAiWizardLocalPanel.IsVisible = _firstLaunchAiWizardStep == FirstLaunchAiWizardStep.LocalSetup;
		FirstLaunchAiWizardOnlinePanel.IsVisible = _firstLaunchAiWizardStep == FirstLaunchAiWizardStep.OnlineSetup;
		FirstLaunchAiWizardCompletePanel.IsVisible = _firstLaunchAiWizardStep == FirstLaunchAiWizardStep.Complete;

		FirstLaunchAiWizardBackButton.IsVisible = _firstLaunchAiWizardStep is not FirstLaunchAiWizardStep.Welcome;
		FirstLaunchAiWizardNextButton.IsVisible = _firstLaunchAiWizardStep == FirstLaunchAiWizardStep.Welcome;
		FirstLaunchAiWizardSkipFooterButton.IsVisible = _firstLaunchAiWizardStep != FirstLaunchAiWizardStep.Complete;
		FirstLaunchAiWizardGetStartedButton.IsVisible = _firstLaunchAiWizardStep == FirstLaunchAiWizardStep.Complete;
	}

	private void ApplyFirstLaunchAiWizardDetectionResult(AiSystemDetectionResult detectionResult)
	{
		_aiDetectionResult = detectionResult;
		_aiSelectedModelId = PickDefaultSelectedModelId(detectionResult);

		FirstLaunchAiWizardLocalDetectionProgressPanel.IsVisible = false;
		FirstLaunchAiWizardLocalDetectionFailedPanel.IsVisible = false;
		FirstLaunchAiWizardLocalContentPanel.IsVisible = true;

		FirstLaunchAiWizardLocalSystemDetailsPanel.Children.Clear();
		foreach (var line in AiSystemInfoFormatter.FormatDetailLines(
					 detectionResult.Profile,
					 detectionResult.Ollama,
					 _diskSpaceChecker.GetAvailableBytesForLocalData(),
					 _localizer))
		{
			FirstLaunchAiWizardLocalSystemDetailsPanel.Children.Add(new TextBlock
			{
				Text = line,
				Classes = { "re-vitae-secondary" },
				TextWrapping = TextWrapping.Wrap,
			});
		}

		var recommended = detectionResult.Models.FirstOrDefault(model => model.IsRecommended);
		if (recommended is null)
		{
			FirstLaunchAiWizardLocalRecommendedCard.IsVisible = false;
		}
		else
		{
			FirstLaunchAiWizardLocalRecommendedCard.IsVisible = true;
			FirstLaunchAiWizardLocalRecommendedLabelTextBlock.Text =
				_localizer.Get(TranslationKeys.FirstLaunchAiWizardLocalRecommended);
			FirstLaunchAiWizardLocalRecommendedNameTextBlock.Text =
				_localizer.Get(recommended.Model.DisplayNameKey);
			FirstLaunchAiWizardLocalRecommendedMetaTextBlock.Text = _localizer.Format(
				TranslationKeys.AiSetupModelMeta,
				AiFormatBytes.Format(recommended.Model.ApproxDownloadBytes),
				AiFormatBytes.Format(recommended.Model.MinimumMemoryBytes));
		}

		UpdateFirstLaunchAiWizardLocalDownloadButtonState();
	}

	private void ShowFirstLaunchAiWizardDetectionFailed()
	{
		FirstLaunchAiWizardLocalDetectionProgressPanel.IsVisible = false;
		FirstLaunchAiWizardLocalDetectionFailedPanel.IsVisible = true;
		FirstLaunchAiWizardLocalContentPanel.IsVisible = false;
	}

	private void UpdateFirstLaunchAiWizardLocalDownloadButtonState()
	{
		if (_firstLaunchAiWizardStep != FirstLaunchAiWizardStep.LocalSetup || _aiDetectionResult is null)
		{
			FirstLaunchAiWizardLocalDownloadButton.IsEnabled = false;
			return;
		}

		if (string.IsNullOrWhiteSpace(_aiSelectedModelId))
		{
			FirstLaunchAiWizardLocalDownloadButton.IsEnabled = false;
			return;
		}

		var selected = _aiDetectionResult.Models.FirstOrDefault(model =>
			string.Equals(model.Model.Id, _aiSelectedModelId, StringComparison.Ordinal));
		if (selected is null)
		{
			FirstLaunchAiWizardLocalDownloadButton.IsEnabled = false;
			return;
		}

		var isInstalled = IsAiModelInstalled(_aiDetectionResult, selected.Model.OllamaModelTag);
		FirstLaunchAiWizardLocalDownloadButton.IsEnabled =
			selected.IsDownloadAllowed && !isInstalled && !_aiDownloadCoordinator.HasActiveJob;

		if (isInstalled && IsLocalModelActive(selected.Model.Id))
		{
			ShowFirstLaunchAiWizardComplete(FirstLaunchAiWizardCompleteKind.ActiveLocal);
		}
	}

	internal void OnFirstLaunchAiWizardBackendActivated()
	{
		_appPreferencesService.ClearHideAiPromotionsOnBackendActivated();
		UpdateAiPromotionsUiVisibility();

		if (!FirstLaunchAiWizardOverlay.IsVisible && !_wizardSuspendedForSubModal)
		{
			return;
		}

		var snapshot = ActiveBackendService.GetActiveSnapshot();
		if (snapshot.Kind == AiBackendKind.Local)
		{
			ShowFirstLaunchAiWizardComplete(FirstLaunchAiWizardCompleteKind.ActiveLocal);
		}
		else if (snapshot.Kind == AiBackendKind.Online)
		{
			ShowFirstLaunchAiWizardComplete(FirstLaunchAiWizardCompleteKind.ActiveOnline);
		}
	}

	internal void OnFirstLaunchAiWizardDownloadStarted()
	{
		if (FirstLaunchAiWizardOverlay.IsVisible || _wizardSuspendedForSubModal)
		{
			ShowFirstLaunchAiWizardComplete(FirstLaunchAiWizardCompleteKind.DownloadInProgress);
		}
	}

	private void ApplyFirstLaunchAiWizardLocalization()
	{
		FirstLaunchAiWizardTitleTextBlock.Text = _localizer.Get(TranslationKeys.FirstLaunchAiWizardTitle);
		FirstLaunchAiWizardWelcomeLeadTextBlock.Text = _localizer.Get(TranslationKeys.FirstLaunchAiWizardWelcomeLead);
		FirstLaunchAiWizardWelcomeReviewTextBlock.Text = _localizer.Get(TranslationKeys.FirstLaunchAiWizardWelcomeReview);
		FirstLaunchAiWizardWelcomePrivacyTextBlock.Text = _localizer.Get(TranslationKeys.FirstLaunchAiWizardWelcomePrivacy);
		FirstLaunchAiWizardLanguageLabelTextBlock.Text = _localizer.Get(TranslationKeys.FirstLaunchAiWizardWelcomeLanguage);
		FirstLaunchAiWizardChangeLanguageButton.Content =
			_localizer.Get(TranslationKeys.FirstLaunchAiWizardWelcomeChangeLanguage);
		FirstLaunchAiWizardChoosePathTitleTextBlock.Text =
			_localizer.Get(TranslationKeys.FirstLaunchAiWizardChoosePathTitle);
		FirstLaunchAiWizardPathLocalTitleTextBlock.Text = _localizer.Get(TranslationKeys.FirstLaunchAiWizardPathLocalTitle);
		FirstLaunchAiWizardPathLocalSubtitleTextBlock.Text =
			_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathLocalSubtitle);
		FirstLaunchAiWizardPathOnlineTitleTextBlock.Text =
			_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathOnlineTitle);
		FirstLaunchAiWizardPathOnlineSubtitleTextBlock.Text =
			_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathOnlineSubtitle);
		FirstLaunchAiWizardPathSkipTitleTextBlock.Text = _localizer.Get(TranslationKeys.FirstLaunchAiWizardPathSkipTitle);
		FirstLaunchAiWizardPathSkipSubtitleTextBlock.Text =
			_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathSkipSubtitle);
		FirstLaunchAiWizardPathOfflineTitleTextBlock.Text =
			_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathOfflineTitle);
		FirstLaunchAiWizardPathOfflineSubtitleTextBlock.Text =
			_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathOfflineSubtitle);
		FirstLaunchAiWizardLocalDetectingTextBlock.Text = _localizer.Get(TranslationKeys.FirstLaunchAiWizardLocalDetecting);
		FirstLaunchAiWizardLocalDetectionFailedTextBlock.Text =
			_localizer.Get(TranslationKeys.FirstLaunchAiWizardLocalDetectionFailed);
		FirstLaunchAiWizardLocalRetryButton.Content = _localizer.Get(TranslationKeys.FirstLaunchAiWizardRetry);
		FirstLaunchAiWizardLocalDownloadButton.Content =
			_localizer.Get(TranslationKeys.FirstLaunchAiWizardLocalDownloadActivate);
		FirstLaunchAiWizardMoreProvidersButton.Content =
			_localizer.Get(TranslationKeys.FirstLaunchAiWizardOnlineMoreProviders);
		FirstLaunchAiWizardSkipFooterButton.Content = _localizer.Get(TranslationKeys.FirstLaunchAiWizardSkip);
		FirstLaunchAiWizardBackButton.Content = _localizer.Get(TranslationKeys.FirstLaunchAiWizardBack);
		FirstLaunchAiWizardNextButton.Content = _localizer.Get(TranslationKeys.FirstLaunchAiWizardNext);
		FirstLaunchAiWizardGetStartedButton.Content = _localizer.Get(TranslationKeys.FirstLaunchAiWizardGetStarted);
		SetupShowAiWizardAgainButton.Content = _localizer.Get(TranslationKeys.SetupShowAiWizardAgain);

		_isUpdatingWizardLanguageSelection = true;
		FirstLaunchAiWizardLanguageComboBox.SelectedItem = AppLocalizer.SupportedLanguages
			.FirstOrDefault(language => language.Code.Equals(_localizer.LanguageCode, StringComparison.OrdinalIgnoreCase))
			?? AppLocalizer.SupportedLanguages.First(language => language.Code == "en");
		_isUpdatingWizardLanguageSelection = false;

		AutomationProperties.SetName(FirstLaunchAiWizardPathLocalButton,
			$"{_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathLocalTitle)}. {_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathLocalSubtitle)}");
		AutomationProperties.SetName(FirstLaunchAiWizardPathOnlineButton,
			$"{_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathOnlineTitle)}. {_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathOnlineSubtitle)}");
		AutomationProperties.SetName(FirstLaunchAiWizardPathSkipButton,
			$"{_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathSkipTitle)}. {_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathSkipSubtitle)}");
		AutomationProperties.SetName(FirstLaunchAiWizardPathOfflineButton,
			$"{_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathOfflineTitle)}. {_localizer.Get(TranslationKeys.FirstLaunchAiWizardPathOfflineSubtitle)}");

		if (_firstLaunchAiWizardStep == FirstLaunchAiWizardStep.Complete)
		{
			UpdateFirstLaunchAiWizardCompleteSummary();
		}

		UpdateFirstLaunchAiWizardStepUi();
	}

	private bool ShouldShowAiPromotionsInUi() =>
		_appPreferencesService.ShouldShowAiPromotionsInUi(_aiProviderConfigService.CurrentSettings);

	private void UpdateAiPromotionsUiVisibility()
	{
		var show = ShouldShowAiPromotionsInUi();
		OpenAiSetupButton.Opacity = show ? 1.0 : 0.55;

		if (!show)
		{
			IntroImportTryAiButton.IsVisible = false;
			ReplaceImportTryAiButton.IsVisible = false;
			ImportAiEnhancePanel.IsVisible = false;
		}

		if (QualityHintFlyoutHelper.AiOptions is not null)
		{
			QualityHintFlyoutHelper.AiOptions.ShouldShowAiPromotions = ShouldShowAiPromotionsInUi;
		}
	}
}
