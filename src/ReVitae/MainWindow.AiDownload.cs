using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Download;
using ReVitae.Core.Ai.Ollama;
using ReVitae.Core.Localization;
using System;
using System.Threading.Tasks;

namespace ReVitae;

public partial class MainWindow
{
    private readonly AiModelDownloadCoordinator _aiDownloadCoordinator = new();
    private readonly AiDownloadDisplayProgress _aiDownloadDisplayProgress = new();
    private AiDownloadJobState _lastRenderedDownloadState = AiDownloadJobState.Idle;
    private long _lastProgressUiRefreshMs;
    private bool _progressUiRefreshScheduled;

    private void InitializeAiDownload()
    {
        _aiDownloadCoordinator.SnapshotChanged += OnAiDownloadSnapshotChanged;
        Opened += OnMainWindowOpenedForAiDownload;
    }

    private async void OnMainWindowOpenedForAiDownload(object? sender, EventArgs e)
    {
        if (IntroModalOverlay.IsVisible)
        {
            return;
        }

        await RunAiDownloadRecoveryAsync().ConfigureAwait(true);
    }

    private void ScheduleAiDownloadRecoveryWhenIntroDismissed()
    {
        if (IntroModalOverlay.IsVisible)
        {
            return;
        }

        _ = RunAiDownloadRecoveryAsync();
    }

    private async Task RunAiDownloadRecoveryAsync()
    {
        await _aiDownloadCoordinator.TryRecoverOnStartupAsync().ConfigureAwait(true);
        UpdateAiDownloadUi();
    }

    private void OnAiDownloadSnapshotChanged(AiDownloadJobSnapshot snapshot)
    {
        if (snapshot.State != _lastRenderedDownloadState)
        {
            _lastRenderedDownloadState = snapshot.State;
            if (snapshot.State == AiDownloadJobState.Idle)
            {
                _aiDownloadDisplayProgress.Reset();
                if (AiSetupModalOverlay.IsVisible)
                {
                    StartAiSetupDetection();
                }
            }
            else if (snapshot.State is AiDownloadJobState.Completed
                     or AiDownloadJobState.Failed
                     or AiDownloadJobState.Paused
                     or AiDownloadJobState.Downloading)
            {
                RefreshAiSetupModelCardsIfVisible();
            }

            Dispatcher.UIThread.Post(UpdateAiDownloadUi);
            return;
        }

        ScheduleThrottledAiDownloadUi();
    }

    private void ScheduleThrottledAiDownloadUi()
    {
        var now = Environment.TickCount64;
        if (now - _lastProgressUiRefreshMs >= 150)
        {
            _lastProgressUiRefreshMs = now;
            Dispatcher.UIThread.Post(UpdateAiDownloadUi);
            return;
        }

        if (_progressUiRefreshScheduled)
        {
            return;
        }

        _progressUiRefreshScheduled = true;
        var delayMs = (int)Math.Max(1, 150 - (now - _lastProgressUiRefreshMs));
        Dispatcher.UIThread.Post(() => _ = RunScheduledProgressUiRefreshAsync(delayMs));
    }

    private async Task RunScheduledProgressUiRefreshAsync(int delayMs)
    {
        try
        {
            await Task.Delay(delayMs).ConfigureAwait(true);
            _lastProgressUiRefreshMs = Environment.TickCount64;
            UpdateAiDownloadUi();
        }
        finally
        {
            _progressUiRefreshScheduled = false;
        }
    }

    private void OnAiDownloadDockClicked(object? sender, PointerReleasedEventArgs e)
    {
        if (IsBlockingOverlayOpen())
        {
            return;
        }

        SetAiSetupModalVisible(true);
    }

    private async void OnAiDownloadPauseClicked(object? sender, RoutedEventArgs e)
    {
        HideAiDownloadStopConfirmPanels();
        await _aiDownloadCoordinator.PauseAsync().ConfigureAwait(true);
        UpdateAiDownloadUi();
    }

    private async void OnAiDownloadResumeClicked(object? sender, RoutedEventArgs e)
    {
        HideAiDownloadStopConfirmPanels();
        await _aiDownloadCoordinator.ResumeAsync().ConfigureAwait(true);
        UpdateAiSetupDownloadButtonState();
    }

