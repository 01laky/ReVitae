using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Core.Projects;
using ReVitae.Core.Validation;
using ReVitae.Projects;
using ReVitae.Ui.Quality;

namespace ReVitae;

public partial class MainWindow
{
	private enum PendingProjectAction
	{
		None,
		NewCv,
		OpenProject,
		ImportReplace,
		CloseWindow,
		OpenRecent,
		SaveAsThenContinue
	}

	private readonly RecentProjectsStore _recentProjectsStore = new();
	private readonly CvProjectLifecycleService _projectLifecycle = new(
		SystemClock.Instance,
		new FileProjectAutosaveStore());
	private bool _isProjectDirty => _projectLifecycle.HasUnsavedChanges();
	private QualityHintSnackbarPresenter? _projectSnackbarPresenter;
	private PendingProjectAction _pendingProjectAction = PendingProjectAction.None;
	private string? _pendingRecentProjectPath;
	private string? _projectFilePath;
	private bool _suppressProjectDirtyTracking;
	private DispatcherTimer? _projectAutosaveTimer;

	private void InitializeProjectsUi()
	{
		_projectSnackbarPresenter = new QualityHintSnackbarPresenter(
			QualityHintSnackbarBorder,
			QualityHintSnackbarTextBlock);

		_projectAutosaveTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(CvProjectConstants.AutosaveIntervalSeconds)
		};
		_projectAutosaveTimer.Tick += (_, _) => TryWriteAutosaveRecovery();
		_projectAutosaveTimer.Start();

