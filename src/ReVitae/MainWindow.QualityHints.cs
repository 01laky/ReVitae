using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using ReVitae.Core.Cv;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;
using ReVitae.Ui.Quality;

namespace ReVitae;

public partial class MainWindow
{
	private readonly QualityHintDismissalStore _qualityHintDismissalStore = new();
	private SectionHeaderBadges? _personalHeaderBadges;
	private IReadOnlyList<ImportedFieldConfidence> _lastImportConfidences = [];
	private QualityHintSnackbarPresenter? _qualitySnackbarPresenter;
	private QualityHintModalPresenter? _qualityHintModalPresenter;
	private bool _qualityFlyoutOpenedThisSession;
	private bool _qualityFirstSessionSnackbarShown;
	private bool _showQualitySnackbarAfterImport;
	private CvQualityReport _lastQualityReport = new([]);

	private void InitializeQualityHintsUi()
	{
		_personalHeaderBadges = new SectionHeaderBadges();
		PersonalInformationSection.HeaderActions = _personalHeaderBadges.Root;

		_qualitySnackbarPresenter = new QualityHintSnackbarPresenter(
			QualityHintSnackbarBorder,
			QualityHintSnackbarTextBlock);

		_qualityHintModalPresenter = new QualityHintModalPresenter(
			QualityHintModalOverlay,
			QualityHintModalTitleTextBlock,
			QualityHintModalContentPanel);
		QualityHintFlyoutHelper.RegisterPresenter(_qualityHintModalPresenter);
	}

	private void SetQualityHintModalVisible(bool isVisible)
	{
		QualityHintModalOverlay.IsVisible = isVisible;
	}

	private void OnCloseQualityHintModalClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
		SetQualityHintModalVisible(false);

	private void ResetQualityHintState()
	{
		_qualityHintDismissalStore.Clear();
		_lastImportConfidences = [];
		_showQualitySnackbarAfterImport = false;
		_qualitySnackbarPresenter?.Hide();
	}

	private void UpdateQualityHints()
	{
		RefreshAdvisorVisibility();
		var snapshot = BuildExportSourceData();
		var options = new CvQualityAnalysisOptions(
			_lastImportConfidences,
			_qualityHintDismissalStore.DismissedKeys);
		_lastQualityReport = CvQualityAnalyzer.Analyze(snapshot, options);
		var hints = _lastQualityReport.Hints;

		ApplyPersonalQualityHints(FilterHints(CvImportSectionId.PersonalInformation, hints));
		WorkExperienceSection.ApplyQualityHints(
			FilterHints(CvImportSectionId.WorkExperience, hints),
			_localizer,
			NavigateToQualityHint,
			OnQualityHintDismissed,
			MarkQualityFlyoutOpened);
		EducationSection.ApplyQualityHints(
			FilterHints(CvImportSectionId.Education, hints),
			_localizer,
			NavigateToQualityHint,
			OnQualityHintDismissed,
			MarkQualityFlyoutOpened);
		SkillsSection.ApplyQualityHints(
			FilterHints(CvImportSectionId.Skills, hints),
			_localizer,
			NavigateToQualityHint,
			OnQualityHintDismissed,
			MarkQualityFlyoutOpened);
		LanguagesSection.ApplyQualityHints(
			FilterHints(CvImportSectionId.Languages, hints),
			_localizer,
			NavigateToQualityHint,
			OnQualityHintDismissed,
			MarkQualityFlyoutOpened);
		CertificatesSection.ApplyQualityHints(
			FilterHints(CvImportSectionId.Certificates, hints),
			_localizer,
			NavigateToQualityHint,
			OnQualityHintDismissed,
			MarkQualityFlyoutOpened);
		ProjectsSection.ApplyQualityHints(
			FilterHints(CvImportSectionId.Projects, hints),
			_localizer,
			NavigateToQualityHint,
			OnQualityHintDismissed,
			MarkQualityFlyoutOpened);
		LinksSection.ApplyQualityHints(
			FilterHints(CvImportSectionId.Links, hints),
			_localizer,
			NavigateToQualityHint,
			OnQualityHintDismissed,
			MarkQualityFlyoutOpened);
		AdditionalInformationSection.ApplyQualityHints(
			FilterHints(CvImportSectionId.AdditionalInformation, hints),
			_localizer,
			NavigateToQualityHint,
			OnQualityHintDismissed,
			MarkQualityFlyoutOpened);

		UpdateQualityExportSummary(hints.Count);
		MaybeShowQualitySnackbar(hints.Count);
		_showQualitySnackbarAfterImport = false;
	}

	private void ApplyPersonalQualityHints(IReadOnlyList<CvQualityHint> hints)
	{
		if (_personalHeaderBadges is null)
		{
			return;
		}

		QualityHintsService.UpdateSectionQualityBadge(
			_personalHeaderBadges.QualityBadgePanel,
			_personalHeaderBadges.QualityBadgeTextBlock,
			hints,
			_localizer,
			_localizer.Get(TranslationKeys.MainPersonalInformation),
			NavigateToQualityHint,
			OnQualityHintDismissed,
			MarkQualityFlyoutOpened);
	}

