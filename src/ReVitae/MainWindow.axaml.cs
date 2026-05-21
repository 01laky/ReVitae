using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using ReVitae.Core;
using ReVitae.Core.Cv;
using ReVitae.Core.Cv.AdditionalInformation;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Import;
using ReVitae.Core.Localization;
using ReVitae.Controls;
using ReVitae.Core.Validation;
using ReVitae.Core.Validation.Presentation;
using ReVitae.Export;
using ReVitae.Preview;
using ReVitae.Ui;
using ReVitae.Ui.Validation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExportWorkExperienceEntry = ReVitae.Core.Export.WorkExperienceEntry;
using ExportEducationEntry = ReVitae.Core.Export.EducationEntry;
using ExportSkillItem = ReVitae.Core.Export.SkillItem;
using ExportSkillsGroup = ReVitae.Core.Export.SkillsGroup;
using ExportLanguageEntry = ReVitae.Core.Export.LanguageEntry;
using ExportCertificateEntry = ReVitae.Core.Export.CertificateEntry;
using ExportProjectEntry = ReVitae.Core.Export.ProjectEntry;

namespace ReVitae;

public partial class MainWindow : Window
{
    private const double TemplateContentPadding = 18;

    private readonly FieldValidator _validator = MainPersonalInformationSchema.CreateValidator();
    private readonly WorkExperienceCollectionValidator _workExperienceValidator = new();
    private readonly EducationCollectionValidator _educationValidator = new();
    private readonly SkillsCollectionValidator _skillsValidator = new();
    private readonly LanguagesCollectionValidator _languagesValidator = new();
    private readonly CertificatesCollectionValidator _certificatesValidator = new();
    private readonly ProjectsCollectionValidator _projectsValidator = new();
    private readonly LinksCollectionValidator _linksValidator = new();
    private readonly AdditionalInformationValidator _additionalInformationValidator = new();
    private readonly ValidationTouchTracker _validationTouchTracker = new();
    private readonly ValidationFieldRegistry _personalValidationRegistry = new();
    private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
    private bool _isImportInProgress;
    private bool _isUpdatingLanguageSelection;
    private CvExportTemplateId _selectedTemplate = CvExportTemplateId.CleanTopHeader;
    private StackPanel? _personalSectionErrorBadgePanel;
    private TextBlock? _personalSectionErrorBadgeTextBlock;
    private int _personalSectionErrorCount;
    private string? _lastExportedFilePath;

    public MainWindow()
    {
        InitializeComponent();
        InitializePersonalValidation();
        InitializeLanguageSelector();
        WorkExperienceSection.EntriesChanged += OnWorkExperienceChanged;
        EducationSection.EntriesChanged += OnEducationChanged;
        SkillsSection.EntriesChanged += OnSkillsChanged;
        LanguagesSection.EntriesChanged += OnLanguagesChanged;
        CertificatesSection.EntriesChanged += OnCertificatesChanged;
        ProjectsSection.EntriesChanged += OnProjectsChanged;
        LinksSection.EntriesChanged += OnLinksChanged;
        AdditionalInformationSection.ContentChanged += OnAdditionalInformationChanged;
        WireValidationRefreshOnSectionExpand(PersonalInformationSection);
        WireValidationRefreshOnSectionExpand(WorkExperienceSection);
        WireValidationRefreshOnSectionExpand(EducationSection);
        WireValidationRefreshOnSectionExpand(SkillsSection);
        WireValidationRefreshOnSectionExpand(LanguagesSection);
        WireValidationRefreshOnSectionExpand(CertificatesSection);
        WireValidationRefreshOnSectionExpand(ProjectsSection);
        WireValidationRefreshOnSectionExpand(LinksSection);
        WireValidationRefreshOnSectionExpand(AdditionalInformationSection);
        ApplyLocalization();
        RefreshProfilePhotoUi();
        UpdateTemplateSelectionState();
        UpdatePreview();
        UpdateValidationState();
    }

    private void WireValidationRefreshOnSectionExpand(ExpandableSection section)
    {
        section.ExpandStateChanged += (_, _) => UpdateValidationState();
    }

    private void WireValidationRefreshOnSectionExpand(WorkExperience.WorkExperienceSectionView section)
    {
        section.ExpandStateChanged += (_, _) => UpdateValidationState();
    }

    private void WireValidationRefreshOnSectionExpand(Education.EducationSectionView section)
    {
        section.ExpandStateChanged += (_, _) => UpdateValidationState();
    }

    private void WireValidationRefreshOnSectionExpand(Skills.SkillsSectionView section)
    {
        section.ExpandStateChanged += (_, _) => UpdateValidationState();
    }

    private void WireValidationRefreshOnSectionExpand(Languages.LanguagesSectionView section)
    {
        section.ExpandStateChanged += (_, _) => UpdateValidationState();
    }

    private void WireValidationRefreshOnSectionExpand(Certificates.CertificatesSectionView section)
    {
        section.ExpandStateChanged += (_, _) => UpdateValidationState();
    }

    private void WireValidationRefreshOnSectionExpand(Projects.ProjectsSectionView section)
    {
        section.ExpandStateChanged += (_, _) => UpdateValidationState();
    }

    private void WireValidationRefreshOnSectionExpand(Links.LinksSectionView section)
    {
        section.ExpandStateChanged += (_, _) => UpdateValidationState();
    }

    private void WireValidationRefreshOnSectionExpand(AdditionalInformation.AdditionalInformationSectionView section)
    {
        section.ExpandStateChanged += (_, _) => UpdateValidationState();
    }

    private void InitializeLanguageSelector()
    {
        _isUpdatingLanguageSelection = true;
        LanguageComboBox.ItemsSource = AppLocalizer.SupportedLanguages;
        LanguageComboBox.SelectedItem = AppLocalizer.SupportedLanguages
            .First(language => language.Code == _localizer.LanguageCode);
        _isUpdatingLanguageSelection = false;
    }