    private void OnAiDownloadStopClicked(object? sender, RoutedEventArgs e)
    {
        var modelName = GetAiDownloadModelDisplayName(_aiDownloadCoordinator.CurrentSnapshot);
        var message = _localizer.Format(TranslationKeys.AiDownloadStopConfirm, modelName);

        if (AiSetupModalOverlay.IsVisible)
        {
            AiSetupDownloadStopConfirmTextBlock.Text = message;
            AiSetupDownloadStopConfirmPanel.IsVisible = true;
            AiSetupDownloadBannerActionsPanel.IsVisible = false;
            AiDownloadDockStopConfirmPanel.IsVisible = false;
        }
        else
        {
            AiDownloadDockStopConfirmTextBlock.Text = message;
            AiDownloadDockStopConfirmPanel.IsVisible = true;
            AiDownloadDockActionsPanel.IsVisible = false;
            AiSetupDownloadStopConfirmPanel.IsVisible = false;
        }
    }

    private void OnAiDownloadStopConfirmCancelClicked(object? sender, RoutedEventArgs e)
    {
        HideAiDownloadStopConfirmPanels();
    }

    private async void OnAiDownloadStopConfirmOkClicked(object? sender, RoutedEventArgs e)
    {
        HideAiDownloadStopConfirmPanels();
        await _aiDownloadCoordinator.StopAsync().ConfigureAwait(true);

        if (_aiDetectionResult is null)
        {
            StartAiSetupDetection();
        }

        UpdateAiSetupDownloadButtonState();
        UpdateAiDownloadUi();
    }

    private void HideAiDownloadStopConfirmPanels()
    {
        AiSetupDownloadStopConfirmPanel.IsVisible = false;
        AiDownloadDockStopConfirmPanel.IsVisible = false;
    }

    private void OnAiSetupDownloadRefreshSystemClicked(object? sender, RoutedEventArgs e)
    {
        StartAiSetupDetection();
    }

    private void EnterAiSetupDownloadMode()
    {
        AiSetupDetectionProgressPanel.IsVisible = false;
        AiSetupDetectionFailedPanel.IsVisible = false;
        AiSetupContentScrollViewer.IsVisible = true;
        AiSetupDownloadConfirmPanel.IsVisible = false;
        AiSetupPullProgressBar.IsVisible = false;
        AiSetupStatusTextBlock.IsVisible = false;
    }

    private void UpdateAiDownloadUi()
    {
        var snapshot = _aiDownloadCoordinator.CurrentSnapshot;
        var hasActiveJob = AiDownloadUiStateMapper.HasActiveJob(snapshot.State);
        var showDock = AiDownloadUiStateMapper.ShouldShowDock(
            snapshot.State,
            IntroModalOverlay.IsVisible,
            AiSetupModalOverlay.IsVisible);
        var showBadge = AiDownloadUiStateMapper.ShouldShowHeaderBadge(
            snapshot.State,
            AiSetupModalOverlay.IsVisible);

        if (AiSetupModalOverlay.IsVisible && hasActiveJob)
        {
            EnterAiSetupDownloadMode();
            ApplyAiDownloadBannerUi(snapshot);
        }
        else if (!hasActiveJob)
        {
            AiSetupDownloadJobBanner.IsVisible = false;
        }

        AiDownloadDockOverlay.IsVisible = showDock;
        AiDownloadHeaderBadge.IsVisible = showBadge;

        if (showBadge)
        {
            AutomationProperties.SetName(
                OpenAiSetupButton,
                _localizer.Get(TranslationKeys.AiDownloadHeaderInProgress));
        }
        else
        {
            AutomationProperties.SetName(
                OpenAiSetupButton,
                _localizer.Get(TranslationKeys.OpenAiSetup));
        }

        if (showDock)
        {
            ApplyAiDownloadProgressUi(
                snapshot,
                AiDownloadDockModelTextBlock,
                AiDownloadDockProgressBar,
                AiDownloadDockPercentTextBlock,
                AiDownloadDockStatusTextBlock);

            ApplyAiDownloadActionButtons(snapshot);

            if (!AiDownloadDockStopConfirmPanel.IsVisible)
            {
                AiDownloadDockActionsPanel.IsVisible = snapshot.State != AiDownloadJobState.Completed;
            }

            ToolTip.SetTip(AiDownloadDockOverlay, _localizer.Get(TranslationKeys.AiDownloadDockTooltip));

            var dockDisplay = _aiDownloadDisplayProgress.Update(snapshot);
            var dockAutomationName = dockDisplay.Percent is int percent
                ? $"{GetAiDownloadModelDisplayName(snapshot)} {percent}%"
                : GetAiDownloadModelDisplayName(snapshot);
            AutomationProperties.SetName(AiDownloadDockOverlay, dockAutomationName);
        }
        else
        {
            AiDownloadDockActionsPanel.IsVisible = false;
        }

        if (AiSetupModalOverlay.IsVisible)
        {
            UpdateAiSetupDownloadButtonState();
            UpdateAiActiveBackendStrip();
        }

        UpdateAiHeaderBackendBadges();
    }

