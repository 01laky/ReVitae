using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Ollama;
using ReVitae.Core.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReVitae;

public partial class MainWindow
{
    private readonly ISystemProfileDetector _systemProfileDetector = new SystemProfileDetector();
    private readonly IOllamaRuntimeProbe _ollamaRuntimeProbe = new OllamaRuntimeProbe();
    private readonly IDiskSpaceChecker _diskSpaceChecker = new DiskSpaceChecker();
    private readonly OllamaPullClient _ollamaPullClient = new();
    private readonly AiSettingsStorage _aiSettingsStorage = new();

    private CancellationTokenSource? _aiDetectionCts;
    private CancellationTokenSource? _aiPullCts;
    private AiSystemDetectionResult? _aiDetectionResult;
    private string? _aiSelectedModelId;
    private AiModelRecommendation? _aiPendingDownloadRecommendation;
    private readonly Dictionary<string, Border> _aiModelCards = new(StringComparer.Ordinal);

    private void OnOpenAiSetupClicked(object? sender, RoutedEventArgs e)
    {
        if (IsBlockingOverlayOpen())
        {
            return;
        }

        SetAiSetupModalVisible(true);
    }

    private void OnCloseAiSetupClicked(object? sender, RoutedEventArgs e)
    {
        SetAiSetupModalVisible(false);
    }

    private void SetAiSetupModalVisible(bool isVisible)
    {
        if (!isVisible)
        {
            CancelAiSetupOperations();
        }

        if (isVisible)
        {
            HideOtherContentModals(AiSetupModalOverlay);
            ResetAiSetupModalUi();
            StartAiSetupDetection();
        }

        AiSetupModalOverlay.IsVisible = isVisible;
        UpdateModalSizes();
    }

    private void CancelAiSetupOperations()
    {
        _aiDetectionCts?.Cancel();
        _aiDetectionCts?.Dispose();
        _aiDetectionCts = null;

        _aiPullCts?.Cancel();
        _aiPullCts?.Dispose();
        _aiPullCts = null;
    }

    private void ResetAiSetupModalUi()
    {
        _aiDetectionResult = null;
        _aiSelectedModelId = null;
        _aiPendingDownloadRecommendation = null;
        _aiModelCards.Clear();
        AiSetupModelsPanel.Children.Clear();

        AiSetupDetectionProgressPanel.IsVisible = true;
        AiSetupDetectionFailedPanel.IsVisible = false;
        AiSetupContentScrollViewer.IsVisible = false;
        AiSetupSystemDetailsPanel.Children.Clear();
        AiSetupRecommendedCard.IsVisible = false;
        AiSetupWarningTextBlock.IsVisible = false;
        AiSetupStatusTextBlock.IsVisible = false;
        AiSetupPullProgressBar.IsVisible = false;
        AiSetupPullProgressBar.IsIndeterminate = true;
        AiSetupDownloadConfirmPanel.IsVisible = false;
        AiSetupDownloadButton.IsEnabled = false;
    }

    private void StartAiSetupDetection()
    {
        CancelAiSetupOperations();
        _aiDetectionCts = new CancellationTokenSource();
        var token = _aiDetectionCts.Token;
        _ = RunAiSetupDetectionAsync(token);
    }

