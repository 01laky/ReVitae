using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Import;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae;

public partial class MainWindow
{
	private CvTextImportAttempt? _lastTextImportAttempt;
	private CvImportResult? _pendingAiImportBaseline;
	private AiCvImportOutcome? _pendingAiImportOutcome;
	private bool _aiImportInFlight;
	private bool _pendingAiImportAfterOnlineConfirm;
	private AiCvImportMergeMode _pendingAiImportMergeMode = AiCvImportMergeMode.ReplaceAll;
	private CancellationTokenSource? _aiImportCancellation;
	private AiCvImportService? _aiCvImportService;

	private AiCvImportService AiCvImportService =>
		_aiCvImportService ??= new AiCvImportService(_aiProviderConfigService, new AiBackendRuntimeResolver());

	private async void OnIntroImportTryAiClicked(object? sender, RoutedEventArgs e) =>
		await BeginAiImportFromLastAttemptAsync(AiCvImportMergeMode.ReplaceAll);

	private async void OnReplaceImportTryAiClicked(object? sender, RoutedEventArgs e) =>
		await BeginAiImportFromLastAttemptAsync(AiCvImportMergeMode.ReplaceAll);

	private async void OnImportAiEnhanceClicked(object? sender, RoutedEventArgs e)
	{
		var mergeMode = _isProjectDirty
			? AiCvImportMergeMode.FillEmptyOnly
			: AiCvImportMergeMode.ReplaceAll;
		await BeginAiImportFromLastAttemptAsync(mergeMode);
	}

	private async Task BeginAiImportFromLastAttemptAsync(AiCvImportMergeMode mergeMode)
	{
		if (_lastTextImportAttempt is null || _aiImportInFlight)
		{
			return;
		}

		var backend = AiCvImportService.GetBackendStatus();
		if (!backend.IsAvailable)
		{
			if (backend.Kind == AiBackendKind.None)
			{
				SetAiSetupModalVisible(true);
			}
			else
			{
				ExportStatusTextBlock.Text = _localizer.Get(
					backend.UnavailableMessageKey ?? TranslationKeys.ImportAiNoBackend);
			}

			return;
		}

		if (backend.Kind == AiBackendKind.Online && !_onlineCvSendConfirmedThisSession)
		{
			_pendingAiImportMergeMode = mergeMode;
			_pendingAiImportAfterOnlineConfirm = true;
			ShowAiImportOnlineConfirm(backend);
			return;
		}

		await RunAiImportAsync(_lastTextImportAttempt, mergeMode);
	}

	private void ShowAiImportOnlineConfirm(AiCvBackendStatus backend)
	{
		var providerName = backend.Snapshot?.DisplayNameKey is { } key
			? _localizer.Get(key)
			: _localizer.Get(TranslationKeys.AiSetupActiveAiOnline);
		AiImportOnlineConfirmTextBlock.Text =
			_localizer.Format(TranslationKeys.ImportAiOnlineConfirm, providerName);
		AiImportOnlineConfirmOverlay.IsVisible = true;
	}

	private async void OnAiImportOnlineConfirmOkClicked(object? sender, RoutedEventArgs e)
	{
		_onlineCvSendConfirmedThisSession = true;
		AiImportOnlineConfirmOverlay.IsVisible = false;
		if (_pendingAiImportAfterOnlineConfirm && _lastTextImportAttempt is not null)
		{
			_pendingAiImportAfterOnlineConfirm = false;
			await RunAiImportAsync(_lastTextImportAttempt, _pendingAiImportMergeMode);
		}
	}

	private void OnAiImportOnlineConfirmCancelClicked(object? sender, RoutedEventArgs e)
	{
		_pendingAiImportAfterOnlineConfirm = false;
		AiImportOnlineConfirmOverlay.IsVisible = false;
	}