    private void ApplyAiDownloadBannerUi(AiDownloadJobSnapshot snapshot)
    {
        AiSetupDownloadJobBanner.IsVisible = true;
        AiSetupDownloadRefreshSystemButton.IsVisible = _aiDetectionResult is null;

        var modelName = GetAiDownloadModelDisplayName(snapshot);
        AiSetupDownloadBannerTitleTextBlock.Text = snapshot.State switch
        {
            AiDownloadJobState.Paused => _localizer.Format(TranslationKeys.AiDownloadBannerPaused, modelName),
            AiDownloadJobState.Completed => _localizer.Get(TranslationKeys.AiDownloadCompleted),
            AiDownloadJobState.Interrupted => _localizer.Get(TranslationKeys.AiDownloadResumeOnStartup),
            _ => _localizer.Format(TranslationKeys.AiDownloadBannerTitle, modelName),
        };

        ApplyAiDownloadProgressUi(
            snapshot,
            titleTextBlock: null,
            AiSetupDownloadBannerProgressBar,
            AiSetupDownloadBannerPercentTextBlock,
            AiSetupDownloadBannerStatusTextBlock);

        var showError = snapshot.State == AiDownloadJobState.Failed &&
                        !string.IsNullOrWhiteSpace(snapshot.ErrorMessageKey);
        AiSetupDownloadBannerErrorTextBlock.IsVisible = showError;
        AiSetupDownloadBannerErrorTextBlock.Text = showError
            ? GetAiDownloadErrorMessage(snapshot.ErrorMessageKey!)
            : string.Empty;

        ApplyAiDownloadActionButtons(snapshot);

        if (!AiSetupDownloadStopConfirmPanel.IsVisible)
        {
            AiSetupDownloadBannerActionsPanel.IsVisible = snapshot.State != AiDownloadJobState.Completed;
        }
    }

    private void ApplyAiDownloadActionButtons(AiDownloadJobSnapshot snapshot)
    {
        var isDownloading = snapshot.State is AiDownloadJobState.Downloading
            or AiDownloadJobState.Interrupted;
        var canResume = snapshot.State is AiDownloadJobState.Paused
            or AiDownloadJobState.Interrupted
            or AiDownloadJobState.Failed;
        var isCompleted = snapshot.State == AiDownloadJobState.Completed;
        var showActions = !isCompleted;

        AiSetupDownloadPauseButton.IsVisible = isDownloading;
        AiSetupDownloadResumeButton.IsVisible = canResume;
        AiSetupDownloadStopButton.IsVisible = showActions;
        if (!AiSetupDownloadStopConfirmPanel.IsVisible)
        {
            AiSetupDownloadBannerActionsPanel.IsVisible = showActions;
        }

        if (!AiDownloadDockStopConfirmPanel.IsVisible)
        {
            AiDownloadDockActionsPanel.IsVisible = showActions;
        }
        AiDownloadDockPauseButton.IsVisible = isDownloading;
        AiDownloadDockResumeButton.IsVisible = canResume;
        AiDownloadDockStopButton.IsVisible = showActions;
    }

