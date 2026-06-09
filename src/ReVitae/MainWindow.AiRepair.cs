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
using ReVitae.Core.Ai.Import;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Cv;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Ui;

namespace ReVitae;

public partial class MainWindow
{
	private AiCvImportFieldRepairService? _aiRepairService;
	private CancellationTokenSource? _aiRepairCts;
	private bool _aiRepairInFlight;
	private IReadOnlyList<AiImportFieldRepairResult> _pendingRepairs = [];

	private AiCvImportFieldRepairService AiRepair =>
		_aiRepairService ??= new AiCvImportFieldRepairService(
			_aiProviderConfigService,
			new AiBackendRuntimeResolver());

	/// <summary>Personal-info fields that map cleanly to a form TextBox (045 B.2).</summary>
	private IReadOnlyDictionary<string, (TextBox Box, string LabelKey)> PersonalFieldMap =>
		new Dictionary<string, (TextBox, string)>(StringComparer.Ordinal)
		{
			[MainPersonalInformationFieldKeys.FirstName] = (FirstNameTextBox, TranslationKeys.FirstName),
			[MainPersonalInformationFieldKeys.LastName] = (LastNameTextBox, TranslationKeys.LastName),
			[MainPersonalInformationFieldKeys.ProfessionalTitle] = (ProfessionalTitleTextBox, TranslationKeys.ProfessionalTitle),
			[MainPersonalInformationFieldKeys.Email] = (EmailTextBox, TranslationKeys.Email),
			[MainPersonalInformationFieldKeys.Phone] = (PhoneTextBox, TranslationKeys.Phone),
			[MainPersonalInformationFieldKeys.Location] = (LocationTextBox, TranslationKeys.Location),
			[MainPersonalInformationFieldKeys.LinkedInUrl] = (LinkedInUrlTextBox, TranslationKeys.LinkedInUrl),
			[MainPersonalInformationFieldKeys.PortfolioUrl] = (PortfolioUrlTextBox, TranslationKeys.PortfolioUrl),
			[MainPersonalInformationFieldKeys.GitHubUrl] = (GitHubUrlTextBox, TranslationKeys.GitHubUrl),
			[MainPersonalInformationFieldKeys.ShortSummary] = (ShortSummaryTextBox, TranslationKeys.ShortSummary),
		};

	internal bool TryApplyPersonalField(string fieldKey, string text)
	{
		if (PersonalFieldMap.TryGetValue(fieldKey, out var entry))
		{
			entry.Box.Text = text;
			return true;
		}

		return false;
	}

	private string? TryReadPersonalField(string fieldKey) =>
		PersonalFieldMap.TryGetValue(fieldKey, out var entry) ? entry.Box.Text : null;

	/// <summary>
	/// Shows the "Fix fields with AI" button when the last import left resolvable
	/// low-confidence personal fields and a backend is active (045 B.1/B.2).
	/// </summary>
	private void UpdateFixFieldsButtonVisibility(CvTextImportAttempt? attempt)
	{
		var backendActive = ActiveBackendService.GetActiveSnapshot().Kind != AiBackendKind.None;
		var hasTargets = backendActive && attempt is not null && BuildPersonalRepairTargets(attempt).Count > 0;
		ImportAiFixFieldsButton.IsVisible = hasTargets;
		ImportAiFixFieldsButton.Content = _localizer.Get(TranslationKeys.AiImportFixFields);
	}

	private List<AiImportFieldRepairTarget> BuildPersonalRepairTargets(CvTextImportAttempt attempt)
	{
		var targets = new List<AiImportFieldRepairTarget>();
		foreach (var confidence in attempt.Deterministic.FieldConfidences)
		{
			if (confidence.Confidence != CvImportConfidence.Low)
			{
				continue;
			}

			var current = TryReadPersonalField(confidence.FieldKey);
			if (string.IsNullOrWhiteSpace(current))
			{
				continue; // unresolvable or empty → skip (do not guess)
			}

			targets.Add(new AiImportFieldRepairTarget(
				CvImportSectionId.PersonalInformation,
				confidence.FieldKey,
				null,
				current,
				confidence.Confidence));
		}

		return targets;
	}

	private async void OnImportAiFixFieldsClicked(object? sender, RoutedEventArgs e)
	{
		if (_aiRepairInFlight || _lastTextImportAttempt is null)
		{
			return;
		}

		var backend = ActiveBackendService.GetActiveSnapshot();
		if (backend.Kind == AiBackendKind.Online && !_onlineCvSendConfirmedThisSession)
		{
			// Reuse the import online-send confirm before the first online repair.
			_onlineCvSendConfirmedThisSession = true;
		}

		await RunFieldRepairAsync(_lastTextImportAttempt).ConfigureAwait(true);
	}