		RefreshIntroRecoveryPanel();
		RefreshIntroRecentProjects();
		UpdateWindowTitle();
	}

	private void MarkProjectDirty()
	{
		if (_suppressProjectDirtyTracking || _projectLifecycle.IsDirty)
		{
			return;
		}

		_projectLifecycle.MarkDirty();
		UpdateWindowTitle();
	}

	private void ClearProjectDirtyState()
	{
		_projectLifecycle.ClearDirty();
		UpdateWindowTitle();
	}

	private bool HasUnsavedProjectChanges() => _projectLifecycle.HasUnsavedChanges();

	private void ResetProjectSession(bool markDirty)
	{
		_projectFilePath = null;
		_projectLifecycle.SetDirty(markDirty);
		UpdateWindowTitle();
	}

	private void UpdateWindowTitle()
	{
		const string appName = "ReVitae";
		if (!string.IsNullOrWhiteSpace(_projectFilePath))
		{
			var fileName = Path.GetFileName(_projectFilePath);
			Title = _projectLifecycle.IsDirty ? $"* {fileName} — {appName}" : $"{fileName} — {appName}";
			return;
		}

		if (_projectLifecycle.IsDirty)
		{
			Title = $"* {_localizer.Get(TranslationKeys.ProjectUntitled)} — {appName}";
			return;
		}

		Title = appName;
	}

	private async void OnSaveProjectClicked(object? sender, RoutedEventArgs e)
	{
		if (IsProjectActionBlocked())
		{
			return;
		}

		await SaveProjectAsync(saveAs: string.IsNullOrWhiteSpace(_projectFilePath));
	}

	private async void OnSaveProjectAsClicked(object? sender, RoutedEventArgs e)
	{
		if (IsProjectActionBlocked())
		{
			return;
		}

		await SaveProjectAsync(saveAs: true);
	}

	private async void OnOpenProjectClicked(object? sender, RoutedEventArgs e)
	{
		if (IsProjectActionBlocked())
		{
			return;
		}

		if (!await TryContinueWithUnsavedPromptAsync(PendingProjectAction.OpenProject))
		{
			return;
		}

		await OpenProjectFromPickerAsync(closeIntro: false);
	}

	private async void OnIntroOpenProjectClicked(object? sender, RoutedEventArgs e)
	{
		await OpenProjectFromPickerAsync(closeIntro: true);
	}

	private async void OnIntroRecentProjectClicked(object? sender, RoutedEventArgs e)
	{
		if (sender is not Button { Tag: string path })
		{
			return;
		}

		await OpenProjectFromPathAsync(path, closeIntro: true);
	}

	private void OnIntroClearRecentProjectsClicked(object? sender, RoutedEventArgs e)
	{
		SetProjectRecentClearConfirmModalVisible(true);
	}

	private void OnProjectRecentClearConfirmCancelClicked(object? sender, RoutedEventArgs e)
	{
		SetProjectRecentClearConfirmModalVisible(false);
	}

	private void OnProjectRecentClearConfirmOkClicked(object? sender, RoutedEventArgs e)
	{
		SetProjectRecentClearConfirmModalVisible(false);
		_recentProjectsStore.Clear();
		RefreshIntroRecentProjects();
	}

	private async void OnIntroRecoveryRestoreClicked(object? sender, RoutedEventArgs e)
	{
		var result = CvProjectService.LoadRecovery();
		if (!result.Success || result.Import is null)
		{
			ShowProjectSnackbar(_localizer.Get(TranslationKeys.ProjectOpenFailed));
			IntroRecoveryPanel.IsVisible = false;
			CvProjectService.DeleteRecovery();
			return;
		}

		ApplyLoadedProject(result, recoveryPath: true);
		SetIntroModalVisible(false);
		IntroRecoveryPanel.IsVisible = false;
		ShowProjectSnackbar(_localizer.Get(TranslationKeys.ProjectRecoveryRestore));
	}

	private void OnIntroRecoveryDiscardClicked(object? sender, RoutedEventArgs e)
	{
		CvProjectService.DeleteRecovery();
		IntroRecoveryPanel.IsVisible = false;
		TryShowFirstLaunchAiWizardOnOpened();
	}

	private async void OnUnsavedChangesSaveClicked(object? sender, RoutedEventArgs e)
	{
		var pending = _pendingProjectAction;
		_pendingProjectAction = PendingProjectAction.None;
		SetUnsavedChangesConfirmModalVisible(false);

		var saved = await SaveProjectAsync(saveAs: string.IsNullOrWhiteSpace(_projectFilePath));
		if (!saved)
		{
			_pendingProjectAction = pending;
			return;
		}

		await ContinuePendingProjectActionAsync(pending);
	}

	private async void OnUnsavedChangesDiscardClicked(object? sender, RoutedEventArgs e)
	{
		var pending = _pendingProjectAction;
		_pendingProjectAction = PendingProjectAction.None;
		SetUnsavedChangesConfirmModalVisible(false);
		await ContinuePendingProjectActionAsync(pending);
	}

	private void OnUnsavedChangesCancelClicked(object? sender, RoutedEventArgs e)
	{
		_pendingProjectAction = PendingProjectAction.None;
		_pendingRecentProjectPath = null;
		SetUnsavedChangesConfirmModalVisible(false);
	}

	private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
	{
		if (!HasUnsavedProjectChanges())
		{
			return;
		}

		e.Cancel = true;
		_ = PromptUnsavedChangesForCloseAsync();
	}

	private async Task PromptUnsavedChangesForCloseAsync()
	{
		if (!await TryContinueWithUnsavedPromptAsync(PendingProjectAction.CloseWindow))
		{
			return;
		}

		_projectAutosaveTimer?.Stop();
		Close();
	}

	private bool IsProjectActionBlocked() =>
		IsBlockingOverlayOpen() || _isImportInProgress;

	private async Task<bool> TryContinueWithUnsavedPromptAsync(PendingProjectAction action)
	{
		if (!HasUnsavedProjectChanges())
		{
			return true;
		}

		_pendingProjectAction = action;
		SetUnsavedChangesConfirmModalVisible(true);
		return false;
	}

	private async Task ContinuePendingProjectActionAsync(PendingProjectAction action)
	{
		switch (action)
		{
			case PendingProjectAction.NewCv:
				StartNewCv();
				break;
			case PendingProjectAction.OpenProject:
				await OpenProjectFromPickerAsync(closeIntro: false);
				break;
			case PendingProjectAction.ImportReplace:
				await ImportCvFromFileAsync(replaceExisting: true, useIntroProgressUi: false, useReplaceProgressUi: true);
				break;
			case PendingProjectAction.CloseWindow:
				_projectAutosaveTimer?.Stop();
				Close();
				break;
			case PendingProjectAction.OpenRecent:
				if (!string.IsNullOrWhiteSpace(_pendingRecentProjectPath))
				{
					await OpenProjectFromPathAsync(_pendingRecentProjectPath, closeIntro: true);
				}

				_pendingRecentProjectPath = null;
				break;
			case PendingProjectAction.None:
			case PendingProjectAction.SaveAsThenContinue:
				break;
		}
	}

	private async Task OpenProjectFromPickerAsync(bool closeIntro)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		if (topLevel?.StorageProvider is null)
		{
			ShowProjectSnackbar(_localizer.Get(TranslationKeys.ExportFilePickerUnavailable));
			return;
		}

		var files = await topLevel.StorageProvider.OpenFilePickerAsync(
			CvProjectFilePickerOptions.CreateOpenOptions(_localizer));
		if (files.Count == 0 || files[0].TryGetLocalPath() is not { } filePath)
		{
			return;
		}

		if (CvOpenFileRouting.ShouldLoadAsSavedProject(filePath))
		{
			await OpenProjectFromPathAsync(filePath, closeIntro);
			return;
		}

		if (CvOpenFileRouting.IsImportableCvFile(filePath))
		{
			await ImportCvFromPathAsync(
				filePath,
				replaceExisting: !closeIntro,
				useIntroProgressUi: closeIntro,
				useReplaceProgressUi: !closeIntro,
				forceOcr: false);
			return;
		}

		ShowProjectSnackbar(_localizer.Get(TranslationKeys.ProjectOpenFailed));
	}

	private async Task OpenProjectFromPathAsync(string filePath, bool closeIntro)
	{
		var pathValidation = CvProjectPathValidator.ValidateOpenPath(filePath);
		if (!pathValidation.IsValid || pathValidation.NormalizedPath is null)
		{
			ShowProjectSnackbar(_localizer.Get(TranslationKeys.ProjectOpenFailed));
			return;
		}

		filePath = pathValidation.NormalizedPath;

		if (!File.Exists(filePath))
		{
			_recentProjectsStore.RemoveMissing(filePath);
			RefreshIntroRecentProjects();
			ShowProjectSnackbar(_localizer.Get(TranslationKeys.ProjectRecentMissing));
			return;
		}

		if (!closeIntro && HasUnsavedProjectChanges())
		{
			return;
		}

		var result = _projectLifecycle.LoadValidatedProject(filePath);
		if (!result.Success || result.Import is null)
		{
			ShowProjectSnackbar(_localizer.Get(result.ErrorMessageKey ?? TranslationKeys.ProjectOpenFailed));
			return;
		}

		ApplyLoadedProject(result, recoveryPath: false, filePath);
		if (closeIntro)
		{
			SetIntroModalVisible(false);
		}

		ShowProjectSnackbar(_localizer.Format(TranslationKeys.ProjectOpened, Path.GetFileName(filePath)));
		await Task.CompletedTask;
	}

	private void ApplyLoadedProject(CvProjectLoadResult result, bool recoveryPath, string? filePath = null)
	{
		_suppressProjectDirtyTracking = true;
		try
		{
			ResetQualityHintState();
			ApplyCvImportResult(result.Import!);
			_lastImportConfidences = [];

			var settings = result.Settings ?? CvProjectSettings.CreateDefault(_selectedTemplate);
			if (settings.SelectedTemplateId is { } templateId)
			{
				_selectedTemplate = templateId;
				UpdateTemplateSelectionState();
			}

			_qualityHintDismissalStore.Restore(settings.DismissedQualityHintKeys);
			ApplySectionExpandState(settings.SectionExpandState, result.Import!);

			_projectFilePath = recoveryPath ? null : filePath;
			_projectLifecycle.OnProjectLoaded(recoveryPath);
			UpdatePreview();
			UpdateValidationState();
			UpdateQualityHints();

			if (!recoveryPath && !string.IsNullOrWhiteSpace(filePath))
			{
				_recentProjectsStore.Add(filePath, BuildRecentDisplayName(result.Import!));
				RefreshIntroRecentProjects();
			}
		}
		finally
		{
			_suppressProjectDirtyTracking = false;
		}

		if (result.SettingsPartiallyIgnored)
		{
			ShowProjectSnackbar(_localizer.Get(TranslationKeys.ProjectSettingsPartiallyIgnored));
		}
	}

	private async Task<bool> SaveProjectAsync(bool saveAs)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		if (topLevel?.StorageProvider is null)
		{
			ShowProjectSnackbar(_localizer.Get(TranslationKeys.ExportFilePickerUnavailable));
			return false;
		}

		string? targetPath = _projectFilePath;
		if (saveAs || string.IsNullOrWhiteSpace(targetPath))
		{
			var source = BuildExportSourceData();
			var suggested = CvExportFilenameHelper.SuggestFilename(
				source.Personal.FirstName,
				source.Personal.LastName,
				CvExportFormat.RevitaeJson);
			if (string.IsNullOrWhiteSpace(source.Personal.FirstName)
				&& string.IsNullOrWhiteSpace(source.Personal.LastName))
			{
				suggested = "Untitled_CV.revitae.json";
			}

			var file = await topLevel.StorageProvider.SaveFilePickerAsync(
				CvProjectFilePickerOptions.CreateSaveOptions(_localizer, suggested));
			if (file?.TryGetLocalPath() is not { } pickedPath)
			{
				return false;
			}

			targetPath = pickedPath;
		}

		try
		{
			var saveValidation = CvProjectPathValidator.ValidateSavePath(targetPath);
			if (!saveValidation.IsValid || saveValidation.NormalizedPath is null)
			{
				ShowProjectSnackbar(_localizer.Get(TranslationKeys.ProjectSaveFailed));
				return false;
			}

			targetPath = saveValidation.NormalizedPath;
			var request = new CvProjectSaveRequest(
				BuildExportSourceData(),
				BuildProjectSettings());
			_projectLifecycle.SaveValidatedProject(targetPath, request);
			_projectFilePath = targetPath;
			_projectLifecycle.OnManualSaveSucceeded();
			_recentProjectsStore.Add(targetPath!, BuildRecentDisplayNameFromForm());
			RefreshIntroRecentProjects();
			ShowProjectSnackbar(_localizer.Format(TranslationKeys.ProjectSaved, Path.GetFileName(targetPath)));

			var validationResult = ValidateForm();
			if (!validationResult.IsValid)
			{
				ShowProjectSnackbar(_localizer.Get(TranslationKeys.ProjectSavedWithValidationErrors));
			}

			return true;
		}
		catch
		{
			ShowProjectSnackbar(_localizer.Get(TranslationKeys.ProjectSaveFailed));
			return false;
		}
	}

	private CvProjectSettings BuildProjectSettings() =>
		new(
			CvProjectConstants.CurrentProjectSettingsSchemaVersion,
			_selectedTemplate,
			_qualityHintDismissalStore.DismissedKeys.ToArray(),
			CaptureSectionExpandState(),
			DateTimeOffset.UtcNow,
			CvProjectApplicationInfo.Version);

	private Dictionary<string, bool> CaptureSectionExpandState() => new(StringComparer.Ordinal)
	{
		["personalInformation"] = PersonalInformationSection.IsExpanded,
		["workExperience"] = WorkExperienceSection.IsSectionExpanded,
		["education"] = EducationSection.IsSectionExpanded,
		["skills"] = SkillsSection.IsSectionExpanded,
		["languages"] = LanguagesSection.IsSectionExpanded,
		["certificates"] = CertificatesSection.IsSectionExpanded,
		["projects"] = ProjectsSection.IsSectionExpanded,
		["links"] = LinksSection.IsSectionExpanded,
		["additionalInformation"] = AdditionalInformationSection.IsSectionExpanded
	};

	private void ApplySectionExpandState(IReadOnlyDictionary<string, bool>? expandState, CvImportResult import)
	{
		if (expandState is null || expandState.Count == 0)
		{
			return;
		}

		PersonalInformationSection.IsExpanded = GetExpandState(expandState, "personalInformation", import, CvImportSectionId.PersonalInformation);
		WorkExperienceSection.SetSectionExpanded(GetExpandState(expandState, "workExperience", import, CvImportSectionId.WorkExperience));
		EducationSection.SetSectionExpanded(GetExpandState(expandState, "education", import, CvImportSectionId.Education));
		SkillsSection.SetSectionExpanded(GetExpandState(expandState, "skills", import, CvImportSectionId.Skills));
		LanguagesSection.SetSectionExpanded(GetExpandState(expandState, "languages", import, CvImportSectionId.Languages));
		CertificatesSection.SetSectionExpanded(GetExpandState(expandState, "certificates", import, CvImportSectionId.Certificates));
		ProjectsSection.SetSectionExpanded(GetExpandState(expandState, "projects", import, CvImportSectionId.Projects));
		LinksSection.SetSectionExpanded(GetExpandState(expandState, "links", import, CvImportSectionId.Links));
		AdditionalInformationSection.SetSectionExpanded(GetExpandState(expandState, "additionalInformation", import, CvImportSectionId.AdditionalInformation));
	}

	private static bool GetExpandState(
		IReadOnlyDictionary<string, bool> expandState,
		string key,
		CvImportResult import,
		CvImportSectionId sectionId) =>
		expandState.TryGetValue(key, out var expanded)
			? expanded
			: import.SectionHasData.TryGetValue(sectionId, out var hasData) && hasData;

	private string BuildRecentDisplayName(CvImportResult import)
	{
		var first = import.Personal.FirstName?.Trim();
		var last = import.Personal.LastName?.Trim();
		if (!string.IsNullOrWhiteSpace(first) || !string.IsNullOrWhiteSpace(last))
		{
			return $"{first} {last}".Trim();
		}

		return _localizer.Get(TranslationKeys.ProjectUntitled);
	}

	private string BuildRecentDisplayNameFromForm()
	{
		var first = NormalizeValue(FirstNameTextBox.Text);
		var last = NormalizeValue(LastNameTextBox.Text);
		if (!string.IsNullOrWhiteSpace(first) || !string.IsNullOrWhiteSpace(last))
		{
			return $"{first} {last}".Trim();
		}

		if (!string.IsNullOrWhiteSpace(_projectFilePath))
		{
			return Path.GetFileNameWithoutExtension(_projectFilePath);
		}

		return _localizer.Get(TranslationKeys.ProjectUntitled);
	}

	private void TryWriteAutosaveRecovery()
	{
		var result = _projectLifecycle.TryWriteAutosaveRecovery(
			new CvProjectSaveRequest(BuildExportSourceData(), BuildProjectSettings()),
			HasCvFormData());
		if (result.Status == AutosaveWriteStatus.Failed)
		{
			// Recovery writes must never interrupt editing.
		}
	}

	private void RefreshIntroRecoveryPanel()
	{
		IntroRecoveryPanel.IsVisible = _projectLifecycle.RecoveryExists();
	}

	private void RefreshIntroRecentProjects()
	{
		IntroRecentProjectsPanel.Children.Clear();
		var recents = _recentProjectsStore.Load();
		IntroRecentProjectsPanel.IsVisible = recents.Count > 0;
		IntroRecentTitleTextBlock.IsVisible = recents.Count > 0;
		IntroClearRecentProjectsButton.IsVisible = recents.Count > 0;

		foreach (var entry in recents)
		{
			var button = new Button
			{
				Classes = { "re-vitae-secondary" },
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Tag = entry.Path
			};
			button.Click += OnIntroRecentProjectClicked;

			var panel = new StackPanel { Spacing = 2 };
			panel.Children.Add(new TextBlock
			{
				Text = entry.DisplayName,
				Classes = { "re-vitae-app-title" },
				FontSize = 14
			});
			panel.Children.Add(new TextBlock
			{
				Text = entry.Path,
				Classes = { "re-vitae-secondary" },
				FontSize = 12,
				TextWrapping = TextWrapping.Wrap
			});
			button.Content = panel;
			IntroRecentProjectsPanel.Children.Add(button);
		}
	}

	private void ShowProjectSnackbar(string message) => _projectSnackbarPresenter?.Show(message);

	private void SetUnsavedChangesConfirmModalVisible(bool isVisible)
	{
		UnsavedChangesConfirmModalOverlay.IsVisible = isVisible;
	}

	private void SetProjectRecentClearConfirmModalVisible(bool isVisible)
	{
		ProjectRecentClearConfirmModalOverlay.IsVisible = isVisible;
	}

	private void HandleProjectKeyboardShortcut(KeyEventArgs e)
	{
		var modifier = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
		if (e.KeyModifiers != modifier)
		{
			return;
		}

		if (e.Key == Key.S)
		{
			if (IsProjectActionBlocked())
			{
				return;
			}

			e.Handled = true;
			_ = SaveProjectAsync(saveAs: string.IsNullOrWhiteSpace(_projectFilePath));
			return;
		}

		if (e.Key == Key.O)
		{
			if (IsProjectActionBlocked())
			{
				return;
			}

			e.Handled = true;
			_ = OnOpenProjectShortcutAsync();
		}
	}

	private async Task OnOpenProjectShortcutAsync()
	{
		if (!await TryContinueWithUnsavedPromptAsync(PendingProjectAction.OpenProject))
		{
			return;
		}

		await OpenProjectFromPickerAsync(closeIntro: false);
	}
}