    private void ApplyAiDownloadProgressUi(
        AiDownloadJobSnapshot snapshot,
        TextBlock? titleTextBlock,
        ProgressBar progressBar,
        TextBlock percentTextBlock,
        TextBlock statusTextBlock)
    {
        if (titleTextBlock is not null)
        {
            titleTextBlock.Text = snapshot.State == AiDownloadJobState.Completed
                ? _localizer.Get(TranslationKeys.AiDownloadCompleted)
                : GetAiDownloadModelDisplayName(snapshot);
        }

        if (snapshot.State == AiDownloadJobState.Completed)
        {
            progressBar.IsIndeterminate = false;
            progressBar.Maximum = 100;
            progressBar.Value = 100;
            percentTextBlock.Text = _localizer.Format(TranslationKeys.AiDownloadPercent, 100);
            statusTextBlock.Text = _localizer.Get(TranslationKeys.AiDownloadCompleted);
            AiDownloadDockOverlay.BorderBrush = new SolidColorBrush(Color.Parse("#2563EB"));
            return;
        }

        AiDownloadDockOverlay.ClearValue(Border.BorderBrushProperty);

        var display = _aiDownloadDisplayProgress.Update(snapshot);
        if (display is { IsIndeterminate: false, Percent: int percent })
        {
            progressBar.IsIndeterminate = false;
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = percent;
            percentTextBlock.Text = _localizer.Format(TranslationKeys.AiDownloadPercent, percent);
        }
        else
        {
            progressBar.IsIndeterminate = true;
            percentTextBlock.Text = _localizer.Get(TranslationKeys.AiDownloadPercentUnknown);
        }

        statusTextBlock.Text = snapshot.State switch
        {
            AiDownloadJobState.Paused => _localizer.Format(TranslationKeys.AiDownloadBannerPaused, GetAiDownloadModelDisplayName(snapshot)),
            AiDownloadJobState.Interrupted => _localizer.Get(TranslationKeys.AiDownloadResumeOnStartup),
            AiDownloadJobState.Failed => _localizer.Get(TranslationKeys.AiDownloadFailed),
            _ => ResolveAiDownloadStatusText(snapshot),
        };
    }

    private string ResolveAiDownloadStatusText(AiDownloadJobSnapshot snapshot)
    {
        if (AiDownloadStatus.TryGetTranslationKey(snapshot.StatusText, out var statusKey))
        {
            return _localizer.Get(statusKey);
        }

        return string.IsNullOrWhiteSpace(snapshot.StatusText)
            ? _localizer.Get(TranslationKeys.AiDownloadPreparingEngine)
            : snapshot.StatusText!;
    }

    private string GetAiDownloadModelDisplayName(AiDownloadJobSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.DisplayNameKey))
        {
            return snapshot.OllamaModelTag;
        }

        return _localizer.Get(snapshot.DisplayNameKey);
    }

    private string GetAiDownloadErrorMessage(string errorMessageKey) =>
        errorMessageKey == TranslationKeys.AiSetupOllamaNotRunning
            ? _localizer.Format(errorMessageKey, OllamaHost.DisplayAddress)
            : _localizer.Get(errorMessageKey);

    private void ApplyAiDownloadLocalization()
    {
        ToolTip.SetTip(AiDownloadDockOverlay, _localizer.Get(TranslationKeys.AiDownloadDockTooltip));
        AiSetupDownloadPauseButton.Content = _localizer.Get(TranslationKeys.AiDownloadPause);
        AiSetupDownloadResumeButton.Content = _localizer.Get(TranslationKeys.AiDownloadResume);
        AiSetupDownloadStopButton.Content = _localizer.Get(TranslationKeys.AiDownloadStop);
        AiDownloadDockPauseButton.Content = _localizer.Get(TranslationKeys.AiDownloadPause);
        AiDownloadDockResumeButton.Content = _localizer.Get(TranslationKeys.AiDownloadResume);
        AiDownloadDockStopButton.Content = _localizer.Get(TranslationKeys.AiDownloadStop);
        AiSetupDownloadStopConfirmCancelButton.Content = _localizer.Get(TranslationKeys.Cancel);
        AiSetupDownloadStopConfirmOkButton.Content = _localizer.Get(TranslationKeys.Confirm);
        AiDownloadDockStopConfirmCancelButton.Content = _localizer.Get(TranslationKeys.Cancel);
        AiDownloadDockStopConfirmOkButton.Content = _localizer.Get(TranslationKeys.Confirm);
        AiSetupDownloadRefreshSystemButton.Content = _localizer.Get(TranslationKeys.AiDownloadRefreshSystem);
        UpdateAiDownloadUi();
    }
}
