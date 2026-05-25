using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Cv;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;
using ReVitae.Ui.Quality;

namespace ReVitae;

public partial class MainWindow
{
    private AiCvCompletionService? _aiCvCompletionService;
    private CancellationTokenSource? _aiCvCompletionCts;
    private bool _aiCvCompletionInFlight;
    private bool _onlineCvSendConfirmedThisSession;
    private CvQualityHint? _pendingAiCvHint;
    private AiCvFieldTarget? _pendingAiFieldTarget;
    private string? _currentAiSuggestedText;

    private AiCvCompletionService AiCvCompletion =>
        _aiCvCompletionService ??= new AiCvCompletionService(
            _aiProviderConfigService,
            new AiBackendRuntimeResolver());

    private void InitializeAiCvCompletion()
    {
        QualityHintFlyoutHelper.AiOptions = new QualityHintAiOptions
        {
            IsSupported = hint => AiCvCompletion.IsQualityHintSupported(hint.Id),
            GetActiveBackendKind = () => ActiveBackendService.GetActiveSnapshot().Kind,
            ImproveWithAi = hint => _ = BeginAiCvImprovementAsync(hint),
            SetUpAi = () => SetAiSetupModalVisible(true),
            IsCompletionInFlight = () => _aiCvCompletionInFlight || _aiImportInFlight,
        };
    }

    private async Task BeginAiCvImprovementAsync(CvQualityHint hint)
    {
        if (_aiCvCompletionInFlight)
        {
            return;
        }

        var backend = ActiveBackendService.GetActiveSnapshot();
        if (backend.Kind == AiBackendKind.Online && !_onlineCvSendConfirmedThisSession)
        {
            _pendingAiCvHint = hint;
            ShowAiCvOnlineSendConfirm(backend);
            return;
        }

        await RunAiCvImprovementAsync(hint).ConfigureAwait(true);
    }

    private void ShowAiCvOnlineSendConfirm(ActiveAiBackendSnapshot backend)
    {
        var providerName = backend.DisplayNameKey is not null
            ? _localizer.Get(backend.DisplayNameKey)
            : _localizer.Get(TranslationKeys.AiSetupActiveAiNone);

        AiCvOnlineSendConfirmTextBlock.Text =
            _localizer.Format(TranslationKeys.AiCvOnlineSendConfirm, providerName);
        AiCvOnlineSendConfirmPanel.IsVisible = true;
        SetAiSuggestionModalVisible(true);
    }

    private void OnAiCvOnlineSendConfirmCancelClicked(object? sender, RoutedEventArgs e)
    {
        AiCvOnlineSendConfirmPanel.IsVisible = false;
        _pendingAiCvHint = null;
        SetAiSuggestionModalVisible(false);
    }

    private async void OnAiCvOnlineSendConfirmOkClicked(object? sender, RoutedEventArgs e)
    {
        AiCvOnlineSendConfirmPanel.IsVisible = false;
        _onlineCvSendConfirmedThisSession = true;

        var hint = _pendingAiCvHint;
        _pendingAiCvHint = null;
        if (hint is null)
        {
            SetAiSuggestionModalVisible(false);
            return;
        }

        await RunAiCvImprovementAsync(hint).ConfigureAwait(true);
    }

    private async Task RunAiCvImprovementAsync(CvQualityHint hint)
    {
        _aiCvCompletionInFlight = true;
        _aiCvCompletionCts?.Cancel();
        _aiCvCompletionCts?.Dispose();
        _aiCvCompletionCts = new CancellationTokenSource();

        _pendingAiFieldTarget = AiCvTaskRegistry.ResolveFieldTarget(hint);
        _currentAiSuggestedText = null;

        SetAiSuggestionModalVisible(true);
        ShowAiSuggestionLoadingState();

        try
        {
            var snapshot = BuildExportSourceData();
            var result = await AiCvCompletion.CompleteForQualityHintAsync(
                snapshot,
                hint,
                _localizer.LanguageCode,
                _aiCvCompletionCts.Token).ConfigureAwait(true);

            if (result.Cancelled)
            {
                SetAiSuggestionModalVisible(false);
                return;
            }

            if (result.Succeeded && !string.IsNullOrWhiteSpace(result.SuggestedText))
            {
                ShowAiSuggestionSuccessState(result.SuggestedText, result.BackendUsed?.Label);
                return;
            }

            ShowAiSuggestionErrorState(result.ErrorMessageKey, result.BackendUsed?.Label);
        }
        finally
        {
            _aiCvCompletionInFlight = false;
        }
    }

    private void ShowAiSuggestionLoadingState()
    {
        AiCvSuggestionLoadingPanel.IsVisible = true;
        AiCvSuggestionContentPanel.IsVisible = false;
        AiCvSuggestionErrorPanel.IsVisible = false;
        AiCvSuggestionActionsPanel.IsVisible = false;
        AiCvSuggestionBackendTextBlock.Text = string.Empty;
        AiCvSuggestionTitleTextBlock.Text = _localizer.Get(TranslationKeys.AiCvSuggestionTitle);
    }

    private void ShowAiSuggestionSuccessState(string suggestedText, string? backendLabel)
    {
        _currentAiSuggestedText = suggestedText;
        AiCvSuggestionLoadingPanel.IsVisible = false;
        AiCvSuggestionContentPanel.IsVisible = true;
        AiCvSuggestionErrorPanel.IsVisible = false;
        AiCvSuggestionActionsPanel.IsVisible = true;
        AiCvSuggestionTextBox.Text = suggestedText;
        AiCvSuggestionBackendTextBlock.Text = backendLabel ?? string.Empty;
        AiCvSuggestionRetryButton.IsVisible = false;
        ApplyAiSuggestionModalLocalization();
    }