	private async Task RunAiImportAsync(CvTextImportAttempt attempt, AiCvImportMergeMode mergeMode)
	{
		if (_aiImportInFlight)
		{
			return;
		}

		_aiImportInFlight = true;
		_aiImportCancellation = new CancellationTokenSource();
		SetAiImportProgressVisible(true);

		var plan = AiCvImportService.CreatePlan(attempt.NormalizedText, attempt.Segmentation);
		var backend = AiCvImportService.GetBackendStatus();
		AiImportProgressBackendTextBlock.Text = DescribeAiImportBackend(backend);

		var progress = new Progress<AiCvImportProgress>(report =>
		{
			Dispatcher.UIThread.Post(() => UpdateAiImportProgress(report, backend));
		});

		var existingPhoto = ProfilePhotoStorage.FileExists(_profilePhotoPath) ? _profilePhotoPath : null;
		var request = new AiCvImportRequest(
			attempt.NormalizedText,
			attempt.Segmentation,
			attempt.Deterministic.Success ? attempt.Deterministic : null,
			plan,
			_localizer.LanguageCode,
			mergeMode,
			attempt.Extraction.Warnings ?? [],
			existingPhoto,
			_aiImportCancellation.Token);

		AiCvImportOutcome outcome;
		try
		{
			outcome = await AiCvImportService.ImportAsync(request, progress);
		}
		catch (OperationCanceledException)
		{
			SetAiImportProgressVisible(false);
			_aiImportInFlight = false;
			return;
		}

		_aiImportInFlight = false;
		SetAiImportProgressVisible(false);

		if (!outcome.Succeeded || outcome.Result is null || outcome.ReviewSummary is null)
		{
			ExportStatusTextBlock.Text = _localizer.Get(outcome.ErrorMessageKey ?? TranslationKeys.ImportAiFailed);
			return;
		}

		_pendingAiImportOutcome = outcome;
		_pendingAiImportBaseline = attempt.Deterministic.Success ? attempt.Deterministic : null;
		_pendingAiImportMergeMode = mergeMode;
		ShowAiImportReview(outcome, backend, mergeMode);
	}

	private void UpdateAiImportProgress(AiCvImportProgress report, AiCvBackendStatus backend)
	{
		var phaseLabel = _localizer.Get(report.PhaseLabelKey);
		var batchSuffix = report.BatchCountInPhase > 1
			? " " + _localizer.Format(TranslationKeys.ImportAiProgressBatch, report.BatchIndex, report.BatchCountInPhase)
			: string.Empty;
		AiImportProgressStepTextBlock.Text =
			_localizer.Format(
				TranslationKeys.ImportAiProgressStep,
				report.CompletedBatches + 1,
				report.TotalBatches,
				phaseLabel + batchSuffix);
		AiImportProgressBackendTextBlock.Text = DescribeAiImportBackend(backend);
		var percent = report.TotalBatches == 0
			? 0
			: (double)(report.CompletedBatches + 1) / report.TotalBatches * 100;
		AiImportProgressBar.Value = percent;
	}

	private string DescribeAiImportBackend(AiCvBackendStatus backend)
	{
		if (backend.Snapshot is null)
		{
			return string.Empty;
		}

		if (backend.Kind == AiBackendKind.Local)
		{
			var model = backend.Snapshot.ModelLabel ?? _localizer.Get(TranslationKeys.AiSetupActiveAiLocal);
			return _localizer.Format(TranslationKeys.AiCvBackendLocal, model);
		}

		var provider = backend.Snapshot.DisplayNameKey is { } key
			? _localizer.Get(key)
			: _localizer.Get(TranslationKeys.AiSetupActiveAiOnline);
		return _localizer.Format(TranslationKeys.AiCvBackendOnline, provider);
	}