    private async Task RunAiSetupDetectionAsync(CancellationToken cancellationToken)
    {
        var startedAt = Environment.TickCount64;

        try
        {
            var profileTask = _systemProfileDetector.DetectAsync(cancellationToken);
            var ollamaTask = _ollamaRuntimeProbe.ProbeAsync(cancellationToken);
            await Task.WhenAll(profileTask, ollamaTask).ConfigureAwait(false);

            var detectionResult = AiModelRecommendationService.Recommend(
                await profileTask.ConfigureAwait(false),
                await ollamaTask.ConfigureAwait(false));

            var elapsed = Environment.TickCount64 - startedAt;
            if (elapsed < 300)
            {
                await Task.Delay((int)(300 - elapsed), cancellationToken).ConfigureAwait(false);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!AiSetupModalOverlay.IsVisible)
                {
                    return;
                }

                ApplyAiSetupDetectionResult(detectionResult);
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!AiSetupModalOverlay.IsVisible)
                {
                    return;
                }

                ShowAiSetupDetectionFailed();
            });
        }
    }

    private void ShowAiSetupDetectionFailed()
    {
        AiSetupDetectionProgressPanel.IsVisible = false;
        AiSetupDetectionFailedPanel.IsVisible = true;
        AiSetupContentScrollViewer.IsVisible = false;
    }

    private void ApplyAiSetupDetectionResult(AiSystemDetectionResult detectionResult)
    {
        _aiDetectionResult = detectionResult;
        _aiSelectedModelId = detectionResult.RecommendedModel?.Id;

        AiSetupDetectionProgressPanel.IsVisible = false;
        AiSetupDetectionFailedPanel.IsVisible = false;
        AiSetupContentScrollViewer.IsVisible = true;

        RenderAiSetupSystemDetails(detectionResult);

        if (!string.IsNullOrWhiteSpace(detectionResult.Profile.DetectionWarningKey))
        {
            AiSetupWarningTextBlock.Text = _localizer.Get(detectionResult.Profile.DetectionWarningKey);
            AiSetupWarningTextBlock.IsVisible = true;
        }
        else
        {
            AiSetupWarningTextBlock.IsVisible = false;
        }

        AiSetupPrivacyNoteTextBlock.Text = _localizer.Get(TranslationKeys.AiSetupPrivacyNote);

        RenderAiSetupRecommendedCard(detectionResult);
        RenderAiSetupModelCards(detectionResult);
        UpdateAiSetupDownloadButtonState();
    }

    private void RenderAiSetupSystemDetails(AiSystemDetectionResult detectionResult)
    {
        AiSetupSystemDetailsPanel.Children.Clear();
        var lines = AiSystemInfoFormatter.FormatDetailLines(
            detectionResult.Profile,
            detectionResult.Ollama,
            _diskSpaceChecker.GetAvailableBytesForLocalData(),
            _localizer);

        foreach (var line in lines)
        {
            AiSetupSystemDetailsPanel.Children.Add(new TextBlock
            {
                Text = line,
                Classes = { "re-vitae-secondary" },
                TextWrapping = TextWrapping.Wrap,
            });
        }
    }

    private void RenderAiSetupRecommendedCard(AiSystemDetectionResult detectionResult)
    {
        var recommended = detectionResult.Models.FirstOrDefault(model => model.IsRecommended);
        if (recommended is null)
        {
            AiSetupRecommendedCard.IsVisible = false;
            return;
        }

        AiSetupRecommendedCard.IsVisible = true;
        AiSetupRecommendedNameTextBlock.Text = _localizer.Get(recommended.Model.DisplayNameKey);
        AiSetupRecommendedMetaTextBlock.Text = _localizer.Format(
            TranslationKeys.AiSetupModelMeta,
            AiFormatBytes.Format(recommended.Model.ApproxDownloadBytes),
            AiFormatBytes.Format(recommended.Model.MinimumMemoryBytes));
    }

    private void RenderAiSetupModelCards(AiSystemDetectionResult detectionResult)
    {
        AiSetupModelsPanel.Children.Clear();
        _aiModelCards.Clear();

        foreach (var recommendation in detectionResult.Models)
        {
            var card = BuildAiModelCard(recommendation, detectionResult);
            _aiModelCards[recommendation.Model.Id] = card;
            AiSetupModelsPanel.Children.Add(card);
        }

        UpdateAiModelCardSelectionVisuals();
    }

    private Border BuildAiModelCard(AiModelRecommendation recommendation, AiSystemDetectionResult detectionResult)
    {
        var isInstalled = detectionResult.Ollama.InstalledModelTags.Any(tag =>
            string.Equals(tag, recommendation.Model.OllamaModelTag, StringComparison.OrdinalIgnoreCase) ||
            tag.StartsWith($"{recommendation.Model.OllamaModelTag}:", StringComparison.OrdinalIgnoreCase));

        var card = new Border
        {
            Classes = { "re-vitae-app-card" },
            Padding = new Thickness(14),
            Cursor = recommendation.IsDownloadAllowed
                ? new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                : null,
        };

        card.PointerReleased += (_, _) =>
        {
            if (!recommendation.IsDownloadAllowed)
            {
                return;
            }

            _aiSelectedModelId = recommendation.Model.Id;
            UpdateAiModelCardSelectionVisuals();
            UpdateAiSetupDownloadButtonState();
        };

        var content = new StackPanel { Spacing = 4 };
        var titleRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };

        titleRow.Children.Add(new TextBlock
        {
            Text = _localizer.Get(recommendation.Model.DisplayNameKey),
            Classes = { "re-vitae-app-title" },
            FontSize = 16,
        });

        if (recommendation.IsRecommended)
        {
            titleRow.Children.Add(new TextBlock
            {
                Text = _localizer.Get(TranslationKeys.AiSetupRecommended),
                Classes = { "re-vitae-primary" },
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            });
        }

        if (isInstalled)
        {
            titleRow.Children.Add(new TextBlock
            {
                Text = _localizer.Get(TranslationKeys.AiSetupAlreadyDownloaded),
                Classes = { "re-vitae-secondary" },
                FontSize = 12,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            });
        }

        content.Children.Add(titleRow);
        content.Children.Add(new TextBlock
        {
            Text = _localizer.Format(
                TranslationKeys.AiSetupModelMeta,
                AiFormatBytes.Format(recommendation.Model.ApproxDownloadBytes),
                AiFormatBytes.Format(recommendation.Model.MinimumMemoryBytes)),
            Classes = { "re-vitae-secondary" },
        });

        if (recommendation.RequiresOversizedWarning)
        {
            content.Children.Add(new TextBlock
            {
                Text = _localizer.Get(TranslationKeys.AiSetupOversizedWarning),
                Classes = { "re-vitae-error" },
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
            });
        }

        if (!string.IsNullOrWhiteSpace(recommendation.ReasonKey) && !recommendation.RequiresOversizedWarning)
        {
            content.Children.Add(new TextBlock
            {
                Text = _localizer.Get(recommendation.ReasonKey),
                Classes = { recommendation.IsDownloadAllowed ? "re-vitae-secondary" : "re-vitae-error" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        card.Child = content;
        card.Tag = recommendation;
        card.Opacity = recommendation.IsDownloadAllowed ? 1.0 : 0.5;
        return card;
    }

    private void UpdateAiModelCardSelectionVisuals()
    {
        foreach (var (modelId, card) in _aiModelCards)
        {
            var isSelected = string.Equals(modelId, _aiSelectedModelId, StringComparison.Ordinal);
            card.BorderBrush = isSelected
                ? new SolidColorBrush(Color.Parse("#2563EB"))
                : Brushes.Transparent;
            card.BorderThickness = isSelected ? new Thickness(2) : new Thickness(1);
        }
    }

    private void UpdateAiSetupDownloadButtonState()
    {
        if (_aiDetectionResult is null || string.IsNullOrWhiteSpace(_aiSelectedModelId))
        {
            AiSetupDownloadButton.IsEnabled = false;
            return;
        }

        var selected = _aiDetectionResult.Models.FirstOrDefault(model =>
            string.Equals(model.Model.Id, _aiSelectedModelId, StringComparison.Ordinal));

        if (selected is null)
        {
            AiSetupDownloadButton.IsEnabled = false;
            return;
        }

        var isInstalled = _aiDetectionResult.Ollama.InstalledModelTags.Any(tag =>
            string.Equals(tag, selected.Model.OllamaModelTag, StringComparison.OrdinalIgnoreCase) ||
            tag.StartsWith($"{selected.Model.OllamaModelTag}:", StringComparison.OrdinalIgnoreCase));

        AiSetupDownloadButton.IsEnabled = selected.IsDownloadAllowed &&
                                           _aiDetectionResult.Ollama.IsReachable &&
                                           !isInstalled &&
                                           AiSetupDownloadConfirmPanel.IsVisible == false &&
                                           AiSetupPullProgressBar.IsVisible == false;
    }

    private void OnAiSetupDetectionRetryClicked(object? sender, RoutedEventArgs e)
    {
        ResetAiSetupModalUi();
        StartAiSetupDetection();
    }

    private void OnAiSetupDownloadClicked(object? sender, RoutedEventArgs e)
    {
        if (_aiDetectionResult is null || string.IsNullOrWhiteSpace(_aiSelectedModelId))
        {
            return;
        }

        var selected = _aiDetectionResult.Models.FirstOrDefault(model =>
            string.Equals(model.Model.Id, _aiSelectedModelId, StringComparison.Ordinal));

        if (selected is null)
        {
            return;
        }

        if (!_aiDetectionResult.Ollama.IsReachable)
        {
            ShowAiSetupStatus(_localizer.Get(TranslationKeys.AiSetupOllamaNotRunning), isError: true);
            return;
        }

        if (!_diskSpaceChecker.HasSpaceForDownload(selected.Model.ApproxDownloadBytes))
        {
            var available = _diskSpaceChecker.GetAvailableBytesForLocalData();
            var required = AiFormatBytes.Format((long)(selected.Model.ApproxDownloadBytes * 1.1));
            var availableLabel = available is long availableBytes
                ? AiFormatBytes.Format(availableBytes)
                : _localizer.Get(TranslationKeys.AiSetupDiskSpaceUnknown);

            ShowAiSetupStatus(
                _localizer.Format(TranslationKeys.AiSetupInsufficientDiskSpace, required, availableLabel),
                isError: true);
            return;
        }

        _aiPendingDownloadRecommendation = selected;
        var confirmKey = selected.RequiresOversizedWarning
            ? TranslationKeys.AiSetupDownloadConfirmOversized
            : TranslationKeys.AiSetupDownloadConfirm;
        AiSetupDownloadConfirmTextBlock.Text = _localizer.Format(
            confirmKey,
            _localizer.Get(selected.Model.DisplayNameKey),
            AiFormatBytes.Format(selected.Model.ApproxDownloadBytes));
        AiSetupDownloadConfirmPanel.IsVisible = true;
        UpdateAiSetupDownloadButtonState();
    }

    private void OnAiSetupDownloadConfirmCancelClicked(object? sender, RoutedEventArgs e)
    {
        _aiPendingDownloadRecommendation = null;
        AiSetupDownloadConfirmPanel.IsVisible = false;
        UpdateAiSetupDownloadButtonState();
    }

    private async void OnAiSetupDownloadConfirmOkClicked(object? sender, RoutedEventArgs e)
    {
        var selected = _aiPendingDownloadRecommendation;
        _aiPendingDownloadRecommendation = null;
        AiSetupDownloadConfirmPanel.IsVisible = false;

        if (selected is null)
        {
            return;
        }

        _aiPullCts?.Cancel();
        _aiPullCts?.Dispose();
        _aiPullCts = new CancellationTokenSource();
        var token = _aiPullCts.Token;

        AiSetupPullProgressBar.IsVisible = true;
        AiSetupPullProgressBar.IsIndeterminate = true;
        AiSetupStatusTextBlock.IsVisible = true;
        AiSetupStatusTextBlock.Classes.Clear();
        AiSetupStatusTextBlock.Classes.Add("re-vitae-secondary");
        AiSetupStatusTextBlock.Text = _localizer.Format(TranslationKeys.AiSetupPullProgress, string.Empty);
        UpdateAiSetupDownloadButtonState();

        var progress = new Progress<OllamaPullProgress>(pullProgress =>
        {
            Dispatcher.UIThread.Post(() => ApplyAiPullProgress(pullProgress));
        });

        var result = await Task.Run(() =>
            _ollamaPullClient.PullAsync(selected.Model.OllamaModelTag, progress, token)).ConfigureAwait(true);

        if (token.IsCancellationRequested || result.Outcome == OllamaPullOutcome.Cancelled)
        {
            AiSetupPullProgressBar.IsVisible = false;
            AiSetupStatusTextBlock.IsVisible = false;
            UpdateAiSetupDownloadButtonState();
            return;
        }

        if (result.Outcome != OllamaPullOutcome.Succeeded)
        {
            AiSetupPullProgressBar.IsVisible = false;
            ShowAiSetupStatus(_localizer.Get(TranslationKeys.AiSetupPullFailed), isError: true);
            UpdateAiSetupDownloadButtonState();
            return;
        }

        _aiSettingsStorage.Save(new AiSettingsSnapshot(
            selected.Model.Id,
            selected.Model.OllamaModelTag,
            DateTimeOffset.UtcNow));

        AiSetupPullProgressBar.IsVisible = false;
        ShowAiSetupStatus(_localizer.Get(TranslationKeys.AiSetupPullComplete), isError: false);
        StartAiSetupDetection();
    }

    private void ApplyAiPullProgress(OllamaPullProgress pullProgress)
    {
        if (pullProgress.Total is > 0 && pullProgress.Completed is >= 0)
        {
            AiSetupPullProgressBar.IsIndeterminate = false;
            AiSetupPullProgressBar.Maximum = pullProgress.Total.Value;
            AiSetupPullProgressBar.Value = pullProgress.Completed.Value;
        }
        else
        {
            AiSetupPullProgressBar.IsIndeterminate = true;
        }

        AiSetupStatusTextBlock.Text = _localizer.Format(
            TranslationKeys.AiSetupPullProgress,
            pullProgress.Status);
    }

    private void ShowAiSetupStatus(string message, bool isError)
    {
        AiSetupStatusTextBlock.Text = message;
        AiSetupStatusTextBlock.IsVisible = true;
        AiSetupStatusTextBlock.Classes.Clear();
        AiSetupStatusTextBlock.Classes.Add(isError ? "re-vitae-error" : "re-vitae-secondary");
    }

    private void ApplyAiSetupLocalization()
    {
        ToolTip.SetTip(OpenAiSetupButton, _localizer.Get(TranslationKeys.OpenAiSetup));
        AutomationProperties.SetName(OpenAiSetupButton, _localizer.Get(TranslationKeys.OpenAiSetup));
        AiSetupTitleTextBlock.Text = _localizer.Get(TranslationKeys.AiSetupTitle);
        AiSetupDetectingTextBlock.Text = _localizer.Get(TranslationKeys.AiSetupDetecting);
        AiSetupDetectionFailedTextBlock.Text = _localizer.Get(TranslationKeys.AiSetupDetectionFailed);
        AiSetupDetectionRetryButton.Content = _localizer.Get(TranslationKeys.AiSetupRetry);
        AiSetupSystemDetailsTitleTextBlock.Text = _localizer.Get(TranslationKeys.AiSetupSystemDetailsTitle);
        AiSetupRecommendedBadgeTextBlock.Text = _localizer.Get(TranslationKeys.AiSetupRecommended);
        AiSetupAllModelsLabelTextBlock.Text = _localizer.Get(TranslationKeys.AiSetupAllModels);
        AiSetupDownloadButton.Content = _localizer.Get(TranslationKeys.AiSetupDownload);
        AiSetupDownloadConfirmCancelButton.Content = _localizer.Get(TranslationKeys.Cancel);
        AiSetupDownloadConfirmOkButton.Content = _localizer.Get(TranslationKeys.Confirm);

        var closeLabel = _localizer.Get(TranslationKeys.Close);
        AiSetupBottomCloseButton.Content = closeLabel;
        ToolTip.SetTip(AiSetupTopCloseButton, closeLabel);
        AutomationProperties.SetName(AiSetupTopCloseButton, closeLabel);

        if (_aiDetectionResult is not null)
        {
            ApplyAiSetupDetectionResult(_aiDetectionResult);
        }
    }
}