    private void ShowAiSuggestionErrorState(string? errorKey, string? backendLabel)
    {
        AiCvSuggestionLoadingPanel.IsVisible = false;
        AiCvSuggestionContentPanel.IsVisible = false;
        AiCvSuggestionErrorPanel.IsVisible = true;
        AiCvSuggestionActionsPanel.IsVisible = true;
        AiCvSuggestionBackendTextBlock.Text = backendLabel ?? string.Empty;
        AiCvSuggestionErrorTextBlock.Text = FormatAiCvErrorMessage(errorKey);
        AiCvSuggestionRetryButton.IsVisible = true;
        ApplyAiSuggestionModalLocalization();
    }

    private string FormatAiCvErrorMessage(string? errorKey)
    {
        if (string.IsNullOrWhiteSpace(errorKey))
        {
            return _localizer.Get(TranslationKeys.AiCvTaskFailed);
        }

        return _localizer.Get(errorKey);
    }

    private void SetAiSuggestionModalVisible(bool isVisible)
    {
        AiSuggestionModalOverlay.IsVisible = isVisible;
        if (!isVisible)
        {
            AiCvOnlineSendConfirmPanel.IsVisible = false;
            _pendingAiCvHint = null;
            _pendingAiFieldTarget = null;
            _currentAiSuggestedText = null;
        }
    }

    private void OnCloseAiSuggestionModalClicked(object? sender, RoutedEventArgs e)
    {
        _aiCvCompletionCts?.Cancel();
        SetAiSuggestionModalVisible(false);
    }

    private void OnAiCvSuggestionAcceptClicked(object? sender, RoutedEventArgs e)
    {
        if (_pendingAiFieldTarget is not null && !string.IsNullOrWhiteSpace(_currentAiSuggestedText))
        {
            ApplyAiSuggestionToField(_pendingAiFieldTarget, _currentAiSuggestedText);
        }

        SetAiSuggestionModalVisible(false);
    }

    private void OnAiCvSuggestionEditClicked(object? sender, RoutedEventArgs e)
    {
        if (_pendingAiFieldTarget is not null && !string.IsNullOrWhiteSpace(_currentAiSuggestedText))
        {
            TryNavigateToField(_pendingAiFieldTarget, _currentAiSuggestedText);
        }

        SetAiSuggestionModalVisible(false);
    }

    private void OnAiCvSuggestionCancelClicked(object? sender, RoutedEventArgs e)
    {
        _aiCvCompletionCts?.Cancel();
        SetAiSuggestionModalVisible(false);
    }

    private async void OnAiCvSuggestionRetryClicked(object? sender, RoutedEventArgs e)
    {
        if (_pendingAiFieldTarget is null)
        {
            return;
        }

        var hint = _lastQualityReport.Hints.FirstOrDefault(item =>
            item.EntryId == _pendingAiFieldTarget.EntryId &&
            string.Equals(item.FieldKey, _pendingAiFieldTarget.FieldKey, StringComparison.Ordinal));

        if (hint is null)
        {
            return;
        }

        await RunAiCvImprovementAsync(hint).ConfigureAwait(true);
    }

    private void ApplyAiSuggestionToField(AiCvFieldTarget target, string text)
    {
        switch (target.Section)
        {
            case CvImportSectionId.PersonalInformation
                when target.FieldKey == MainPersonalInformationFieldKeys.ShortSummary:
                ShortSummaryTextBox.Text = text;
                break;
            case CvImportSectionId.WorkExperience:
                WorkExperienceSection.TryApplyFieldText(target.FieldKey, text);
                break;
            case CvImportSectionId.Projects:
                ProjectsSection.TryApplyFieldText(target.FieldKey, text);
                break;
        }

        UpdateValidationState();
        UpdateQualityHints();
    }

    private bool TryNavigateToField(AiCvFieldTarget target, string? prefilledText = null)
    {
        if (!string.IsNullOrWhiteSpace(prefilledText))
        {
            ApplyAiSuggestionToField(target, prefilledText);
        }

        if (target.Section == CvImportSectionId.PersonalInformation)
        {
            PersonalInformationSection.IsExpanded = true;
            if (target.FieldKey == MainPersonalInformationFieldKeys.ShortSummary)
            {
                ShortSummaryTextBox.Focus();
                ShortSummaryTextBox.CaretIndex = ShortSummaryTextBox.Text?.Length ?? 0;
                return true;
            }
        }

        return ExpandAndRevealField(target.FieldKey);
    }

    private void ApplyAiSuggestionModalLocalization()
    {
        AiCvSuggestionTitleTextBlock.Text = _localizer.Get(TranslationKeys.AiCvSuggestionTitle);
        AiCvSuggestionLoadingTextBlock.Text = _localizer.Get(TranslationKeys.AiCvSuggestionLoading);
        AiCvSuggestionAcceptButton.Content = _localizer.Get(TranslationKeys.AiCvSuggestionAccept);
        AiCvSuggestionEditButton.Content = _localizer.Get(TranslationKeys.AiCvSuggestionEdit);
        AiCvSuggestionCancelButton.Content = _localizer.Get(TranslationKeys.AiCvSuggestionCancel);
        AiCvSuggestionRetryButton.Content = _localizer.Get(TranslationKeys.AiCvSuggestionRetry);
        AiCvOnlineSendConfirmOkButton.Content = _localizer.Get(TranslationKeys.Confirm);
        AiCvOnlineSendConfirmCancelButton.Content = _localizer.Get(TranslationKeys.Cancel);
    }
}