	private void ShowAiImportReview(AiCvImportOutcome outcome, AiCvBackendStatus backend, AiCvImportMergeMode mergeMode)
	{
		AiImportReviewBackendTextBlock.Text = DescribeAiImportBackend(backend);
		AiImportReviewWarningTextBlock.Text = _localizer.Get(TranslationKeys.ImportAiReviewWarning);
		AiImportReviewPhotoNoteTextBlock.Text = _localizer.Get(TranslationKeys.ImportAiPhotoNotExtracted);
		AiImportReviewPartialWarningTextBlock.IsVisible = outcome.BatchesFailed > 0;
		if (outcome.BatchesFailed > 0)
		{
			AiImportReviewPartialWarningTextBlock.Text = _localizer.Get(TranslationKeys.ImportAiPartialWarning);
		}

		AiImportReviewSummaryPanel.Children.Clear();
		foreach (var row in outcome.ReviewSummary!.Rows)
		{
			var grid = new Grid
			{
				ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
				Margin = new Thickness(0, 2, 0, 2),
			};
			grid.Children.Add(new TextBlock
			{
				Text = GetSectionLabel(row.SectionId),
				Classes = { "re-vitae-secondary" },
			});
			Grid.SetColumn(grid.Children[0], 0);
			grid.Children.Add(new TextBlock
			{
				Text = row.BeforeLabel,
				HorizontalAlignment = HorizontalAlignment.Right,
				Classes = { "re-vitae-secondary" },
			});
			Grid.SetColumn(grid.Children[1], 1);
			grid.Children.Add(new TextBlock
			{
				Text = row.AfterLabel,
				HorizontalAlignment = HorizontalAlignment.Right,
				Classes = { row.IsImproved ? "re-vitae-error" : "re-vitae-secondary" },
			});
			Grid.SetColumn(grid.Children[2], 2);
			AiImportReviewSummaryPanel.Children.Add(grid);
		}

		if (outcome.ReviewSummary.ImprovedSections.Count > 0)
		{
			var names = string.Join(
				", ",
				outcome.ReviewSummary.ImprovedSections.Select(GetSectionLabel));
			AiImportReviewImprovedTextBlock.Text =
				_localizer.Format(TranslationKeys.ImportAiReviewSectionsImproved, names);
			AiImportReviewImprovedTextBlock.IsVisible = true;
		}
		else
		{
			AiImportReviewImprovedTextBlock.IsVisible = false;
		}

		AiImportReviewMergeEmptyButton.IsVisible =
			mergeMode == AiCvImportMergeMode.FillEmptyOnly ||
			(_pendingAiImportBaseline?.Success == true && _isProjectDirty);
		AiImportReviewDetailsExpander.IsVisible = AiImportDiagnosticsLogger.IsEnabled;
		AiImportReviewDetailsTextBlock.Text = outcome.LastParseError ?? string.Empty;
		AiImportReviewOverlay.IsVisible = true;
	}

	private string GetSectionLabel(CvImportSectionId sectionId) =>
		sectionId switch
		{
			CvImportSectionId.PersonalInformation => _localizer.Get(TranslationKeys.MainPersonalInformation),
			CvImportSectionId.WorkExperience => _localizer.Get(TranslationKeys.WorkExperience),
			CvImportSectionId.Education => _localizer.Get(TranslationKeys.Education),
			CvImportSectionId.Skills => _localizer.Get(TranslationKeys.Skills),
			CvImportSectionId.Languages => _localizer.Get(TranslationKeys.Languages),
			CvImportSectionId.Certificates => _localizer.Get(TranslationKeys.Certificates),
			CvImportSectionId.Projects => _localizer.Get(TranslationKeys.Projects),
			CvImportSectionId.Links => _localizer.Get(TranslationKeys.Links),
			CvImportSectionId.AdditionalInformation => _localizer.Get(TranslationKeys.AdditionalInformation),
			_ => sectionId.ToString(),
		};

	private void OnAiImportReviewCancelClicked(object? sender, RoutedEventArgs e)
	{
		_pendingAiImportOutcome = null;
		_pendingAiImportBaseline = null;
		AiImportReviewOverlay.IsVisible = false;
	}

	private void OnAiImportReviewApplyClicked(object? sender, RoutedEventArgs e) =>
		ApplyPendingAiImport(AiCvImportMergeMode.ReplaceAll);

	private void OnAiImportReviewMergeEmptyClicked(object? sender, RoutedEventArgs e) =>
		ApplyPendingAiImport(AiCvImportMergeMode.FillEmptyOnly);