	private async Task RunFieldRepairAsync(CvTextImportAttempt attempt)
	{
		var targets = BuildPersonalRepairTargets(attempt);
		if (targets.Count == 0)
		{
			return;
		}

		_aiRepairInFlight = true;
		_aiRepairCts?.Cancel();
		_aiRepairCts?.Dispose();
		_aiRepairCts = new CancellationTokenSource();

		try
		{
			var outcome = await AiRepair.RepairImportFieldsAsync(
				attempt,
				targets,
				_localizer.LanguageCode,
				_aiRepairCts.Token).ConfigureAwait(true);

			if (outcome.Cancelled)
			{
				return;
			}

			if (!outcome.Succeeded)
			{
				QualityHintSnackbarTextBlock.Text = _localizer.Get(outcome.ErrorMessageKey ?? TranslationKeys.ImportAiFailed);
				QualityHintSnackbarBorder.IsVisible = true;
				return;
			}

			ShowRepairReview(outcome);
		}
		finally
		{
			_aiRepairInFlight = false;
		}
	}

	private void ShowRepairReview(AiCvImportRepairOutcome outcome)
	{
		_pendingRepairs = outcome.Repairs.Where(r => r.Changed).ToList();
		AiRepairReviewRowsPanel.Children.Clear();

		AiRepairReviewTitleTextBlock.Text = _localizer.Get(TranslationKeys.AiImportRepairReviewTitle);
		AiRepairReviewBackendTextBlock.Text = outcome.BackendUsed?.Label ?? string.Empty;
		AiRepairReviewApplyButton.Content = _localizer.Get(TranslationKeys.ImportAiReviewApply);
		AiRepairReviewCancelButton.Content = _localizer.Get(TranslationKeys.ImportAiReviewCancel);

		foreach (var repair in _pendingRepairs)
		{
			AiRepairReviewRowsPanel.Children.Add(BuildRepairRow(repair));
		}

		if (_pendingRepairs.Count == 0)
		{
			var none = new TextBlock { Text = _localizer.Get(TranslationKeys.AiCvAdvisorEmpty), TextWrapping = TextWrapping.Wrap };
			none.Classes.Add(UiClasses.SecondaryText);
			AiRepairReviewRowsPanel.Children.Add(none);
		}

		if (outcome.DroppedFieldCount > 0)
		{
			AiRepairReviewMoreTextBlock.Text = _localizer.Format(TranslationKeys.AiImportRepairMore, outcome.DroppedFieldCount);
			AiRepairReviewMoreTextBlock.IsVisible = true;
		}
		else
		{
			AiRepairReviewMoreTextBlock.IsVisible = false;
		}

		AiRepairReviewApplyButton.IsEnabled = _pendingRepairs.Count > 0;
		AiRepairReviewOverlay.IsVisible = true;
	}

	private Control BuildRepairRow(AiImportFieldRepairResult repair)
	{
		var label = PersonalFieldMap.TryGetValue(repair.Target.FieldKey, out var entry)
			? _localizer.Get(entry.LabelKey)
			: repair.Target.FieldKey;

		var text = new TextBlock
		{
			Text = _localizer.Format(TranslationKeys.AiImportRepairField, label, repair.Target.CurrentValue, repair.RepairedValue),
			TextWrapping = TextWrapping.Wrap,
			FontSize = 14,
		};
		return text;
	}

	private void OnAiRepairReviewApplyClicked(object? sender, RoutedEventArgs e)
	{
		var snapshots = new List<AiCvFieldValueSnapshot>();
		foreach (var repair in _pendingRepairs)
		{
			var prior = TryReadPersonalField(repair.Target.FieldKey) ?? string.Empty;
			if (TryApplyPersonalField(repair.Target.FieldKey, repair.RepairedValue))
			{
				snapshots.Add(new AiCvFieldValueSnapshot(
					new AiCvFieldTarget(CvImportSectionId.PersonalInformation, repair.Target.FieldKey),
					prior));
			}
		}

		AiRepairReviewOverlay.IsVisible = false;

		if (snapshots.Count > 0)
		{
			_aiCvUndoBuffer.Capture(snapshots);
			ShowAiCvUndoBar();
		}

		UpdateValidationState();
		UpdateQualityHints();
	}

	private void OnAiRepairReviewCancelClicked(object? sender, RoutedEventArgs e)
	{
		_aiRepairCts?.Cancel();
		AiRepairReviewOverlay.IsVisible = false;
	}
}