	private static IReadOnlyList<CvQualityHint> FilterHints(
		CvImportSectionId section,
		IReadOnlyList<CvQualityHint> hints) =>
		hints.Where(hint => hint.Section == section).ToArray();

	private bool NavigateToQualityHint(CvQualityHint hint)
	{
		if (string.IsNullOrEmpty(hint.FieldKey))
		{
			return false;
		}

		if (IsPersonalFieldKey(hint.FieldKey))
		{
			PersonalInformationSection.IsExpanded = true;
			var control = FindPersonalControlForFieldKey(hint.FieldKey);
			control?.Focus();
			return control is not null;
		}

		return ExpandAndRevealField(hint.FieldKey);
	}

	private Control? FindPersonalControlForFieldKey(string fieldKey) =>
		fieldKey switch
		{
			MainPersonalInformationFieldKeys.FirstName => FirstNameTextBox,
			MainPersonalInformationFieldKeys.LastName => LastNameTextBox,
			MainPersonalInformationFieldKeys.ProfessionalTitle => ProfessionalTitleTextBox,
			MainPersonalInformationFieldKeys.Email => EmailTextBox,
			MainPersonalInformationFieldKeys.Phone => PhoneTextBox,
			MainPersonalInformationFieldKeys.Location => LocationTextBox,
			MainPersonalInformationFieldKeys.LinkedInUrl => LinkedInUrlTextBox,
			MainPersonalInformationFieldKeys.PortfolioUrl => PortfolioUrlTextBox,
			MainPersonalInformationFieldKeys.GitHubUrl => GitHubUrlTextBox,
			MainPersonalInformationFieldKeys.ShortSummary => ShortSummaryTextBox,
			_ => null
		};

	private void OnQualityHintDismissed(CvQualityHint hint)
	{
		_qualityHintDismissalStore.Dismiss(hint);
		MarkProjectDirty();
		UpdateQualityHints();
	}

	private void MarkQualityFlyoutOpened() => _qualityFlyoutOpenedThisSession = true;

	private void UpdateQualityExportSummary(int hintCount)
	{
		QualityHintExportSummaryPanel.IsVisible = hintCount > 0;
		QualityHintExportSummaryTextBlock.Text = hintCount > 0
			? _localizer.Format(TranslationKeys.QualityHintExportSummary, hintCount)
			: string.Empty;
	}

	private void MaybeShowQualitySnackbar(int hintCount)
	{
		if (_qualitySnackbarPresenter is null || hintCount == 0)
		{
			return;
		}

		if (_showQualitySnackbarAfterImport)
		{
			_qualitySnackbarPresenter.Show(_localizer.Get(TranslationKeys.QualityHintSnackbarAfterImport));
			return;
		}

		if (!_qualityFirstSessionSnackbarShown
			&& !_qualityFlyoutOpenedThisSession
			&& hintCount >= 3)
		{
			_qualityFirstSessionSnackbarShown = true;
			_qualitySnackbarPresenter.Show(_localizer.Get(TranslationKeys.QualityHintSnackbarFirstSession));
		}
	}

	private void OnQualityHintExportReviewClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		var firstSection = _lastQualityReport.Hints
			.Select(hint => hint.Section)
			.FirstOrDefault(section => section.HasValue);

		if (!firstSection.HasValue)
		{
			return;
		}

		ExpandSectionForQualityHint(firstSection.Value);
	}

	private void ExpandSectionForQualityHint(CvImportSectionId section)
	{
		switch (section)
		{
			case CvImportSectionId.PersonalInformation:
				PersonalInformationSection.IsExpanded = true;
				break;
			case CvImportSectionId.WorkExperience:
				WorkExperienceSection.SetSectionExpanded(true);
				break;
			case CvImportSectionId.Education:
				EducationSection.SetSectionExpanded(true);
				break;
			case CvImportSectionId.Skills:
				SkillsSection.SetSectionExpanded(true);
				break;
			case CvImportSectionId.Languages:
				LanguagesSection.SetSectionExpanded(true);
				break;
			case CvImportSectionId.Certificates:
				CertificatesSection.SetSectionExpanded(true);
				break;
			case CvImportSectionId.Projects:
				ProjectsSection.SetSectionExpanded(true);
				break;
			case CvImportSectionId.Links:
				LinksSection.SetSectionExpanded(true);
				break;
			case CvImportSectionId.AdditionalInformation:
				AdditionalInformationSection.SetSectionExpanded(true);
				break;
		}

		FormScrollViewer.Offset = new Avalonia.Vector(0, FormScrollViewer.Offset.Y);
	}
}