	private void ApplyPendingAiImport(AiCvImportMergeMode mergeMode)
	{
		if (_pendingAiImportOutcome?.Result is not { } aiFull)
		{
			AiImportReviewOverlay.IsVisible = false;
			return;
		}

		var existingPhoto = ProfilePhotoStorage.FileExists(_profilePhotoPath) ? _profilePhotoPath : null;
		var result = AiCvImportResultMerger.MergeForApply(
			aiFull,
			_pendingAiImportBaseline,
			mergeMode,
			existingPhoto);

		ApplyCvImportResult(result);
		ShowImportWarnings(result);
		ResetProjectSession(markDirty: true);
		HideImportAiEnhancePanel();
		UpdateValidationState();
		UpdatePreview();
		_pendingAiImportOutcome = null;
		_pendingAiImportBaseline = null;
		AiImportReviewOverlay.IsVisible = false;
	}

	private void OnAiImportProgressCancelClicked(object? sender, RoutedEventArgs e) =>
		_aiImportCancellation?.Cancel();

	private void SetAiImportProgressVisible(bool visible)
	{
		AiImportProgressOverlay.IsVisible = visible;
		if (visible)
		{
			AiImportProgressBar.Value = 0;
		}
	}

	private void UpdateTryAiImportButtons(bool introVisible, bool replaceVisible)
	{
		IntroImportTryAiButton.IsVisible = introVisible;
		ReplaceImportTryAiButton.IsVisible = replaceVisible;
	}

	private void UpdateImportAiEnhanceBanner(CvTextImportAttempt? attempt)
	{
		if (!ShouldShowAiPromotionsInUi())
		{
			HideImportAiEnhancePanel();
			return;
		}

		if (attempt is null || !AiCvImportTriggerEvaluator.ShouldOfferAi(attempt))
		{
			HideImportAiEnhancePanel();
			return;
		}

		ImportAiEnhanceBannerTextBlock.Text = _localizer.Get(TranslationKeys.ImportAiBannerIncomplete);
		ImportAiEnhancePanel.IsVisible = true;
		UpdateFixFieldsButtonVisibility(attempt);
	}

	private void HideImportAiEnhancePanel()
	{
		ImportAiEnhancePanel.IsVisible = false;
		ImportAiFixFieldsButton.IsVisible = false;
	}

	private void ApplyImportAiLocalization()
	{
		IntroImportTryAiButton.Content = _localizer.Get(TranslationKeys.IntroImportTryAi);
		ReplaceImportTryAiButton.Content = _localizer.Get(TranslationKeys.ReplaceImportTryAi);
		ImportAiEnhanceButton.Content = _localizer.Get(TranslationKeys.ImportAiEnhanceButton);
		ImportAiEnhanceBannerTextBlock.Text = _localizer.Get(TranslationKeys.ImportAiBannerIncomplete);
		ImportAiFixFieldsButton.Content = _localizer.Get(TranslationKeys.AiImportFixFields);
		AiRepairReviewTitleTextBlock.Text = _localizer.Get(TranslationKeys.AiImportRepairReviewTitle);
		AiRepairReviewApplyButton.Content = _localizer.Get(TranslationKeys.ImportAiReviewApply);
		AiRepairReviewCancelButton.Content = _localizer.Get(TranslationKeys.ImportAiReviewCancel);
		AiImportProgressTitleTextBlock.Text = _localizer.Get(TranslationKeys.ImportAiProgressTitle);
		AiImportProgressCancelButton.Content = _localizer.Get(TranslationKeys.Cancel);
		AiImportReviewTitleTextBlock.Text = _localizer.Get(TranslationKeys.ImportAiReviewTitle);
		AiImportReviewSummaryTitleTextBlock.Text = _localizer.Get(TranslationKeys.ImportAiReviewSummaryTitle);
		AiImportReviewApplyButton.Content = _localizer.Get(TranslationKeys.ImportAiReviewApply);
		AiImportReviewCancelButton.Content = _localizer.Get(TranslationKeys.ImportAiReviewCancel);
		AiImportReviewMergeEmptyButton.Content = _localizer.Get(TranslationKeys.ImportAiReviewMergeEmpty);
		AiImportReviewDetailsExpander.Header = _localizer.Get(TranslationKeys.ImportAiReviewDetails);
		AiImportOnlineConfirmCancelButton.Content = _localizer.Get(TranslationKeys.Cancel);
		AiImportOnlineConfirmOkButton.Content = _localizer.Get(TranslationKeys.Confirm);
	}
}
