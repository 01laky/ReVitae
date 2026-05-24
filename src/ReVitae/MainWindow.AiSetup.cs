using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Download;
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
    private readonly AiModelLifecycleService _aiModelLifecycleService = new();

    private enum AiModelPendingAction
    {
        None,
        Uninstall,
        CleanStaleDownload,
    }

    private CancellationTokenSource? _aiDetectionCts;
    private AiSystemDetectionResult? _aiDetectionResult;
    private string? _aiSelectedModelId;
    private AiModelRecommendation? _aiPendingDownloadRecommendation;
    private AiModelPendingAction _aiPendingModelAction = AiModelPendingAction.None;
    private string? _aiPendingModelActionModelId;
    private readonly Dictionary<string, Border> _aiModelCards = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AiModelInstallationStatus> _aiModelInstallationStatuses =
        new(StringComparer.Ordinal);

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
            CancelAiDetectionOnly();
        }

        if (isVisible)
        {
            HideOtherContentModals(AiSetupModalOverlay);
            UpdateAiActiveBackendStrip();

            if (_aiDownloadCoordinator.HasActiveJob)
            {
                EnterAiSetupDownloadMode();
            }
            else
            {
                ResetAiSetupModalUi();
            }

            if (_aiDetectionResult is null)
            {
                StartAiSetupDetection();
            }
            else
            {
                UpdateAiSetupDownloadButtonState();
            }

            UpdateAiDownloadUi();
        }

        AiSetupModalOverlay.IsVisible = isVisible;
        UpdateModalSizes();
        UpdateAiDownloadUi();
    }

    private void CancelAiDetectionOnly()
    {
        _aiDetectionCts?.Cancel();
        _aiDetectionCts?.Dispose();
        _aiDetectionCts = null;
    }

    private void CancelAiSetupOperations()
    {
        CancelAiDetectionOnly();
    }

    private void ResetAiSetupModalUi()
    {
        _aiDetectionResult = null;
        _aiSelectedModelId = null;
        _aiPendingDownloadRecommendation = null;
        _aiPendingModelAction = AiModelPendingAction.None;
        _aiPendingModelActionModelId = null;
        _aiModelCards.Clear();
        _aiModelInstallationStatuses.Clear();
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
        AiSetupDownloadStopConfirmPanel.IsVisible = false;
        AiSetupModelActionConfirmPanel.IsVisible = false;
        AiSetupProviderSwitchConfirmPanel.IsVisible = false;
        AiSetupProviderUntestedConfirmPanel.IsVisible = false;
        AiSetupDownloadJobBanner.IsVisible = false;
        AiSetupDownloadButton.IsEnabled = false;
    }

    private void StartAiSetupDetection()
    {
        CancelAiDetectionOnly();
        AiSetupDetectionProgressPanel.IsVisible = true;
        AiSetupDetectionFailedPanel.IsVisible = false;
        AiSetupContentScrollViewer.IsVisible = _aiDetectionResult is not null ||
                                                 _aiDownloadCoordinator.HasActiveJob;
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
        _aiSelectedModelId = PickDefaultSelectedModelId(detectionResult);

        AiSetupDetectionProgressPanel.IsVisible = false;
        AiSetupDetectionFailedPanel.IsVisible = false;
        AiSetupContentScrollViewer.IsVisible = true;
        AiSetupDownloadRefreshSystemButton.IsVisible = false;

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
        UpdateAiDownloadUi();
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
        _aiModelInstallationStatuses.Clear();

        var installationStatuses = _aiModelLifecycleService.AnalyzeCatalog(
            detectionResult.Ollama,
            _aiDownloadCoordinator.CurrentSnapshot,
            _aiDownloadCoordinator.HasActiveJob);

        foreach (var recommendation in detectionResult.Models)
        {
            var installationStatus = installationStatuses.First(status =>
                string.Equals(status.Model.Id, recommendation.Model.Id, StringComparison.Ordinal));
            _aiModelInstallationStatuses[recommendation.Model.Id] = installationStatus;

            var card = BuildAiModelCard(recommendation, installationStatus);
            _aiModelCards[recommendation.Model.Id] = card;
            AiSetupModelsPanel.Children.Add(card);
        }

        UpdateAiModelCardSelectionVisuals();
    }

    private Border BuildAiModelCard(
        AiModelRecommendation recommendation,
        AiModelInstallationStatus installationStatus)
    {
        var isInstalled = installationStatus.Presence == AiModelInstallPresence.Installed;
        var hasStaleDownload = installationStatus.Presence == AiModelInstallPresence.StaleDownload;
        var isActiveDownload = installationStatus.Presence == AiModelInstallPresence.ActiveDownload;

        var card = new Border
        {
            Classes = { "re-vitae-app-card" },
            Padding = new Thickness(20),
            Cursor = recommendation.IsDownloadAllowed && !_aiDownloadCoordinator.HasActiveJob
                ? new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                : null,
        };

        card.PointerReleased += (_, _) =>
        {
            if (!recommendation.IsDownloadAllowed ||
                _aiDownloadCoordinator.HasActiveJob ||
                isInstalled)
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

        content.Children.Add(titleRow);

        var (statusText, statusClass) = GetModelInstallationStatusPresentation(installationStatus);
        content.Children.Add(new TextBlock
        {
            Text = statusText,
            Classes = { statusClass },
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
        });

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

        var actionsRow = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 6, 0, 0),
        };

        if (isInstalled)
        {
            var isActive = IsLocalModelActive(recommendation.Model.Id);
            var activateButton = new Button
            {
                Content = _localizer.Get(
                    isActive
                        ? TranslationKeys.AiSetupModelDeactivate
                        : TranslationKeys.AiSetupModelActivate),
                Classes = { isActive ? "re-vitae-secondary" : "re-vitae-primary" },
            };
            activateButton.Click += (_, _) =>
            {
                if (IsLocalModelActive(recommendation.Model.Id))
                {
                    DeactivateLocalModelIfActive(recommendation.Model.Id);
                }
                else
                {
                    TryActivateLocalModel(recommendation.Model.Id, installed: true);
                }
            };
            actionsRow.Children.Add(activateButton);

            var removeButton = new Button
            {
                Content = _localizer.Get(TranslationKeys.AiSetupModelRemove),
                Classes = { "re-vitae-secondary" },
                IsEnabled = installationStatus.CanUninstall,
            };
            removeButton.Click += (_, _) => BeginAiModelActionConfirm(
                recommendation.Model.Id,
                AiModelPendingAction.Uninstall);
            actionsRow.Children.Add(removeButton);
        }

        if (installationStatus.CanCleanStaleDownload)
        {
            var cleanButton = new Button
            {
                Content = _localizer.Get(TranslationKeys.AiSetupModelCleanStale),
                Classes = { "re-vitae-secondary" },
            };
            cleanButton.Click += (_, _) => BeginAiModelActionConfirm(
                recommendation.Model.Id,
                AiModelPendingAction.CleanStaleDownload);
            actionsRow.Children.Add(cleanButton);
        }

        if (actionsRow.Children.Count > 0)
        {
            content.Children.Add(actionsRow);
        }

        card.Child = content;
        card.Tag = recommendation;
        card.Opacity = recommendation.IsDownloadAllowed &&
                       !_aiDownloadCoordinator.HasActiveJob &&
                       !isInstalled &&
                       !isActiveDownload
            ? 1.0
            : isInstalled || hasStaleDownload
                ? 1.0
                : 0.5;
        return card;
    }

    private (string Text, string CssClass) GetModelInstallationStatusPresentation(
        AiModelInstallationStatus installationStatus)
    {
        if (installationStatus.Presence == AiModelInstallPresence.Installed &&
            IsLocalModelActive(installationStatus.Model.Id))
        {
            return (_localizer.Get(TranslationKeys.AiSetupModelActive), "re-vitae-primary");
        }

        return installationStatus.Presence switch
        {
            AiModelInstallPresence.Installed =>
                (_localizer.Get(TranslationKeys.AiSetupModelStatusDownloaded), "re-vitae-primary"),
            AiModelInstallPresence.ActiveDownload =>
                (_localizer.Get(TranslationKeys.AiSetupModelStatusDownloading), "re-vitae-primary"),
            AiModelInstallPresence.StaleDownload =>
                (_localizer.Get(TranslationKeys.AiSetupModelStaleDownload), "re-vitae-error"),
            _ => (_localizer.Get(TranslationKeys.AiSetupModelStatusNotDownloaded), "re-vitae-secondary"),
        };
    }

    private void RefreshAiSetupModelCardsIfVisible()
    {
        if (!AiSetupModalOverlay.IsVisible || _aiDetectionResult is null)
        {
            return;
        }

        RenderAiSetupModelCards(_aiDetectionResult);
        UpdateAiSetupDownloadButtonState();
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
        if (_aiDownloadCoordinator.HasActiveJob)
        {
            AiSetupDownloadButton.IsEnabled = false;
            return;
        }

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

        var isInstalled = _aiModelInstallationStatuses.TryGetValue(_aiSelectedModelId, out var installationStatus)
            ? installationStatus.Presence == AiModelInstallPresence.Installed
            : IsAiModelInstalled(_aiDetectionResult, selected.Model.OllamaModelTag);
        var canDownload = selected.IsDownloadAllowed &&
                          !isInstalled &&
                          !AiSetupDownloadConfirmPanel.IsVisible &&
                          !AiSetupModelActionConfirmPanel.IsVisible;

        AiSetupDownloadButton.IsEnabled = canDownload;

        if (canDownload && AiSetupStatusTextBlock.IsVisible &&
            AiSetupStatusTextBlock.Classes.Contains("re-vitae-error") &&
            (AiSetupStatusTextBlock.Text == _localizer.Get(TranslationKeys.AiSetupOllamaNotInstalled) ||
             AiSetupStatusTextBlock.Text == _localizer.Format(
                 TranslationKeys.AiSetupOllamaNotRunning,
                 OllamaHost.DisplayAddress)))
        {
            AiSetupStatusTextBlock.IsVisible = false;
        }
    }

    private static string? PickDefaultSelectedModelId(AiSystemDetectionResult detectionResult)
    {
        var recommendedId = detectionResult.RecommendedModel?.Id;
        if (recommendedId is not null &&
            TryGetSelectableRecommendation(detectionResult, recommendedId) is not null)
        {
            return recommendedId;
        }

        return detectionResult.Models
            .FirstOrDefault(recommendation => TryGetSelectableRecommendation(
                detectionResult,
                recommendation.Model.Id) is not null)
            ?.Model.Id ?? recommendedId;
    }

    private static AiModelRecommendation? TryGetSelectableRecommendation(
        AiSystemDetectionResult detectionResult,
        string modelId)
    {
        var recommendation = detectionResult.Models.FirstOrDefault(model =>
            string.Equals(model.Model.Id, modelId, StringComparison.Ordinal));

        if (recommendation is null ||
            !recommendation.IsDownloadAllowed ||
            IsAiModelInstalled(detectionResult, recommendation.Model.OllamaModelTag))
        {
            return null;
        }

        return recommendation;
    }

    private static bool IsAiModelInstalled(
        AiSystemDetectionResult detectionResult,
        string ollamaModelTag) =>
        AiModelInstallationMatcher.IsModelInstalled(detectionResult.Ollama.InstalledModelTags, ollamaModelTag);

    private void BeginAiModelActionConfirm(string modelId, AiModelPendingAction action)
    {
        if (_aiDetectionResult is null ||
            _aiDownloadCoordinator.HasActiveJob ||
            AiSetupDownloadConfirmPanel.IsVisible)
        {
            return;
        }

        var model = _aiDetectionResult.Models
            .FirstOrDefault(entry => string.Equals(entry.Model.Id, modelId, StringComparison.Ordinal))
            ?.Model;
        if (model is null)
        {
            return;
        }

        _aiPendingModelAction = action;
        _aiPendingModelActionModelId = modelId;

        var confirmKey = action == AiModelPendingAction.Uninstall
            ? TranslationKeys.AiSetupModelRemoveConfirm
            : TranslationKeys.AiSetupModelCleanStaleConfirm;
        AiSetupModelActionConfirmTextBlock.Text = _localizer.Format(
            confirmKey,
            _localizer.Get(model.DisplayNameKey));
        AiSetupModelActionConfirmPanel.IsVisible = true;
        UpdateAiSetupDownloadButtonState();
    }

    private void OnAiSetupModelActionConfirmCancelClicked(object? sender, RoutedEventArgs e)
    {
        _aiPendingModelAction = AiModelPendingAction.None;
        _aiPendingModelActionModelId = null;
        AiSetupModelActionConfirmPanel.IsVisible = false;
        UpdateAiSetupDownloadButtonState();
    }

    private async void OnAiSetupModelActionConfirmOkClicked(object? sender, RoutedEventArgs e)
    {
        var action = _aiPendingModelAction;
        var modelId = _aiPendingModelActionModelId;
        _aiPendingModelAction = AiModelPendingAction.None;
        _aiPendingModelActionModelId = null;
        AiSetupModelActionConfirmPanel.IsVisible = false;

        if (_aiDetectionResult is null || modelId is null || action == AiModelPendingAction.None)
        {
            return;
        }

        var model = _aiDetectionResult.Models
            .FirstOrDefault(entry => string.Equals(entry.Model.Id, modelId, StringComparison.Ordinal))
            ?.Model;
        if (model is null)
        {
            return;
        }

        AiModelLifecycleResult result;
        if (action == AiModelPendingAction.Uninstall)
        {
            result = await _aiModelLifecycleService.TryUninstallModelAsync(
                    model,
                    _aiDetectionResult.Ollama,
                    _aiDownloadCoordinator.HasActiveJob)
                .ConfigureAwait(true);
        }
        else
        {
            result = await _aiModelLifecycleService.TryCleanStaleDownloadAsync(
                    model,
                    _aiDetectionResult.Ollama,
                    _aiDownloadCoordinator.HasActiveJob)
                .ConfigureAwait(true);
        }

        if (!result.Succeeded)
        {
            ShowAiSetupStatus(
                result.ErrorMessageKey == TranslationKeys.AiSetupOllamaNotRunning
                    ? _localizer.Format(TranslationKeys.AiSetupOllamaNotRunning, OllamaHost.DisplayAddress)
                    : _localizer.Get(result.ErrorMessageKey ?? TranslationKeys.AiSetupModelRemoveFailed),
                isError: true);
            UpdateAiSetupDownloadButtonState();
            return;
        }

        _aiDownloadCoordinator.ResetIfJobMatches(modelId);
        ShowAiSetupStatus(
            _localizer.Get(action == AiModelPendingAction.Uninstall
                ? TranslationKeys.AiSetupModelRemoveComplete
                : TranslationKeys.AiSetupModelCleanStaleComplete),
            isError: false);
        StartAiSetupDetection();
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

        if (!selected.IsDownloadAllowed)
        {
            ShowAiSetupStatus(
                _localizer.Get(TranslationKeys.AiSetupRequiresMoreMemory),
                isError: true);
            return;
        }

        if (IsAiModelInstalled(_aiDetectionResult, selected.Model.OllamaModelTag))
        {
            ShowAiSetupStatus(_localizer.Get(TranslationKeys.AiSetupAlreadyDownloaded), isError: false);
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

    private void OnAiSetupDownloadConfirmOkClicked(object? sender, RoutedEventArgs e)
    {
        var selected = _aiPendingDownloadRecommendation;
        _aiPendingDownloadRecommendation = null;
        AiSetupDownloadConfirmPanel.IsVisible = false;

        if (selected is null)
        {
            return;
        }

        if (!_aiDownloadCoordinator.TryStart(selected.Model, selected.RequiresOversizedWarning))
        {
            ShowAiSetupStatus(_localizer.Get(TranslationKeys.AiSetupPullFailed), isError: true);
            UpdateAiSetupDownloadButtonState();
            return;
        }

        EnterAiSetupDownloadMode();
        AiSetupStatusTextBlock.IsVisible = false;
        UpdateAiSetupDownloadButtonState();
        UpdateAiDownloadUi();
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
        AiSetupModelActionConfirmCancelButton.Content = _localizer.Get(TranslationKeys.Cancel);
        AiSetupModelActionConfirmOkButton.Content = _localizer.Get(TranslationKeys.Confirm);

        ApplyAiProviderLocalization();

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