    private void OnWorkExperienceChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
        UpdateValidationState();
        ExportStatusTextBlock.Text = string.Empty;
    }

    private void OnEducationChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
        UpdateValidationState();
        ExportStatusTextBlock.Text = string.Empty;
    }

    private void OnSkillsChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
        UpdateValidationState();
        ExportStatusTextBlock.Text = string.Empty;
    }

    private void OnLanguagesChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
        UpdateValidationState();
        ExportStatusTextBlock.Text = string.Empty;
    }

    private void OnCertificatesChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
        UpdateValidationState();
        ExportStatusTextBlock.Text = string.Empty;
    }

    private void OnProjectsChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
        UpdateValidationState();
        ExportStatusTextBlock.Text = string.Empty;
    }

    private void OnLinksChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
        UpdateValidationState();
        ExportStatusTextBlock.Text = string.Empty;
    }

    private void OnAdditionalInformationChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
        UpdateValidationState();
        ExportStatusTextBlock.Text = string.Empty;
    }

    private void OnFormTextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdatePreview();
        UpdateValidationState();
        ExportStatusTextBlock.Text = string.Empty;
    }

    private void OnOpenSetupClicked(object? sender, RoutedEventArgs e)
    {
        if (IntroModalOverlay.IsVisible || ReplaceCvImportProgressModalOverlay.IsVisible)
        {
            return;
        }

        SetSetupModalVisible(true);
    }

    private void OnOpenTemplatesClicked(object? sender, RoutedEventArgs e)
    {
        if (IntroModalOverlay.IsVisible || ReplaceCvImportProgressModalOverlay.IsVisible)
        {
            return;
        }

        UpdateTemplateSelectionState();
        SetTemplatesModalVisible(true);
    }

    private void OnOpenPreviewExpandClicked(object? sender, RoutedEventArgs e)
    {
        if (IntroModalOverlay.IsVisible || ReplaceCvImportProgressModalOverlay.IsVisible)
        {
            return;
        }

        UpdatePreview();
        SetPreviewExpandModalVisible(true);
    }

    private void OnCreateNewCvClicked(object? sender, RoutedEventArgs e)
    {
        SetIntroModalVisible(false);
        StartNewCv();
    }

    private void OnOpenCreateNewCvClicked(object? sender, RoutedEventArgs e)
    {
        if (IntroModalOverlay.IsVisible || ReplaceCvImportProgressModalOverlay.IsVisible || _isImportInProgress)
        {
            return;
        }

        if (HasCvFormData())
        {
            SetNewCvConfirmModalVisible(true);
            return;
        }

        StartNewCv();
    }

    private void OnNewCvConfirmCancelClicked(object? sender, RoutedEventArgs e)
    {
        SetNewCvConfirmModalVisible(false);
    }

    private void OnNewCvConfirmOkClicked(object? sender, RoutedEventArgs e)
    {
        SetNewCvConfirmModalVisible(false);
        StartNewCv();
    }

    private void StartNewCv()
    {
        ClearCvForm();
        HideExportPostActions();
        ExportStatusTextBlock.Text = string.Empty;
        FormScrollViewer.Offset = new Vector(0, 0);
        UpdatePreview();
        UpdateValidationState();
    }

    private async void OnUploadCvClicked(object? sender, RoutedEventArgs e)
    {
        if (IntroModalOverlay.IsVisible || _isImportInProgress)
        {
            return;
        }

        if (HasCvFormData())
        {
            SetReplaceCvConfirmModalVisible(true);
            return;
        }

        await ImportCvFromFileAsync(replaceExisting: false, useIntroProgressUi: false, useReplaceProgressUi: true);
    }

    private void OnReplaceCvConfirmCancelClicked(object? sender, RoutedEventArgs e)
    {
        SetReplaceCvConfirmModalVisible(false);
    }

    private async void OnReplaceCvConfirmOkClicked(object? sender, RoutedEventArgs e)
    {
        SetReplaceCvConfirmModalVisible(false);
        await ImportCvFromFileAsync(replaceExisting: true, useIntroProgressUi: false, useReplaceProgressUi: true);
    }

    private async void OnReplaceCvImportRetryClicked(object? sender, RoutedEventArgs e)
    {
        await ImportCvFromFileAsync(replaceExisting: true, useIntroProgressUi: false, useReplaceProgressUi: true);
    }

    private async void OnImportCvClicked(object? sender, RoutedEventArgs e)
    {
        await ImportCvFromFileAsync(replaceExisting: false, useIntroProgressUi: true, useReplaceProgressUi: false);
    }

    private async Task ImportCvFromFileAsync(bool replaceExisting, bool useIntroProgressUi, bool useReplaceProgressUi)
    {
        if (_isImportInProgress)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            if (useIntroProgressUi)
            {
                ShowIntroImportError(_localizer.Get(TranslationKeys.ExportFilePickerUnavailable));
            }
            else if (useReplaceProgressUi)
            {
                ShowReplaceCvImportError(_localizer.Get(TranslationKeys.ExportFilePickerUnavailable));
            }

            return;
        }

        var filePickerTitle = useIntroProgressUi
            ? _localizer.Get(TranslationKeys.ImportCvFilePickerTitle)
            : _localizer.Get(TranslationKeys.UploadCvFilePickerTitle);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = filePickerTitle,
                AllowMultiple = false,
                FileTypeFilter = CvImportFilePickerOptions.CreateFileTypeFilter(_localizer)
            });

        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } filePath)
        {
            if (useIntroProgressUi)
            {
                ResetIntroImportState();
            }

            return;
        }

        _isImportInProgress = true;
        SetImportProgressUiVisible(
            true,
            _localizer.Get(TranslationKeys.IntroReadingPdf),
            useIntroProgressUi,
            useReplaceProgressUi);

        if (useIntroProgressUi)
        {
            IntroErrorTextBlock.IsVisible = false;
            IntroRetryImportButton.IsVisible = false;
        }
        else if (useReplaceProgressUi)
        {
            ReplaceCvImportErrorTextBlock.IsVisible = false;
            ReplaceCvImportRetryButton.IsVisible = false;
        }

        UploadCvButton.IsEnabled = false;
        OpenCreateNewCvButton.IsEnabled = false;

        try
        {
            var importResult = await Task.Run(() => CvDocumentImporter.Import(filePath));
            SetImportProgressUiVisible(
                true,
                _localizer.Get(TranslationKeys.IntroParsingCv),
                useIntroProgressUi,
                useReplaceProgressUi);

            if (!importResult.Success)
            {
                var errorMessage = _localizer.Get(importResult.ErrorMessageKey ?? TranslationKeys.ImportErrorUnreadableDocument);
                if (useIntroProgressUi)
                {
                    ShowIntroImportError(errorMessage);
                }
                else if (useReplaceProgressUi)
                {
                    ShowReplaceCvImportError(errorMessage);
                }

                return;
            }

            if (replaceExisting)
            {
                ClearCvForm();
            }

            ApplyCvImportResult(importResult);

            if (useIntroProgressUi)
            {
                SetIntroModalVisible(false);
            }
            else if (useReplaceProgressUi)
            {
                SetReplaceCvImportProgressModalVisible(false);
            }

            UpdatePreview();
            UpdateValidationState();
            ExportStatusTextBlock.Text = string.Empty;
        }
        finally
        {
            _isImportInProgress = false;
            if (useIntroProgressUi)
            {
                SetIntroImportProgressVisible(false, string.Empty);
            }
            else if (useReplaceProgressUi && !ReplaceCvImportErrorTextBlock.IsVisible)
            {
                SetReplaceCvImportProgressModalVisible(false);
            }

            UploadCvButton.IsEnabled = true;
            OpenCreateNewCvButton.IsEnabled = true;
        }
    }

    private bool HasCvFormData()
    {
        if (HasPersonalInformationData())
        {
            return true;
        }

        if (WorkExperienceSection.Entries.Any(entry => entry.HasUserInput()))
        {
            return true;
        }

        if (EducationSection.Entries.Any(entry => entry.HasUserInput()))
        {
            return true;
        }

        if (SkillsSection.Entries.Any(entry => entry.HasUserInput()))
        {
            return true;
        }

        if (LanguagesSection.Entries.Any(entry => entry.HasUserInput()))
        {
            return true;
        }

        if (CertificatesSection.Entries.Any(entry => entry.HasUserInput()))
        {
            return true;
        }

        if (ProjectsSection.Entries.Any(entry => entry.HasUserInput()))
        {
            return true;
        }

        if (LinksSection.Entries.Any(entry => entry.HasUserInput()))
        {
            return true;
        }

        return AdditionalInformationSection.ContentModel.HasUserInput();
    }

    private bool HasPersonalInformationData()
    {
        return !string.IsNullOrWhiteSpace(FirstNameTextBox.Text)
            || !string.IsNullOrWhiteSpace(LastNameTextBox.Text)
            || !string.IsNullOrWhiteSpace(ProfessionalTitleTextBox.Text)
            || !string.IsNullOrWhiteSpace(EmailTextBox.Text)
            || !string.IsNullOrWhiteSpace(PhoneTextBox.Text)
            || !string.IsNullOrWhiteSpace(LocationTextBox.Text)
            || !string.IsNullOrWhiteSpace(LinkedInUrlTextBox.Text)
            || !string.IsNullOrWhiteSpace(PortfolioUrlTextBox.Text)
            || !string.IsNullOrWhiteSpace(GitHubUrlTextBox.Text)
            || !string.IsNullOrWhiteSpace(ShortSummaryTextBox.Text)
            || ProfilePhotoStorage.FileExists(_profilePhotoPath);
    }

    private void ClearCvForm()
    {
        ClearPersonalInformationForm();

        WorkExperienceSection.ReplaceEntries([], expandSection: false);
        EducationSection.ReplaceEntries([], expandSection: false);
        SkillsSection.ReplaceEntries([], expandSection: false);
        LanguagesSection.ReplaceEntries([], expandSection: false);
        CertificatesSection.ReplaceEntries([], expandSection: false);
        ProjectsSection.ReplaceEntries([], expandSection: false);
        LinksSection.ReplaceEntries([], expandSection: false);
        AdditionalInformationSection.SetContent(string.Empty, expandSection: false);

        PersonalInformationSection.IsExpanded = false;
        WorkExperienceSection.SetSectionExpanded(false);
        EducationSection.SetSectionExpanded(false);
        SkillsSection.SetSectionExpanded(false);
        LanguagesSection.SetSectionExpanded(false);
        CertificatesSection.SetSectionExpanded(false);
        ProjectsSection.SetSectionExpanded(false);
        LinksSection.SetSectionExpanded(false);
        AdditionalInformationSection.SetSectionExpanded(false);

        _validationTouchTracker.Reset();
        _personalValidationRegistry.ClearAll();
    }

    private void ClearPersonalInformationForm()
    {
        FirstNameTextBox.Text = string.Empty;
        LastNameTextBox.Text = string.Empty;
        ProfessionalTitleTextBox.Text = string.Empty;
        EmailTextBox.Text = string.Empty;
        PhoneTextBox.Text = string.Empty;
        LocationTextBox.Text = string.Empty;
        LinkedInUrlTextBox.Text = string.Empty;
        PortfolioUrlTextBox.Text = string.Empty;
        GitHubUrlTextBox.Text = string.Empty;
        ShortSummaryTextBox.Text = string.Empty;
        ClearProfilePhoto(showMissingWarning: false);

        foreach (var textBox in new TextBox[]
        {
            FirstNameTextBox,
            LastNameTextBox,
            ProfessionalTitleTextBox,
            EmailTextBox,
            PhoneTextBox,
            LocationTextBox,
            LinkedInUrlTextBox,
            PortfolioUrlTextBox,
            GitHubUrlTextBox,
            ShortSummaryTextBox
        })
        {
            textBox.Classes.Remove(UiClasses.ImportHint);
            textBox.Classes.Remove(UiClasses.FieldInvalid);
        }
    }

    private void SetReplaceCvConfirmModalVisible(bool isVisible)
    {
        ReplaceCvConfirmModalOverlay.IsVisible = isVisible;
        if (isVisible)
        {
            SetupModalOverlay.IsVisible = false;
            TemplatesModalOverlay.IsVisible = false;
            PreviewExpandModalOverlay.IsVisible = false;
            ExportModalOverlay.IsVisible = false;
            NewCvConfirmModalOverlay.IsVisible = false;
        }
    }

    private void SetNewCvConfirmModalVisible(bool isVisible)
    {
        NewCvConfirmModalOverlay.IsVisible = isVisible;
        if (isVisible)
        {
            SetupModalOverlay.IsVisible = false;
            TemplatesModalOverlay.IsVisible = false;
            PreviewExpandModalOverlay.IsVisible = false;
            ExportModalOverlay.IsVisible = false;
            ReplaceCvConfirmModalOverlay.IsVisible = false;
        }
    }

    private void SetReplaceCvImportProgressModalVisible(bool isVisible)
    {
        ReplaceCvImportProgressModalOverlay.IsVisible = isVisible;
        if (isVisible)
        {
            SetupModalOverlay.IsVisible = false;
            TemplatesModalOverlay.IsVisible = false;
            PreviewExpandModalOverlay.IsVisible = false;
            ReplaceCvConfirmModalOverlay.IsVisible = false;
        }
    }

    private void SetImportProgressUiVisible(
        bool isVisible,
        string message,
        bool useIntroProgressUi,
        bool useReplaceProgressUi)
    {
        if (useIntroProgressUi)
        {
            SetIntroImportProgressVisible(isVisible, message);
        }
        else if (useReplaceProgressUi)
        {
            SetReplaceCvImportProgressVisible(isVisible, message);
        }
    }

    private void SetReplaceCvImportProgressVisible(bool isVisible, string message)
    {
        SetReplaceCvImportProgressModalVisible(isVisible);
        ReplaceCvImportProgressTextBlock.Text = message;
        if (!isVisible)
        {
            ReplaceCvImportErrorTextBlock.IsVisible = false;
            ReplaceCvImportRetryButton.IsVisible = false;
        }
    }

    private void ShowReplaceCvImportError(string message)
    {
        ReplaceCvImportErrorTextBlock.Text = message;
        ReplaceCvImportErrorTextBlock.IsVisible = true;
        ReplaceCvImportRetryButton.IsVisible = true;
        ReplaceCvImportProgressTextBlock.Text = string.Empty;
    }

    private void ApplyCvImportResult(CvImportResult result)
    {
        ClearProfilePhotoBeforeImport();
        ApplyPersonalInformationImport(result.Personal);
        WorkExperienceSection.ReplaceEntries(
            result.WorkExperienceEntries,
            IsSectionExpanded(result, CvImportSectionId.WorkExperience));
        EducationSection.ReplaceEntries(
            result.EducationEntries,
            IsSectionExpanded(result, CvImportSectionId.Education));
        SkillsSection.ReplaceEntries(
            result.SkillsGroups,
            IsSectionExpanded(result, CvImportSectionId.Skills));
        LanguagesSection.ReplaceEntries(
            result.LanguageEntries,
            IsSectionExpanded(result, CvImportSectionId.Languages));
        CertificatesSection.ReplaceEntries(
            result.CertificateEntries,
            IsSectionExpanded(result, CvImportSectionId.Certificates));
        ProjectsSection.ReplaceEntries(
            result.ProjectEntries,
            IsSectionExpanded(result, CvImportSectionId.Projects));
        LinksSection.ReplaceEntries(
            result.LinkEntries,
            IsSectionExpanded(result, CvImportSectionId.Links));
        AdditionalInformationSection.SetContent(
            result.AdditionalInformationContent,
            IsSectionExpanded(result, CvImportSectionId.AdditionalInformation));

        PersonalInformationSection.IsExpanded = IsSectionExpanded(result, CvImportSectionId.PersonalInformation);

        ApplyImportConfidence(result.FieldConfidences);
    }

    private void ApplyPersonalInformationImport(PersonalInformationImport personal)
    {
        FirstNameTextBox.Text = personal.FirstName;
        LastNameTextBox.Text = personal.LastName;
        ProfessionalTitleTextBox.Text = personal.ProfessionalTitle;
        EmailTextBox.Text = personal.Email;
        PhoneTextBox.Text = personal.Phone;
        LocationTextBox.Text = personal.Location;
        LinkedInUrlTextBox.Text = personal.LinkedInUrl;
        PortfolioUrlTextBox.Text = personal.PortfolioUrl;
        GitHubUrlTextBox.Text = personal.GitHubUrl;
        ShortSummaryTextBox.Text = personal.ShortSummary;
        ApplyImportedProfilePhoto(
            string.IsNullOrWhiteSpace(personal.ProfilePhotoPath) ? null : personal.ProfilePhotoPath);
    }

    private void ApplyImportConfidence(IReadOnlyList<ImportedFieldConfidence> confidences)
    {
        ImportConfidenceHelper.ApplyToFields(
            new Dictionary<string, TextBox>(StringComparer.Ordinal)
            {
                [MainPersonalInformationFieldKeys.FirstName] = FirstNameTextBox,
                [MainPersonalInformationFieldKeys.LastName] = LastNameTextBox,
                [MainPersonalInformationFieldKeys.ProfessionalTitle] = ProfessionalTitleTextBox,
                [MainPersonalInformationFieldKeys.Email] = EmailTextBox,
                [MainPersonalInformationFieldKeys.Phone] = PhoneTextBox,
                [MainPersonalInformationFieldKeys.Location] = LocationTextBox,
                [MainPersonalInformationFieldKeys.LinkedInUrl] = LinkedInUrlTextBox,
                [MainPersonalInformationFieldKeys.PortfolioUrl] = PortfolioUrlTextBox,
                [MainPersonalInformationFieldKeys.GitHubUrl] = GitHubUrlTextBox,
                [MainPersonalInformationFieldKeys.ShortSummary] = ShortSummaryTextBox
            },
            confidences);

        WorkExperienceSection.ApplyImportConfidence(confidences);
        EducationSection.ApplyImportConfidence(confidences);
        SkillsSection.ApplyImportConfidence(confidences);
        LanguagesSection.ApplyImportConfidence(confidences);
        CertificatesSection.ApplyImportConfidence(confidences);
        ProjectsSection.ApplyImportConfidence(confidences);
        LinksSection.ApplyImportConfidence(confidences);
        AdditionalInformationSection.ApplyImportConfidence(confidences);
    }

    private static bool IsSectionExpanded(CvImportResult result, CvImportSectionId sectionId)
    {
        return result.SectionHasData.TryGetValue(sectionId, out var hasData) && hasData;
    }

    private void SetIntroModalVisible(bool isVisible)
    {
        IntroModalOverlay.IsVisible = isVisible;
        if (isVisible)
        {
            ResetIntroImportState();
        }
    }

    private void ResetIntroImportState()
    {
        IntroActionsPanel.IsVisible = true;
        IntroProgressPanel.IsVisible = false;
        IntroErrorTextBlock.IsVisible = false;
        IntroRetryImportButton.IsVisible = false;
        CreateNewCvButton.IsEnabled = true;
        ImportCvButton.IsEnabled = true;
    }

    private void SetIntroImportProgressVisible(bool isVisible, string message)
    {
        IntroActionsPanel.IsVisible = !isVisible;
        IntroProgressPanel.IsVisible = isVisible;
        IntroProgressTextBlock.Text = message;
        CreateNewCvButton.IsEnabled = !isVisible;
        ImportCvButton.IsEnabled = !isVisible;
    }

    private void ShowIntroImportError(string message)
    {
        IntroErrorTextBlock.Text = message;
        IntroErrorTextBlock.IsVisible = true;
        IntroRetryImportButton.IsVisible = true;
        IntroActionsPanel.IsVisible = true;
        IntroProgressPanel.IsVisible = false;
        CreateNewCvButton.IsEnabled = true;
        ImportCvButton.IsEnabled = true;
    }

    private void OnCloseSetupClicked(object? sender, RoutedEventArgs e)
    {
        SetSetupModalVisible(false);
    }

    private void OnCloseTemplatesClicked(object? sender, RoutedEventArgs e)
    {
        SetTemplatesModalVisible(false);
    }

    private void OnClosePreviewExpandClicked(object? sender, RoutedEventArgs e)
    {
        SetPreviewExpandModalVisible(false);
    }

    private void OnLanguageSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingLanguageSelection || LanguageComboBox.SelectedItem is not SupportedLanguage language)
        {
            return;
        }

        _localizer = new AppLocalizer(language.Code);
        ApplyLocalization();
        UpdateValidationState();
        UpdatePreview();
    }

    private void OnSelectClassicSidebarTemplateClicked(object? sender, RoutedEventArgs e)
    {
        SelectTemplate(CvExportTemplateId.ClassicSidebar);
    }

    private void OnSelectModernSidebarTemplateClicked(object? sender, RoutedEventArgs e)
    {
        SelectTemplate(CvExportTemplateId.ModernSidebar);
    }

    private void OnSelectCleanTopHeaderTemplateClicked(object? sender, RoutedEventArgs e)
    {
        SelectTemplate(CvExportTemplateId.CleanTopHeader);
    }

    private void OnSelectDarkSidebarTemplateClicked(object? sender, RoutedEventArgs e)
    {
        SelectTemplate(CvExportTemplateId.DarkSidebarAccent);
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        if (IntroModalOverlay.IsVisible)
        {
            return;
        }

        if (ReplaceCvConfirmModalOverlay.IsVisible)
        {
            SetReplaceCvConfirmModalVisible(false);
        }
        else if (NewCvConfirmModalOverlay.IsVisible)
        {
            SetNewCvConfirmModalVisible(false);
        }
        else if (ReplaceCvImportProgressModalOverlay.IsVisible)
        {
            if (!_isImportInProgress)
            {
                SetReplaceCvImportProgressModalVisible(false);
            }
        }
        else if (ExportModalOverlay.IsVisible)
        {
            SetExportModalVisible(false);
        }
        else if (TemplatesModalOverlay.IsVisible)
        {
            SetTemplatesModalVisible(false);
        }
        else if (SetupModalOverlay.IsVisible)
        {
            SetSetupModalVisible(false);
        }
        else if (PreviewExpandModalOverlay.IsVisible)
        {
            SetPreviewExpandModalVisible(false);
        }
        else
        {
            return;
        }

        e.Handled = true;
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateModalSizes();
    }

    private async void OnExportPdfClicked(object? sender, RoutedEventArgs e)
    {
        if (IntroModalOverlay.IsVisible || ReplaceCvImportProgressModalOverlay.IsVisible)
        {
            return;
        }

        HideExportPostActions();

        var validationResult = ValidateForm();
        if (!validationResult.IsValid)
        {
            FormValidationService.ApplyExportFailure(validationResult, _validationTouchTracker);
            UpdateValidationState(validationResult);
            ExportStatusTextBlock.Text = _localizer.Get(TranslationKeys.ExportFixValidation);
            ScrollToFirstInvalidField(validationResult);
            return;
        }

        OpenExportModal();
    }

    private void OnCloseExportModalClicked(object? sender, RoutedEventArgs e)
    {
        SetExportModalVisible(false);
    }

    private void OpenExportModal()
    {
        SetSetupModalVisible(false);
        SetTemplatesModalVisible(false);
        SetPreviewExpandModalVisible(false);
        PopulateExportFormatCards();
        SetExportModalVisible(true);
    }

    private void PopulateExportFormatCards()
    {
        ExportFormatCategoriesPanel.Children.Clear();

        foreach (var category in new[]
                 {
                     CvExportFormatCategory.Documents,
                     CvExportFormatCategory.WebAndText,
                     CvExportFormatCategory.Structured
                 })
        {
            ExportFormatCategoriesPanel.Children.Add(new TextBlock
            {
                Text = GetExportCategoryLabel(category),
                Classes = { "re-vitae-export-category" }
            });

            var wrapPanel = new WrapPanel();
            foreach (var descriptor in CvExportFormatCatalog.GetEnabledFormats().Where(d => d.Category == category))
            {
                wrapPanel.Children.Add(CreateExportFormatCard(descriptor));
            }

            ExportFormatCategoriesPanel.Children.Add(wrapPanel);
        }
    }

    private Button CreateExportFormatCard(CvExportFormatDescriptor descriptor)
    {
        var card = new Button
        {
            Classes = { UiClasses.ExportFormatCard },
            IsEnabled = descriptor.IsEnabled
        };

        var content = new StackPanel { Spacing = 8 };
        var icon = CvExportFormatIconLoader.LoadIcon(descriptor.Format);
        if (icon is not null)
        {
            content.Children.Add(new Image
            {
                Source = icon,
                Width = 40,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Left
            });
        }

        content.Children.Add(new TextBlock
        {
            Text = _localizer.Get(descriptor.LabelKey),
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        if (!string.IsNullOrWhiteSpace(descriptor.HintKey))
        {
            content.Children.Add(new TextBlock
            {
                Text = _localizer.Get(descriptor.HintKey!),
                Classes = { UiClasses.SecondaryText },
                TextWrapping = TextWrapping.Wrap
            });
        }

        if (descriptor.IsRecommended)
        {
            content.Children.Add(new TextBlock
            {
                Text = _localizer.Get(TranslationKeys.ExportFormatRecommended),
                Classes = { UiClasses.SecondaryText },
                FontWeight = FontWeight.SemiBold
            });
        }

        card.Content = content;
        card.Click += async (_, _) => await OnExportFormatSelectedAsync(descriptor.Format);
        return card;
    }

    private string GetExportCategoryLabel(CvExportFormatCategory category) => category switch
    {
        CvExportFormatCategory.Documents => _localizer.Get(TranslationKeys.ExportCategoryDocuments),
        CvExportFormatCategory.WebAndText => _localizer.Get(TranslationKeys.ExportCategoryWebAndText),
        CvExportFormatCategory.Structured => _localizer.Get(TranslationKeys.ExportCategoryStructured),
        _ => string.Empty
    };

    private async Task OnExportFormatSelectedAsync(CvExportFormat format)
    {
        SetExportModalVisible(false);
        HideExportPostActions();

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            ExportStatusTextBlock.Text = _localizer.Get(TranslationKeys.ExportFilePickerUnavailable);
            return;
        }

        var suggestedFilename = CvExportFilenameHelper.SuggestFilename(
            FirstNameTextBox.Text,
            LastNameTextBox.Text,
            format);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            CvExportFilePickerOptions.Create(format, _localizer, suggestedFilename));

        if (file is null)
        {
            return;
        }

        var document = BuildExportDocument();
        var source = BuildExportSourceData();

        try
        {
            var localPath = file.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(localPath))
            {
                ExportStatusTextBlock.Text = _localizer.Get(TranslationKeys.ExportFailed);
                return;
            }

            await using (var stream = File.Create(localPath))
            {
                var result = CvDocumentExporter.Export(document, source, format, stream);
                if (!result.Success)
                {
                    ExportStatusTextBlock.Text = _localizer.Get(result.ErrorMessageKey ?? TranslationKeys.ExportFailed);
                    return;
                }
            }

            _lastExportedFilePath = localPath;
            ExportStatusTextBlock.Text = _localizer.Format(TranslationKeys.ExportedTo, Path.GetFileName(localPath));
            ShowExportPostActions();
        }
        catch
        {
            ExportStatusTextBlock.Text = _localizer.Format(
                TranslationKeys.ExportFailedFormat,
                _localizer.Get(CvExportFormatCatalog.Get(format).LabelKey));
        }
    }

    private void OnExportOpenFileClicked(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_lastExportedFilePath))
        {
            CvExportShellHelper.OpenFile(_lastExportedFilePath);
        }
    }

    private void OnExportShowInFolderClicked(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_lastExportedFilePath))
        {
            CvExportShellHelper.RevealInFolder(_lastExportedFilePath);
        }
    }

    private void ShowExportPostActions()
    {
        ExportPostActionPanel.IsVisible = !string.IsNullOrWhiteSpace(_lastExportedFilePath);
    }

    private void HideExportPostActions()
    {
        _lastExportedFilePath = null;
        ExportPostActionPanel.IsVisible = false;
    }

    private CvExportSourceData BuildExportSourceData()
    {
        var personal = new PersonalInformationImport
        {
            FirstName = NormalizeValue(FirstNameTextBox.Text),
            LastName = NormalizeValue(LastNameTextBox.Text),
            ProfessionalTitle = NormalizeValue(ProfessionalTitleTextBox.Text),
            Email = NormalizeValue(EmailTextBox.Text),
            Phone = NormalizeValue(PhoneTextBox.Text),
            Location = NormalizeValue(LocationTextBox.Text),
            LinkedInUrl = NormalizeValue(LinkedInUrlTextBox.Text),
            PortfolioUrl = NormalizeValue(PortfolioUrlTextBox.Text),
            GitHubUrl = NormalizeValue(GitHubUrlTextBox.Text),
            ShortSummary = ShortSummaryTextBox.Text?.Trim() ?? string.Empty,
            ProfilePhotoPath = _profilePhotoPath ?? string.Empty
        };

        return CvExportSourceDataFactory.Create(
            personal,
            WorkExperienceSection.Entries,
            EducationSection.Entries,
            SkillsSection.Entries,
            LanguagesSection.Entries,
            CertificatesSection.Entries,
            ProjectsSection.Entries,
            LinksSection.Entries,
            GetAdditionalInformationContent());
    }

    private void ApplyLocalization()
    {
        HeaderSubtitleTextBlock.Text = _localizer.Get(TranslationKeys.HeaderSubtitle);
        ToolTip.SetTip(UploadCvButton, _localizer.Get(TranslationKeys.OpenUploadCv));
        AutomationProperties.SetName(UploadCvButton, _localizer.Get(TranslationKeys.OpenUploadCv));
        ToolTip.SetTip(OpenCreateNewCvButton, _localizer.Get(TranslationKeys.OpenCreateNewCv));
        AutomationProperties.SetName(OpenCreateNewCvButton, _localizer.Get(TranslationKeys.OpenCreateNewCv));
        ToolTip.SetTip(OpenSetupButton, _localizer.Get(TranslationKeys.OpenSetup));
        ToolTip.SetTip(OpenTemplatesButton, _localizer.Get(TranslationKeys.OpenTemplates));
        ToolTip.SetTip(OpenPreviewExpandButton, _localizer.Get(TranslationKeys.OpenExpandPreview));
        AutomationProperties.SetName(OpenPreviewExpandButton, _localizer.Get(TranslationKeys.OpenExpandPreview));

        PersonalInformationSection.Title = _localizer.Get(TranslationKeys.MainPersonalInformation);
        PersonalInformationSection.ExpandToolTip = _localizer.Get(TranslationKeys.ExpandSection);
        PersonalInformationSection.CollapseToolTip = _localizer.Get(TranslationKeys.CollapseSection);
        WorkExperienceSection.SetLocalizer(_localizer);
        EducationSection.SetLocalizer(_localizer);
        SkillsSection.SetLocalizer(_localizer);
        LanguagesSection.SetLocalizer(_localizer);
        CertificatesSection.SetLocalizer(_localizer);
        ProjectsSection.SetLocalizer(_localizer);
        LinksSection.SetLocalizer(_localizer);
        AdditionalInformationSection.SetLocalizer(_localizer);
        IntroTitleTextBlock.Text = _localizer.Get(TranslationKeys.IntroTitle);
        IntroSubtitleTextBlock.Text = _localizer.Get(TranslationKeys.IntroSubtitle);
        IntroHelperTextBlock.Text = _localizer.Get(TranslationKeys.IntroHelper);
        CreateNewCvButtonTextBlock.Text = _localizer.Get(TranslationKeys.IntroCreateNew);
        ImportCvButtonTextBlock.Text = _localizer.Get(TranslationKeys.IntroImportPdf);
        IntroRetryImportButton.Content = _localizer.Get(TranslationKeys.IntroImportRetry);
        ReplaceCvConfirmTitleTextBlock.Text = _localizer.Get(TranslationKeys.ReplaceCvConfirmTitle);
        ReplaceCvConfirmMessageTextBlock.Text = _localizer.Get(TranslationKeys.ReplaceCvConfirmMessage);
        ReplaceCvConfirmCancelButton.Content = _localizer.Get(TranslationKeys.Cancel);
        ReplaceCvConfirmOkButton.Content = _localizer.Get(TranslationKeys.Confirm);
        NewCvConfirmTitleTextBlock.Text = _localizer.Get(TranslationKeys.NewCvConfirmTitle);
        NewCvConfirmMessageTextBlock.Text = _localizer.Get(TranslationKeys.NewCvConfirmMessage);
        NewCvConfirmCancelButton.Content = _localizer.Get(TranslationKeys.Cancel);
        NewCvConfirmOkButton.Content = _localizer.Get(TranslationKeys.Confirm);
        ReplaceCvImportRetryButton.Content = _localizer.Get(TranslationKeys.IntroImportRetry);
        AutomationProperties.SetName(CreateNewCvButton, _localizer.Get(TranslationKeys.IntroCreateNew));
        AutomationProperties.SetName(ImportCvButton, _localizer.Get(TranslationKeys.IntroImportPdf));
        FirstNameLabelTextBlock.Text = _localizer.Get(TranslationKeys.FirstName);
        ProfilePhotoLabelTextBlock.Text = _localizer.Get(TranslationKeys.ProfilePhoto);
        ProfilePhotoRemoveButton.Content = _localizer.Get(TranslationKeys.ProfilePhotoRemove);
        RefreshProfilePhotoUi();
        LastNameLabelTextBlock.Text = _localizer.Get(TranslationKeys.LastName);
        ProfessionalTitleLabelTextBlock.Text = _localizer.Get(TranslationKeys.ProfessionalTitle);
        EmailLabelTextBlock.Text = _localizer.Get(TranslationKeys.Email);
        PhoneLabelTextBlock.Text = _localizer.Get(TranslationKeys.Phone);
        LocationLabelTextBlock.Text = _localizer.Get(TranslationKeys.Location);
        LinkedInUrlLabelTextBlock.Text = _localizer.Get(TranslationKeys.LinkedInUrl);
        PortfolioUrlLabelTextBlock.Text = _localizer.Get(TranslationKeys.PortfolioUrl);
        GitHubUrlLabelTextBlock.Text = _localizer.Get(TranslationKeys.GitHubUrl);
        ShortSummaryLabelTextBlock.Text = _localizer.Get(TranslationKeys.ShortSummary);
        ShortSummaryTextBox.PlaceholderText = _localizer.Get(TranslationKeys.ShortSummaryPlaceholder);
        ExportPdfButton.Content = _localizer.Get(TranslationKeys.Export);
        ExportModalTitleTextBlock.Text = _localizer.Get(TranslationKeys.ExportModalTitle);
        ExportModalSubtitleTextBlock.Text = _localizer.Get(TranslationKeys.ExportModalSubtitle);
        ExportModalTopCloseButton.Content = _localizer.Get(TranslationKeys.ExportModalClose);
        ExportModalBottomCloseButton.Content = _localizer.Get(TranslationKeys.ExportModalClose);
        ExportOpenFileButton.Content = _localizer.Get(TranslationKeys.ExportOpenFile);
        ExportShowInFolderButton.Content = _localizer.Get(TranslationKeys.ExportShowInFolder);
        PreviewTitleTextBlock.Text = _localizer.Get(TranslationKeys.Preview);
        PreviewExpandTitleTextBlock.Text = _localizer.Get(TranslationKeys.PreviewExpandTitle);
        PreviewExpandTopCloseButton.Content = _localizer.Get(TranslationKeys.Close);
        PreviewExpandBottomCloseButton.Content = _localizer.Get(TranslationKeys.Close);

        SetupTitleTextBlock.Text = _localizer.Get(TranslationKeys.Setup);
        AboutTitleTextBlock.Text = _localizer.Get(TranslationKeys.SetupAbout);
        AboutAppNameTextBlock.Text = _localizer.Get(TranslationKeys.SetupAppName);
        AboutVersionLabelTextBlock.Text = _localizer.Get(TranslationKeys.SetupVersion);
        AboutVersionValueTextBlock.Text = AppVersion.Current;
        AboutEarlyPreviewTextBlock.Text = _localizer.Get(TranslationKeys.SetupEarlyPreview);
        AboutEarlyPreviewTextBlock.IsVisible = AppVersion.IsPreRelease;
        SetupTopCloseButton.Content = _localizer.Get(TranslationKeys.Close);
        SetupBottomCloseButton.Content = _localizer.Get(TranslationKeys.Close);
        LanguageLabelTextBlock.Text = _localizer.Get(TranslationKeys.Language);

        TemplatesTitleTextBlock.Text = _localizer.Get(TranslationKeys.Templates);
        TemplatesTopCloseButton.Content = _localizer.Get(TranslationKeys.Close);
        TemplatesBottomCloseButton.Content = _localizer.Get(TranslationKeys.Close);
        ClassicSidebarNameTextBlock.Text = _localizer.Get(TranslationKeys.ClassicSidebar);
        ClassicSidebarDescriptionTextBlock.Text = _localizer.Get(TranslationKeys.ClassicSidebarDescription);
        ModernSidebarNameTextBlock.Text = _localizer.Get(TranslationKeys.ModernSidebar);
        ModernSidebarDescriptionTextBlock.Text = _localizer.Get(TranslationKeys.ModernSidebarDescription);
        CleanTopHeaderNameTextBlock.Text = _localizer.Get(TranslationKeys.CleanTopHeader);
        CleanTopHeaderDescriptionTextBlock.Text = _localizer.Get(TranslationKeys.CleanTopHeaderDescription);
        DarkSidebarAccentNameTextBlock.Text = _localizer.Get(TranslationKeys.DarkSidebarAccent);
        DarkSidebarAccentDescriptionTextBlock.Text = _localizer.Get(TranslationKeys.DarkSidebarAccentDescription);
        ClassicSidebarSelectedTextBlock.Text = _localizer.Get(TranslationKeys.Selected);
        ModernSidebarSelectedTextBlock.Text = _localizer.Get(TranslationKeys.Selected);
        CleanTopHeaderSelectedTextBlock.Text = _localizer.Get(TranslationKeys.Selected);
        DarkSidebarSelectedTextBlock.Text = _localizer.Get(TranslationKeys.Selected);
    }

    private void UpdatePreview()
    {
        PreviewContentControl.Content = BuildTemplatePreview();
        PreviewExpandContentControl.Content = BuildTemplatePreview();
    }

    private void SelectTemplate(CvExportTemplateId templateId)
    {
        _selectedTemplate = templateId;
        UpdateTemplateSelectionState();
        UpdatePreview();
        SetTemplatesModalVisible(false);
    }

    private void UpdateTemplateSelectionState()
    {
        ClassicSidebarSelectedTextBlock.IsVisible = _selectedTemplate == CvExportTemplateId.ClassicSidebar;
        ModernSidebarSelectedTextBlock.IsVisible = _selectedTemplate == CvExportTemplateId.ModernSidebar;
        CleanTopHeaderSelectedTextBlock.IsVisible = _selectedTemplate == CvExportTemplateId.CleanTopHeader;
        DarkSidebarSelectedTextBlock.IsVisible = _selectedTemplate == CvExportTemplateId.DarkSidebarAccent;

        ClassicSidebarTemplateButton.Classes.Set("selected", _selectedTemplate == CvExportTemplateId.ClassicSidebar);
        ModernSidebarTemplateButton.Classes.Set("selected", _selectedTemplate == CvExportTemplateId.ModernSidebar);
        CleanTopHeaderTemplateButton.Classes.Set("selected", _selectedTemplate == CvExportTemplateId.CleanTopHeader);
        DarkSidebarTemplateButton.Classes.Set("selected", _selectedTemplate == CvExportTemplateId.DarkSidebarAccent);
    }

    private void SetSetupModalVisible(bool isVisible)
    {
        SetupModalOverlay.IsVisible = isVisible;
        if (isVisible)
        {
            TemplatesModalOverlay.IsVisible = false;
            PreviewExpandModalOverlay.IsVisible = false;
            ExportModalOverlay.IsVisible = false;
        }

        UpdateModalSizes();
    }

    private void SetTemplatesModalVisible(bool isVisible)
    {
        TemplatesModalOverlay.IsVisible = isVisible;
        if (isVisible)
        {
            SetupModalOverlay.IsVisible = false;
            PreviewExpandModalOverlay.IsVisible = false;
            ExportModalOverlay.IsVisible = false;
        }

        UpdateModalSizes();
    }

    private void SetPreviewExpandModalVisible(bool isVisible)
    {
        PreviewExpandModalOverlay.IsVisible = isVisible;
        if (isVisible)
        {
            SetupModalOverlay.IsVisible = false;
            TemplatesModalOverlay.IsVisible = false;
            ExportModalOverlay.IsVisible = false;
        }

        UpdateModalSizes();
    }

    private void SetExportModalVisible(bool isVisible)
    {
        ExportModalOverlay.IsVisible = isVisible;
        if (isVisible)
        {
            SetupModalOverlay.IsVisible = false;
            TemplatesModalOverlay.IsVisible = false;
            PreviewExpandModalOverlay.IsVisible = false;
        }

        UpdateModalSizes();
    }

    private void UpdateModalSizes()
    {
        SetupModalPanel.Width = Math.Max(SetupModalPanel.MinWidth, RootGrid.Bounds.Width * 0.8);
        SetupModalPanel.Height = Math.Max(SetupModalPanel.MinHeight, RootGrid.Bounds.Height * 0.8);
        TemplatesModalPanel.Width = Math.Max(TemplatesModalPanel.MinWidth, RootGrid.Bounds.Width * 0.8);
        TemplatesModalPanel.Height = Math.Max(TemplatesModalPanel.MinHeight, RootGrid.Bounds.Height * 0.8);
        PreviewExpandModalPanel.Width = Math.Max(PreviewExpandModalPanel.MinWidth, RootGrid.Bounds.Width * 0.8);
        PreviewExpandModalPanel.Height = Math.Max(PreviewExpandModalPanel.MinHeight, RootGrid.Bounds.Height * 0.8);
        ExportModalPanel.Width = Math.Max(ExportModalPanel.MinWidth, RootGrid.Bounds.Width * 0.8);
        ExportModalPanel.Height = Math.Max(ExportModalPanel.MinHeight, RootGrid.Bounds.Height * 0.8);
    }

    private void UpdateValidationState(FieldValidationResult? validationResult = null)
    {
        validationResult ??= ValidateForm();

        ExportPdfButton.IsEnabled = validationResult.IsValid;

        var personalErrors = validationResult.Errors
            .Where(error => IsPersonalFieldKey(error.FieldKey))
            .ToArray();
        _personalValidationRegistry.ApplyErrors(personalErrors, _localizer, _validationTouchTracker);
        _personalSectionErrorCount = personalErrors.Length;
        UpdatePersonalSectionErrorBadge();

        WorkExperienceSection.UpdateValidation(validationResult, _validationTouchTracker);
        EducationSection.UpdateValidation(validationResult, _validationTouchTracker);
        SkillsSection.UpdateValidation(validationResult, _validationTouchTracker);
        LanguagesSection.UpdateValidation(validationResult, _validationTouchTracker);
        CertificatesSection.UpdateValidation(validationResult, _validationTouchTracker);
        ProjectsSection.UpdateValidation(validationResult, _validationTouchTracker);
        LinksSection.UpdateValidation(validationResult, _validationTouchTracker);
        AdditionalInformationSection.UpdateValidation(validationResult, _validationTouchTracker);
    }

    private void InitializePersonalValidation()
    {
        (_personalSectionErrorBadgePanel, _personalSectionErrorBadgeTextBlock) = ValidationErrorBadgeFactory.Create();
        PersonalInformationSection.HeaderActions = _personalSectionErrorBadgePanel;

        RegisterPersonalField(MainPersonalInformationFieldKeys.FirstName, FirstNameTextBox, FirstNameErrorTextBlock);
        RegisterPersonalField(MainPersonalInformationFieldKeys.LastName, LastNameTextBox, LastNameErrorTextBlock);
        RegisterPersonalField(MainPersonalInformationFieldKeys.ProfessionalTitle, ProfessionalTitleTextBox, ProfessionalTitleErrorTextBlock);
        RegisterPersonalField(MainPersonalInformationFieldKeys.Email, EmailTextBox, EmailErrorTextBlock);
        RegisterPersonalField(MainPersonalInformationFieldKeys.Phone, PhoneTextBox, PhoneErrorTextBlock);
        RegisterPersonalField(MainPersonalInformationFieldKeys.Location, LocationTextBox, LocationErrorTextBlock);
        RegisterPersonalField(MainPersonalInformationFieldKeys.LinkedInUrl, LinkedInUrlTextBox, LinkedInUrlErrorTextBlock);
        RegisterPersonalField(MainPersonalInformationFieldKeys.PortfolioUrl, PortfolioUrlTextBox, PortfolioUrlErrorTextBlock);
        RegisterPersonalField(MainPersonalInformationFieldKeys.GitHubUrl, GitHubUrlTextBox, GitHubUrlErrorTextBlock);
        RegisterPersonalField(MainPersonalInformationFieldKeys.ShortSummary, ShortSummaryTextBox, ShortSummaryErrorTextBlock);
    }

    private void RegisterPersonalField(string fieldKey, Control input, TextBlock errorTextBlock)
    {
        var binding = new ValidationFieldBinding(fieldKey, input, errorTextBlock);
        binding.WireTouchTracking(_validationTouchTracker, _ => UpdateValidationState());
        _personalValidationRegistry.Register(binding);
    }

    private void UpdatePersonalSectionErrorBadge()
    {
        if (_personalSectionErrorBadgePanel is null || _personalSectionErrorBadgeTextBlock is null)
        {
            return;
        }

        FormValidationService.UpdateSectionErrorBadge(
            _personalSectionErrorBadgePanel,
            _personalSectionErrorBadgeTextBlock,
            _personalSectionErrorCount,
            !PersonalInformationSection.IsExpanded,
            _localizer,
            TranslationKeys.PersonalInformationValidationErrors,
            () => PersonalInformationSection.IsExpanded = true);
    }

    private static bool IsPersonalFieldKey(string fieldKey)
    {
        return fieldKey is MainPersonalInformationFieldKeys.FirstName
            or MainPersonalInformationFieldKeys.LastName
            or MainPersonalInformationFieldKeys.ProfessionalTitle
            or MainPersonalInformationFieldKeys.Email
            or MainPersonalInformationFieldKeys.Phone
            or MainPersonalInformationFieldKeys.Location
            or MainPersonalInformationFieldKeys.LinkedInUrl
            or MainPersonalInformationFieldKeys.PortfolioUrl
            or MainPersonalInformationFieldKeys.GitHubUrl
            or MainPersonalInformationFieldKeys.ShortSummary;
    }

    private IReadOnlyList<string> BuildOrderedFieldKeys()
    {
        var keys = new List<string>
        {
            MainPersonalInformationFieldKeys.FirstName,
            MainPersonalInformationFieldKeys.LastName,
            MainPersonalInformationFieldKeys.ProfessionalTitle,
            MainPersonalInformationFieldKeys.Email,
            MainPersonalInformationFieldKeys.Phone,
            MainPersonalInformationFieldKeys.Location,
            MainPersonalInformationFieldKeys.LinkedInUrl,
            MainPersonalInformationFieldKeys.PortfolioUrl,
            MainPersonalInformationFieldKeys.GitHubUrl,
            MainPersonalInformationFieldKeys.ShortSummary
        };

        keys.AddRange(WorkExperienceSection.GetOrderedFieldKeys());
        keys.AddRange(EducationSection.GetOrderedFieldKeys());
        keys.AddRange(SkillsSection.GetOrderedFieldKeys());
        keys.AddRange(LanguagesSection.GetOrderedFieldKeys());
        keys.AddRange(CertificatesSection.GetOrderedFieldKeys());
        keys.AddRange(ProjectsSection.GetOrderedFieldKeys());
        keys.AddRange(LinksSection.GetOrderedFieldKeys());
        keys.Add(AdditionalInformationFieldKeys.Content);
        return keys;
    }

    private void ScrollToFirstInvalidField(FieldValidationResult validationResult)
    {
        var firstKey = FormValidationService.GetFirstInvalidFieldKey(
            BuildOrderedFieldKeys(),
            validationResult);
        if (firstKey is null)
        {
            return;
        }

        ExpandAndRevealField(firstKey);
    }

    private bool ExpandAndRevealField(string fieldKey)
    {
        if (IsPersonalFieldKey(fieldKey))
        {
            PersonalInformationSection.IsExpanded = true;
            var control = _personalValidationRegistry.FindControlForFieldKey(fieldKey);
            control?.Focus();
            control?.BringIntoView();
            return control is not null;
        }

        IValidationNavigableSection[] sections =
        [
            WorkExperienceSection,
            EducationSection,
            SkillsSection,
            LanguagesSection,
            CertificatesSection,
            ProjectsSection,
            LinksSection,
            AdditionalInformationSection
        ];

        foreach (var section in sections)
        {
            if (!section.ExpandAndRevealField(fieldKey))
            {
                continue;
            }

            var control = section.FindControlForFieldKey(fieldKey);
            control?.BringIntoView();
            return true;
        }

        return false;
    }

    private FieldValidationResult ValidateForm()
    {
        var personalResult = _validator.Validate(BuildFieldValues());
        var workExperienceResult = _workExperienceValidator.Validate(WorkExperienceSection.Entries.ToArray());
        var educationResult = _educationValidator.Validate(EducationSection.Entries.ToArray());
        var skillsResult = _skillsValidator.Validate(SkillsSection.Entries.ToArray());
        var languagesResult = _languagesValidator.Validate(LanguagesSection.Entries.ToArray());
        var certificatesResult = _certificatesValidator.Validate(CertificatesSection.Entries.ToArray());
        var projectsResult = _projectsValidator.Validate(ProjectsSection.Entries.ToArray());
        var linksResult = _linksValidator.Validate(LinksSection.Entries.ToArray());
        var additionalInformationResult = _additionalInformationValidator.Validate(AdditionalInformationSection.ContentModel);
        var combinedErrors = personalResult.Errors
            .Concat(workExperienceResult.Errors)
            .Concat(educationResult.Errors)
            .Concat(skillsResult.Errors)
            .Concat(languagesResult.Errors)
            .Concat(certificatesResult.Errors)
            .Concat(projectsResult.Errors)
            .Concat(linksResult.Errors)
            .Concat(additionalInformationResult.Errors)
            .ToArray();
        return new FieldValidationResult(combinedErrors);
    }

    private IReadOnlyDictionary<string, string?> BuildFieldValues()
    {
        return new Dictionary<string, string?>
        {
            [MainPersonalInformationFieldKeys.FirstName] = FirstNameTextBox.Text,
            [MainPersonalInformationFieldKeys.LastName] = LastNameTextBox.Text,
            [MainPersonalInformationFieldKeys.ProfessionalTitle] = ProfessionalTitleTextBox.Text,
            [MainPersonalInformationFieldKeys.Email] = EmailTextBox.Text,
            [MainPersonalInformationFieldKeys.Phone] = PhoneTextBox.Text,
            [MainPersonalInformationFieldKeys.Location] = LocationTextBox.Text,
            [MainPersonalInformationFieldKeys.LinkedInUrl] = LinkedInUrlTextBox.Text,
            [MainPersonalInformationFieldKeys.PortfolioUrl] = PortfolioUrlTextBox.Text,
            [MainPersonalInformationFieldKeys.GitHubUrl] = GitHubUrlTextBox.Text,
            [MainPersonalInformationFieldKeys.ShortSummary] = ShortSummaryTextBox.Text
        };
    }

    private IReadOnlyList<ExportEducationEntry> GetActiveEducationEntries()
    {
        return EducationSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(BuildEducationPreviewEntry)
            .ToArray();
    }

    private ExportEducationEntry BuildEducationPreviewEntry(ReVitae.Core.Cv.Education.EducationEntry entry)
    {
        return new ExportEducationEntry(
            NormalizeValue(entry.Degree),
            NormalizeValue(entry.Institution),
            NormalizeOptionalValue(entry.FieldOfStudy),
            NormalizeOptionalValue(entry.Location),
            _localizer.Get(entry.DegreeType.ToTranslationKey()),
            entry.BuildDateRangeLabel(_localizer.Get(TranslationKeys.EducationPresent)),
            entry.Grade,
            entry.Description,
            entry.InstitutionUrl);
    }

    private IReadOnlyList<ExportSkillsGroup> GetActiveSkillsPreviewGroups()
    {
        var activeGroups = SkillsSection.Entries
            .Where(entry => entry.HasUserInput())
            .ToArray();

        var prepared = SkillsDeduplication.PrepareForPreview(
            activeGroups,
            WorkExperienceSection.Entries.ToArray());

        return prepared
            .Where(group => group.Skills.Any(skill => skill.HasUserInput()))
            .Select(BuildSkillsPreviewGroup)
            .ToArray();
    }

    private ExportSkillsGroup BuildSkillsPreviewGroup(SkillsGroupEntry group)
    {
        var skills = group.Skills
            .Where(skill => skill.HasUserInput())
            .Select(skill => new ExportSkillItem(
                skill.Name.Trim(),
                _localizer.Get(skill.Proficiency.ToTranslationKey()),
                skill.YearsOfExperience))
            .ToArray();

        return new ExportSkillsGroup(
            NormalizeValue(group.Category),
            skills);
    }

    private IReadOnlyList<ExportLanguageEntry> GetActiveLanguagePreviewEntries()
    {
        return LanguagesSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(BuildLanguagePreviewEntry)
            .ToArray();
    }

    private ExportLanguageEntry BuildLanguagePreviewEntry(ReVitae.Core.Cv.Languages.LanguageEntry entry)
    {
        return new ExportLanguageEntry(
            LanguagePreviewFormatter.FormatMainLine(entry, _localizer),
            LanguagePreviewFormatter.FormatSubSkillLines(entry, _localizer));
    }

    private IReadOnlyList<ExportCertificateEntry> GetActiveCertificatePreviewEntries()
    {
        return CertificatesSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(BuildCertificatePreviewEntry)
            .ToArray();
    }

    private ExportCertificateEntry BuildCertificatePreviewEntry(ReVitae.Core.Cv.Certificates.CertificateEntry entry)
    {
        return new ExportCertificateEntry(
            CertificatePreviewFormatter.FormatMainLine(entry, _localizer),
            CertificatePreviewFormatter.FormatDetailLines(entry, _localizer));
    }

    private IReadOnlyList<ExportProjectEntry> GetActiveProjectPreviewEntries()
    {
        return ProjectsSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(BuildProjectPreviewEntry)
            .ToArray();
    }

    private ExportProjectEntry BuildProjectPreviewEntry(ReVitae.Core.Cv.Projects.ProjectEntry entry)
    {
        return new ExportProjectEntry(
            ProjectPreviewFormatter.FormatMainLine(entry, _localizer),
            ProjectPreviewFormatter.FormatDetailLines(entry, _localizer));
    }

    private IReadOnlyList<string> GetActiveCustomLinkLines()
    {
        return LinksSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(LinkPreviewFormatter.FormatLine)
            .ToArray();
    }

    private IReadOnlyList<ExportWorkExperienceEntry> GetActiveWorkExperienceEntries()
    {
        return WorkExperienceSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(BuildWorkExperiencePreviewEntry)
            .ToArray();
    }

    private ExportWorkExperienceEntry BuildWorkExperiencePreviewEntry(ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry entry)
    {
        return new ExportWorkExperienceEntry(
            NormalizeValue(entry.JobTitle),
            NormalizeValue(entry.Company),
            NormalizeOptionalValue(entry.Location),
            _localizer.Get(entry.EmploymentType.ToTranslationKey()),
            entry.BuildDateRangeLabel(_localizer.Get(TranslationKeys.WorkExperiencePresent)),
            entry.Description,
            entry.Achievements,
            entry.Technologies,
            entry.CompanyUrl);
    }

    private Control BuildTemplatePreview()
    {
        var document = BuildExportDocument();

        return _selectedTemplate switch
        {
            CvExportTemplateId.ClassicSidebar => BuildClassicSidebarTemplate(document),
            CvExportTemplateId.ModernSidebar => BuildModernSidebarTemplate(document),
            CvExportTemplateId.CleanTopHeader => BuildCleanTopHeaderTemplate(document),
            CvExportTemplateId.DarkSidebarAccent => BuildDarkSidebarAccentTemplate(document),
            _ => throw new ArgumentOutOfRangeException(nameof(_selectedTemplate))
        };
    }

    private CvExportDocument BuildExportDocument()
    {
        return new CvExportDocument(
            _selectedTemplate,
            CvExportSectionLabelsFactory.Create(_localizer),
            NormalizeValue(FirstNameTextBox.Text),
            NormalizeValue(LastNameTextBox.Text),
            NormalizeValue(ProfessionalTitleTextBox.Text),
            NormalizeValue(EmailTextBox.Text),
            NormalizeValue(PhoneTextBox.Text),
            NormalizeValue(LocationTextBox.Text),
            NormalizeValue(LinkedInUrlTextBox.Text),
            NormalizeValue(PortfolioUrlTextBox.Text),
            NormalizeValue(GitHubUrlTextBox.Text),
            ShortSummaryTextBox.Text?.Trim(),
            PhotoPath: ProfilePhotoStorage.FileExists(_profilePhotoPath) ? _profilePhotoPath : null,
            GetActiveWorkExperienceEntries(),
            GetActiveEducationEntries(),
            GetActiveSkillsPreviewGroups(),
            GetActiveLanguagePreviewEntries(),
            GetActiveCertificatePreviewEntries(),
            GetActiveProjectPreviewEntries(),
            GetActiveCustomLinkLines(),
            GetAdditionalInformationContent());
    }

    private string? GetAdditionalInformationContent()
    {
        var content = AdditionalInformationSection.ContentModel.Content?.Trim();
        return string.IsNullOrWhiteSpace(content) ? null : content;
    }

    private static string NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private Control BuildClassicSidebarTemplate(CvExportDocument document)
    {
        var root = CreatePreviewRoot();
        root.ColumnDefinitions = new ColumnDefinitions("0.36*,0.64*");

        var sidebarContent = new StackPanel { Spacing = 14 };
        sidebarContent.Children.Add(ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
            document,
            88,
            Brush.Parse("#B8B8B8"),
            Brushes.White));
        sidebarContent.Children.Add(CreateNameBlock(document.FirstName, document.LastName, "#F47C2C", stacked: true));
        sidebarContent.Children.Add(CreateContactSection(document));

        var content = CreateContentStack();
        content.Children.Add(CreateSection(document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document)));
        AddWorkExperienceSection(content, document);
        AddEducationSection(content, document);
        AddSkillsSection(content, document);
        AddLanguagesSection(content, document);
        AddCertificatesSection(content, document);
        AddProjectsSection(content, document);
        AddCustomLinksSection(content, document);
        AddAdditionalInformationSection(content, document);
        content.Children.Add(CreateSection(document.Labels.ContactLinks, CvExportPreviewContentBuilder.BuildContactLinksLines(document)));

        root.Children.Add(CreateSidebarPanel(Brush.Parse("#D8D8D8"), sidebarContent));
        var contentPanel = WrapContentPanel(content);
        Grid.SetColumn(contentPanel, 1);
        root.Children.Add(contentPanel);

        return root;
    }

    private Control BuildModernSidebarTemplate(CvExportDocument document)
    {
        var root = CreatePreviewRoot();
        root.ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*");

        var sidebarContent = new StackPanel { Spacing = 14 };
        sidebarContent.Children.Add(ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
            document,
            88,
            Brush.Parse("#BBBBBB"),
            Brush.Parse("#333333")));
        sidebarContent.Children.Add(CreateSection(document.Labels.Contact, CvExportPreviewContentBuilder.BuildLines(
            document.Labels.Phone, document.Phone,
            document.Labels.Email, document.Email,
            document.Labels.LinkedInUrl, document.LinkedInUrl)));

        var content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        content.Children.Add(
            new Border
            {
                Background = Brush.Parse("#4A4A4A"),
                Padding = new Thickness(TemplateContentPadding, 12),
                Child = CreateText(document.FullName, 26, Brushes.White, FontWeight.Bold)
            });

        var body = CreateContentStack();
        body.Children.Add(CreateSection(document.Labels.Profile, CvExportPreviewContentBuilder.BuildSummary(document)));
        AddWorkExperienceSection(body, document);
        AddEducationSection(body, document);
        AddSkillsSection(body, document);
        AddLanguagesSection(body, document);
        AddCertificatesSection(body, document);
        AddProjectsSection(body, document);
        AddCustomLinksSection(body, document);
        AddAdditionalInformationSection(body, document);
        body.Children.Add(CreateSection(document.Labels.Digital, CvExportPreviewContentBuilder.BuildDigitalLines(document)));
        var wrappedBody = WrapContentPanel(body);
        Grid.SetRow(wrappedBody, 1);
        content.Children.Add(wrappedBody);

        root.Children.Add(CreateSidebarPanel(Brush.Parse("#D7D7D7"), sidebarContent));
        Grid.SetColumn(content, 1);
        root.Children.Add(content);

        return root;
    }

    private Control BuildCleanTopHeaderTemplate(CvExportDocument document)
    {
        var root = new StackPanel
        {
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,0.55*,0.45*")
        };

        var headerPhoto = ProfilePhotoPreviewFactory.CreateHeaderPhotoIfPresent(document, 72);
        var nameColumnIndex = 0;
        if (headerPhoto is not null)
        {
            header.Children.Add(headerPhoto);
            nameColumnIndex = 1;
            Grid.SetColumn(headerPhoto, 0);
        }
        else
        {
            header.ColumnDefinitions = new ColumnDefinitions("0.55*,0.45*");
        }

        var namePanel = new StackPanel { Spacing = 6 };
        namePanel.Children.Add(CreateText(document.FullName, 30, Brushes.White, FontWeight.Bold));
        namePanel.Children.Add(CreateText(document.ProfessionalTitle, 14, Brushes.White, FontWeight.SemiBold));
        Grid.SetColumn(namePanel, nameColumnIndex);
        header.Children.Add(namePanel);

        var contact = new StackPanel { Spacing = 3 };
        contact.Children.Add(CreateText($"{document.Labels.Email}: {document.Email}", 11, Brushes.White, FontWeight.SemiBold));
        contact.Children.Add(CreateText($"{document.Labels.Phone}: {document.Phone}", 11, Brushes.White, FontWeight.SemiBold));
        contact.Children.Add(CreateText($"{document.Labels.Location}: {document.Location}", 11, Brushes.White, FontWeight.SemiBold));
        Grid.SetColumn(contact, nameColumnIndex + 1);
        header.Children.Add(contact);

        root.Children.Add(
            new Border
            {
                Background = Brush.Parse("#5A9BD5"),
                Padding = new Thickness(28),
                Child = header
            });

        var body = CreateContentStack();
        body.Children.Add(CreateSection(document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document)));
        AddWorkExperienceSection(body, document);
        AddEducationSection(body, document);
        AddSkillsSection(body, document);
        AddLanguagesSection(body, document);
        AddCertificatesSection(body, document);
        AddProjectsSection(body, document);
        AddCustomLinksSection(body, document);
        AddAdditionalInformationSection(body, document);
        body.Children.Add(CreateSection(document.Labels.Links, CvExportPreviewContentBuilder.BuildLinksLines(document)));
        root.Children.Add(WrapContentPanel(body));

        return root;
    }

    private Control BuildDarkSidebarAccentTemplate(CvExportDocument document)
    {
        var root = CreatePreviewRoot();
        root.ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*");

        var sidebarContent = new StackPanel { Spacing = 16 };
        sidebarContent.Children.Add(ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
            document,
            88,
            Brush.Parse("#5B9BB0"),
            Brushes.White));
        sidebarContent.Children.Add(CreateText(document.Labels.Contact.ToUpperInvariant(), 16, Brushes.White, FontWeight.Bold));
        sidebarContent.Children.Add(CreateText(CvExportPreviewContentBuilder.BuildContactLines(document), 11, Brushes.White, FontWeight.Normal));

        var content = new StackPanel
        {
            Background = Brush.Parse("#F2F2F2"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        content.Children.Add(
            new Border
            {
                Background = Brush.Parse("#5B9BB0"),
                Padding = new Thickness(20),
                Child = new StackPanel
                {
                    Children =
                    {
                        CreateText(document.FullName.ToUpperInvariant(), 28, Brushes.White, FontWeight.Bold),
                        CreateText(document.ProfessionalTitle.ToUpperInvariant(), 14, Brushes.White, FontWeight.SemiBold)
                    }
                }
            });

        var body = CreateContentStack();
        body.Background = Brush.Parse("#F2F2F2");
        body.Children.Add(CreateSection(document.Labels.Objective, CvExportPreviewContentBuilder.BuildSummary(document)));
        AddWorkExperienceSection(body, document);
        AddEducationSection(body, document);
        AddSkillsSection(body, document);
        AddLanguagesSection(body, document);
        AddCertificatesSection(body, document);
        AddProjectsSection(body, document);
        AddCustomLinksSection(body, document);
        AddAdditionalInformationSection(body, document);
        body.Children.Add(CreateSection(document.Labels.Online, CvExportPreviewContentBuilder.BuildOnlineLines(document)));
        content.Children.Add(WrapContentPanel(body, Brush.Parse("#F2F2F2")));

        root.Children.Add(CreateSidebarPanel(Brush.Parse("#2F3A45"), sidebarContent));
        Grid.SetColumn(content, 1);
        root.Children.Add(content);

        return root;
    }

    private static Grid CreatePreviewRoot()
    {
        return new Grid
        {
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
    }

    private static StackPanel CreateContentStack()
    {
        return new StackPanel
        {
            Spacing = 18,
            Background = Brushes.White
        };
    }

    private static Border CreateSidebarPanel(IBrush background, Control content)
    {
        return new Border
        {
            Background = background,
            Padding = new Thickness(TemplateContentPadding),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = content
        };
    }

    private static Border WrapContentPanel(StackPanel content, IBrush? background = null)
    {
        return new Border
        {
            Background = background ?? Brushes.White,
            Padding = new Thickness(TemplateContentPadding),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = content
        };
    }

    private static Control CreateNameBlock(string firstName, string lastName, string accentColor, bool stacked)
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(CreateText(firstName, 24, Brushes.Black, FontWeight.Bold));
        panel.Children.Add(CreateText(lastName, 24, Brush.Parse(accentColor), FontWeight.Bold));
        return panel;
    }

    private Control CreateContactSection(CvExportDocument document)
    {
        return CreateSection(document.Labels.Contact, CvExportPreviewContentBuilder.BuildContactLines(document));
    }

    private void AddWorkExperienceSection(StackPanel panel, CvExportDocument document)
    {
        if (document.WorkExperienceEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(document.Labels.PreviewWorkExperience, BuildWorkExperiencePreviewContent(document)));
    }

    private static string BuildWorkExperiencePreviewContent(CvExportDocument document)
    {
        return CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document);
    }

    private void AddEducationSection(StackPanel panel, CvExportDocument document)
    {
        if (document.EducationEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(document.Labels.PreviewEducation, BuildEducationPreviewContent(document)));
    }

    private static string BuildEducationPreviewContent(CvExportDocument document)
    {
        return CvExportPreviewContentBuilder.BuildEducationPreviewContent(document);
    }

    private void AddSkillsSection(StackPanel panel, CvExportDocument document)
    {
        if (document.SkillsGroups.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(document.Labels.PreviewSkills, BuildSkillsPreviewContent(document)));
    }

    private static string BuildSkillsPreviewContent(CvExportDocument document)
    {
        return CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document);
    }

    private void AddLanguagesSection(StackPanel panel, CvExportDocument document)
    {
        if (document.LanguageEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(document.Labels.PreviewLanguages, BuildLanguagesPreviewContent(document)));
    }

    private static string BuildLanguagesPreviewContent(CvExportDocument document)
    {
        return CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document);
    }

    private void AddCertificatesSection(StackPanel panel, CvExportDocument document)
    {
        if (document.CertificateEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(document.Labels.PreviewCertificates, BuildCertificatesPreviewContent(document)));
    }

    private static string BuildCertificatesPreviewContent(CvExportDocument document)
    {
        return CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document);
    }

    private void AddProjectsSection(StackPanel panel, CvExportDocument document)
    {
        if (document.ProjectEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(document.Labels.PreviewProjects, BuildProjectsPreviewContent(document)));
    }

    private static string BuildProjectsPreviewContent(CvExportDocument document)
    {
        return CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document);
    }

    private void AddCustomLinksSection(StackPanel panel, CvExportDocument document)
    {
        if (document.CustomLinkLines.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(document.Labels.PreviewCustomLinks, BuildCustomLinksPreviewContent(document)));
    }

    private static string BuildCustomLinksPreviewContent(CvExportDocument document)
    {
        return CvExportPreviewContentBuilder.BuildCustomLinksPreviewContent(document);
    }

    private void AddAdditionalInformationSection(StackPanel panel, CvExportDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.AdditionalInformationContent))
        {
            return;
        }

        panel.Children.Add(CreateSection(document.Labels.PreviewAdditionalInformation, BuildAdditionalInformationPreviewContent(document)));
    }

    private static string BuildAdditionalInformationPreviewContent(CvExportDocument document)
    {
        return CvExportPreviewContentBuilder.BuildAdditionalInformationPreviewContent(document);
    }

    private static Control CreateSection(string title, string content)
    {
        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(CreateText(title, 18, Brushes.Black, FontWeight.Bold));
        panel.Children.Add(
            new Border
            {
                Height = 1,
                Background = Brush.Parse("#B8B8B8")
            });
        panel.Children.Add(CreateText(content, 12, Brushes.Black, FontWeight.Normal));
        return panel;
    }

    private static TextBlock CreateText(string text, double fontSize, IBrush foreground, FontWeight fontWeight)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            Foreground = foreground,
            FontWeight = fontWeight,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.None
        };
    }

    private static string NormalizeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}