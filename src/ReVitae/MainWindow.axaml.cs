using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using ReVitae.Core.Cv;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
    private bool _isUpdatingLanguageSelection;
    private CvTemplateId _selectedTemplate = CvTemplateId.CleanTopHeader;

    private enum CvTemplateId
    {
        ClassicSidebar,
        ModernSidebar,
        CleanTopHeader,
        DarkSidebarAccent
    }

    private sealed record WorkExperiencePreviewEntry(
        string JobTitle,
        string Company,
        string Location,
        string EmploymentTypeLabel,
        string DateRange,
        string? Description,
        string? Achievements,
        string? Technologies,
        string? CompanyUrl);

    private sealed record EducationPreviewEntry(
        string Degree,
        string Institution,
        string FieldOfStudy,
        string Location,
        string DegreeTypeLabel,
        string DateRange,
        string? Grade,
        string? Description,
        string? InstitutionUrl);

    private sealed record SkillPreviewItem(
        string Name,
        string ProficiencyLabel,
        int? YearsOfExperience);

    private sealed record SkillsPreviewGroup(
        string Category,
        IReadOnlyList<SkillPreviewItem> Skills);

    private sealed record LanguagePreviewEntry(
        string MainLine,
        IReadOnlyList<string> SubSkillLines);

    private sealed record CertificatePreviewEntry(
        string MainLine,
        IReadOnlyList<string> DetailLines);

    private sealed record CvTemplateData(
        string FirstName,
        string LastName,
        string ProfessionalTitle,
        string Email,
        string Phone,
        string Location,
        string LinkedInUrl,
        string PortfolioUrl,
        string GitHubUrl,
        string? ShortSummary,
        string? PhotoPath,
        IReadOnlyList<WorkExperiencePreviewEntry> WorkExperienceEntries,
        IReadOnlyList<EducationPreviewEntry> EducationEntries,
        IReadOnlyList<SkillsPreviewGroup> SkillsGroups,
        IReadOnlyList<LanguagePreviewEntry> LanguageEntries,
        IReadOnlyList<CertificatePreviewEntry> CertificateEntries)
    {
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    public MainWindow()
    {
        InitializeComponent();
        InitializeLanguageSelector();
        WorkExperienceSection.EntriesChanged += OnWorkExperienceChanged;
        EducationSection.EntriesChanged += OnEducationChanged;
        SkillsSection.EntriesChanged += OnSkillsChanged;
        LanguagesSection.EntriesChanged += OnLanguagesChanged;
        CertificatesSection.EntriesChanged += OnCertificatesChanged;
        ApplyLocalization();
        UpdateTemplateSelectionState();
        UpdatePreview();
        UpdateValidationState();
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

    private void OnFormTextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdatePreview();
        UpdateValidationState();
        ExportStatusTextBlock.Text = string.Empty;
    }

    private void OnOpenSetupClicked(object? sender, RoutedEventArgs e)
    {
        SetSetupModalVisible(true);
    }

    private void OnOpenTemplatesClicked(object? sender, RoutedEventArgs e)
    {
        UpdateTemplateSelectionState();
        SetTemplatesModalVisible(true);
    }

    private void OnOpenPreviewExpandClicked(object? sender, RoutedEventArgs e)
    {
        UpdatePreview();
        SetPreviewExpandModalVisible(true);
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
        SelectTemplate(CvTemplateId.ClassicSidebar);
    }

    private void OnSelectModernSidebarTemplateClicked(object? sender, RoutedEventArgs e)
    {
        SelectTemplate(CvTemplateId.ModernSidebar);
    }

    private void OnSelectCleanTopHeaderTemplateClicked(object? sender, RoutedEventArgs e)
    {
        SelectTemplate(CvTemplateId.CleanTopHeader);
    }

    private void OnSelectDarkSidebarTemplateClicked(object? sender, RoutedEventArgs e)
    {
        SelectTemplate(CvTemplateId.DarkSidebarAccent);
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        if (TemplatesModalOverlay.IsVisible)
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
        var validationResult = ValidateForm();
        if (!validationResult.IsValid)
        {
            UpdateValidationState(validationResult);
            ExportStatusTextBlock.Text = _localizer.Get(TranslationKeys.ExportFixValidation);
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            ExportStatusTextBlock.Text = _localizer.Get(TranslationKeys.ExportFilePickerUnavailable);
            return;
        }

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Export PDF",
                SuggestedFileName = "revitae-basic-cv.pdf",
                DefaultExtension = "pdf",
                FileTypeChoices =
                [
                    new FilePickerFileType("PDF")
                    {
                        Patterns = ["*.pdf"],
                        MimeTypes = ["application/pdf"]
                    }
                ]
            });

        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        var pdfBytes = CreatePdfBytes(BuildPreviewLines());
        await stream.WriteAsync(pdfBytes);

        ExportStatusTextBlock.Text = _localizer.Format(TranslationKeys.ExportedPdfTo, file.Name);
    }

    private void ApplyLocalization()
    {
        HeaderSubtitleTextBlock.Text = _localizer.Get(TranslationKeys.HeaderSubtitle);
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
        FirstNameLabelTextBlock.Text = _localizer.Get(TranslationKeys.FirstName);
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
        ExportPdfButton.Content = _localizer.Get(TranslationKeys.ExportPdf);
        PreviewTitleTextBlock.Text = _localizer.Get(TranslationKeys.Preview);
        PreviewExpandTitleTextBlock.Text = _localizer.Get(TranslationKeys.PreviewExpandTitle);
        PreviewExpandTopCloseButton.Content = _localizer.Get(TranslationKeys.Close);
        PreviewExpandBottomCloseButton.Content = _localizer.Get(TranslationKeys.Close);

        SetupTitleTextBlock.Text = _localizer.Get(TranslationKeys.Setup);
        SetupPlaceholderTextBlock.Text = _localizer.Get(TranslationKeys.SetupPlaceholder);
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

    private void SelectTemplate(CvTemplateId templateId)
    {
        _selectedTemplate = templateId;
        UpdateTemplateSelectionState();
        UpdatePreview();
        SetTemplatesModalVisible(false);
    }

    private void UpdateTemplateSelectionState()
    {
        ClassicSidebarSelectedTextBlock.IsVisible = _selectedTemplate == CvTemplateId.ClassicSidebar;
        ModernSidebarSelectedTextBlock.IsVisible = _selectedTemplate == CvTemplateId.ModernSidebar;
        CleanTopHeaderSelectedTextBlock.IsVisible = _selectedTemplate == CvTemplateId.CleanTopHeader;
        DarkSidebarSelectedTextBlock.IsVisible = _selectedTemplate == CvTemplateId.DarkSidebarAccent;

        ClassicSidebarTemplateButton.Classes.Set("selected", _selectedTemplate == CvTemplateId.ClassicSidebar);
        ModernSidebarTemplateButton.Classes.Set("selected", _selectedTemplate == CvTemplateId.ModernSidebar);
        CleanTopHeaderTemplateButton.Classes.Set("selected", _selectedTemplate == CvTemplateId.CleanTopHeader);
        DarkSidebarTemplateButton.Classes.Set("selected", _selectedTemplate == CvTemplateId.DarkSidebarAccent);
    }

    private void SetSetupModalVisible(bool isVisible)
    {
        SetupModalOverlay.IsVisible = isVisible;
        if (isVisible)
        {
            TemplatesModalOverlay.IsVisible = false;
            PreviewExpandModalOverlay.IsVisible = false;
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
    }

    private void UpdateValidationState(FieldValidationResult? validationResult = null)
    {
        validationResult ??= ValidateForm();

        ExportPdfButton.IsEnabled = validationResult.IsValid;
        UpdateFieldErrorMessages(validationResult);
        WorkExperienceSection.UpdateValidation(validationResult);
        EducationSection.UpdateValidation(validationResult);
        SkillsSection.UpdateValidation(validationResult);
        LanguagesSection.UpdateValidation(validationResult);
        CertificatesSection.UpdateValidation(validationResult);
        ValidationSummaryTextBlock.Text = validationResult.IsValid
            ? string.Empty
            : string.Join(Environment.NewLine, validationResult.Errors.Select(error => _localizer.Get(error.Message)));
    }

    private void UpdateFieldErrorMessages(FieldValidationResult validationResult)
    {
        var errorsByField = validationResult.Errors
            .GroupBy(error => error.FieldKey)
            .ToDictionary(
                group => group.Key,
                group => string.Join(Environment.NewLine, group.Select(error => error.Message)),
                StringComparer.Ordinal);

        FirstNameErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.FirstName);
        LastNameErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.LastName);
        ProfessionalTitleErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.ProfessionalTitle);
        EmailErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.Email);
        PhoneErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.Phone);
        LocationErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.Location);
        LinkedInUrlErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.LinkedInUrl);
        PortfolioUrlErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.PortfolioUrl);
        GitHubUrlErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.GitHubUrl);
        ShortSummaryErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.ShortSummary);
    }

    private string GetFieldError(IReadOnlyDictionary<string, string> errorsByField, string fieldKey)
    {
        return errorsByField.TryGetValue(fieldKey, out var error) ? _localizer.Get(error) : string.Empty;
    }

    private FieldValidationResult ValidateForm()
    {
        var personalResult = _validator.Validate(BuildFieldValues());
        var workExperienceResult = _workExperienceValidator.Validate(WorkExperienceSection.Entries.ToArray());
        var educationResult = _educationValidator.Validate(EducationSection.Entries.ToArray());
        var skillsResult = _skillsValidator.Validate(SkillsSection.Entries.ToArray());
        var languagesResult = _languagesValidator.Validate(LanguagesSection.Entries.ToArray());
        var certificatesResult = _certificatesValidator.Validate(CertificatesSection.Entries.ToArray());
        var combinedErrors = personalResult.Errors
            .Concat(workExperienceResult.Errors)
            .Concat(educationResult.Errors)
            .Concat(skillsResult.Errors)
            .Concat(languagesResult.Errors)
            .Concat(certificatesResult.Errors)
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

    private string[] BuildPreviewLines()
    {
        var lines = new List<string>
        {
            BuildFullName(),
            NormalizeValue(ProfessionalTitleTextBox.Text),
            string.Empty,
            $"{_localizer.Get(TranslationKeys.Email)}: {NormalizeValue(EmailTextBox.Text)}",
            $"{_localizer.Get(TranslationKeys.Phone)}: {NormalizeValue(PhoneTextBox.Text)}",
            $"{_localizer.Get(TranslationKeys.Location)}: {NormalizeValue(LocationTextBox.Text)}",
            $"{_localizer.Get(TranslationKeys.LinkedInUrl)}: {NormalizeValue(LinkedInUrlTextBox.Text)}",
            $"{_localizer.Get(TranslationKeys.PortfolioUrl)}: {NormalizeValue(PortfolioUrlTextBox.Text)}",
            $"{_localizer.Get(TranslationKeys.GitHubUrl)}: {NormalizeValue(GitHubUrlTextBox.Text)}",
            string.Empty,
            $"{_localizer.Get(TranslationKeys.Summary)}:"
        };

        lines.AddRange(BuildSummaryLines());
        lines.AddRange(BuildWorkExperiencePdfLines());
        lines.AddRange(BuildEducationPdfLines());
        lines.AddRange(BuildSkillsPdfLines());
        lines.AddRange(BuildLanguagesPdfLines());
        lines.AddRange(BuildCertificatesPdfLines());

        return lines.ToArray();
    }

    private IEnumerable<string> BuildWorkExperiencePdfLines()
    {
        var activeEntries = GetActiveWorkExperienceEntries();
        if (activeEntries.Count == 0)
        {
            return Array.Empty<string>();
        }

        var lines = new List<string>
        {
            string.Empty,
            _localizer.Get(TranslationKeys.PreviewWorkExperience)
        };

        foreach (var entry in activeEntries)
        {
            lines.Add(string.Empty);
            lines.Add(entry.JobTitle);
            lines.Add(BuildWorkExperienceMetaLine(entry));
            AppendMultilineBlock(lines, entry.Description);
            if (!string.IsNullOrWhiteSpace(entry.Achievements))
            {
                lines.Add(_localizer.Get(TranslationKeys.PreviewAchievements) + ":");
                AppendMultilineBlock(lines, entry.Achievements);
            }

            if (!string.IsNullOrWhiteSpace(entry.Technologies))
            {
                lines.Add($"{_localizer.Get(TranslationKeys.PreviewTechnologies)}: {entry.Technologies}");
            }

            if (!string.IsNullOrWhiteSpace(entry.CompanyUrl))
            {
                lines.Add($"{_localizer.Get(TranslationKeys.WorkExperienceCompanyUrl)}: {entry.CompanyUrl}");
            }
        }

        return lines;
    }

    private static void AppendMultilineBlock(List<string> lines, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        lines.AddRange(
            value.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.None));
    }

    private string BuildWorkExperienceMetaLine(WorkExperiencePreviewEntry entry)
    {
        var parts = new List<string> { entry.Company };
        if (!string.IsNullOrWhiteSpace(entry.Location) && entry.Location != "-")
        {
            parts.Add(entry.Location);
        }

        parts.Add(entry.EmploymentTypeLabel);
        parts.Add(entry.DateRange);
        return string.Join(" · ", parts);
    }

    private IEnumerable<string> BuildEducationPdfLines()
    {
        var activeEntries = GetActiveEducationEntries();
        if (activeEntries.Count == 0)
        {
            return Array.Empty<string>();
        }

        var lines = new List<string>
        {
            string.Empty,
            _localizer.Get(TranslationKeys.PreviewEducation)
        };

        foreach (var entry in activeEntries)
        {
            lines.Add(string.Empty);
            lines.Add(entry.Degree);
            lines.Add(BuildEducationMetaLine(entry));
            if (!string.IsNullOrWhiteSpace(entry.FieldOfStudy))
            {
                lines.Add($"{_localizer.Get(TranslationKeys.PreviewFieldOfStudy)}: {entry.FieldOfStudy}");
            }

            AppendMultilineBlock(lines, entry.Description);
            if (!string.IsNullOrWhiteSpace(entry.Grade))
            {
                lines.Add($"{_localizer.Get(TranslationKeys.PreviewGrade)}: {entry.Grade}");
            }

            if (!string.IsNullOrWhiteSpace(entry.InstitutionUrl))
            {
                lines.Add($"{_localizer.Get(TranslationKeys.EducationInstitutionUrl)}: {entry.InstitutionUrl}");
            }
        }

        return lines;
    }

    private string BuildEducationMetaLine(EducationPreviewEntry entry)
    {
        var parts = new List<string> { entry.Institution };
        if (!string.IsNullOrWhiteSpace(entry.Location) && entry.Location != "-")
        {
            parts.Add(entry.Location);
        }

        parts.Add(entry.DegreeTypeLabel);
        parts.Add(entry.DateRange);
        return string.Join(" · ", parts);
    }

    private IReadOnlyList<EducationPreviewEntry> GetActiveEducationEntries()
    {
        return EducationSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(BuildEducationPreviewEntry)
            .ToArray();
    }

    private EducationPreviewEntry BuildEducationPreviewEntry(EducationEntry entry)
    {
        return new EducationPreviewEntry(
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

    private IEnumerable<string> BuildSkillsPdfLines()
    {
        var groups = GetActiveSkillsPreviewGroups();
        if (groups.Count == 0)
        {
            return Array.Empty<string>();
        }

        var lines = new List<string>
        {
            string.Empty,
            _localizer.Get(TranslationKeys.PreviewSkills)
        };

        foreach (var group in groups)
        {
            lines.Add(string.Empty);
            lines.Add(group.Category);
            foreach (var skill in group.Skills)
            {
                lines.Add(FormatSkillPreviewLine(skill));
            }
        }

        return lines;
    }

    private string FormatSkillPreviewLine(SkillPreviewItem skill)
    {
        var parts = new List<string> { skill.Name, skill.ProficiencyLabel };
        if (skill.YearsOfExperience is not null)
        {
            parts.Add($"{skill.YearsOfExperience} {_localizer.Get(TranslationKeys.PreviewYearsSuffix)}");
        }

        return string.Join(" · ", parts);
    }

    private IReadOnlyList<SkillsPreviewGroup> GetActiveSkillsPreviewGroups()
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

    private SkillsPreviewGroup BuildSkillsPreviewGroup(SkillsGroupEntry group)
    {
        var skills = group.Skills
            .Where(skill => skill.HasUserInput())
            .Select(skill => new SkillPreviewItem(
                skill.Name.Trim(),
                _localizer.Get(skill.Proficiency.ToTranslationKey()),
                skill.YearsOfExperience))
            .ToArray();

        return new SkillsPreviewGroup(
            NormalizeValue(group.Category),
            skills);
    }

    private IEnumerable<string> BuildLanguagesPdfLines()
    {
        var entries = GetActiveLanguagePreviewEntries();
        if (entries.Count == 0)
        {
            return Array.Empty<string>();
        }

        var lines = new List<string>
        {
            string.Empty,
            _localizer.Get(TranslationKeys.PreviewLanguages)
        };

        foreach (var entry in entries)
        {
            lines.Add(string.Empty);
            lines.Add(entry.MainLine);
            lines.AddRange(entry.SubSkillLines);
        }

        return lines;
    }

    private IReadOnlyList<LanguagePreviewEntry> GetActiveLanguagePreviewEntries()
    {
        return LanguagesSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(BuildLanguagePreviewEntry)
            .ToArray();
    }

    private LanguagePreviewEntry BuildLanguagePreviewEntry(LanguageEntry entry)
    {
        return new LanguagePreviewEntry(
            LanguagePreviewFormatter.FormatMainLine(entry, _localizer),
            LanguagePreviewFormatter.FormatSubSkillLines(entry, _localizer));
    }

    private IEnumerable<string> BuildCertificatesPdfLines()
    {
        var entries = GetActiveCertificatePreviewEntries();
        if (entries.Count == 0)
        {
            return Array.Empty<string>();
        }

        var lines = new List<string>
        {
            string.Empty,
            _localizer.Get(TranslationKeys.PreviewCertificates)
        };

        foreach (var entry in entries)
        {
            lines.Add(string.Empty);
            lines.Add(entry.MainLine);
            lines.AddRange(entry.DetailLines);
        }

        return lines;
    }

    private IReadOnlyList<CertificatePreviewEntry> GetActiveCertificatePreviewEntries()
    {
        return CertificatesSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(BuildCertificatePreviewEntry)
            .ToArray();
    }

    private CertificatePreviewEntry BuildCertificatePreviewEntry(CertificateEntry entry)
    {
        return new CertificatePreviewEntry(
            CertificatePreviewFormatter.FormatMainLine(entry, _localizer),
            CertificatePreviewFormatter.FormatDetailLines(entry, _localizer));
    }

    private IReadOnlyList<WorkExperiencePreviewEntry> GetActiveWorkExperienceEntries()
    {
        return WorkExperienceSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(BuildWorkExperiencePreviewEntry)
            .ToArray();
    }

    private WorkExperiencePreviewEntry BuildWorkExperiencePreviewEntry(WorkExperienceEntry entry)
    {
        return new WorkExperiencePreviewEntry(
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

    private string BuildFullName()
    {
        var nameParts = new[]
        {
            FirstNameTextBox.Text?.Trim(),
            LastNameTextBox.Text?.Trim()
        };

        var fullName = string.Join(
            " ",
            Array.FindAll(nameParts, part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(fullName) ? "-" : fullName;
    }

    private string[] BuildSummaryLines()
    {
        var summary = ShortSummaryTextBox.Text;
        if (string.IsNullOrWhiteSpace(summary))
        {
            return new[] { "-" };
        }

        return summary
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None);
    }

    private Control BuildTemplatePreview()
    {
        var data = BuildTemplateData();

        return _selectedTemplate switch
        {
            CvTemplateId.ClassicSidebar => BuildClassicSidebarTemplate(data),
            CvTemplateId.ModernSidebar => BuildModernSidebarTemplate(data),
            CvTemplateId.CleanTopHeader => BuildCleanTopHeaderTemplate(data),
            CvTemplateId.DarkSidebarAccent => BuildDarkSidebarAccentTemplate(data),
            _ => throw new ArgumentOutOfRangeException(nameof(_selectedTemplate))
        };
    }

    private CvTemplateData BuildTemplateData()
    {
        return new CvTemplateData(
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
            PhotoPath: null,
            GetActiveWorkExperienceEntries(),
            GetActiveEducationEntries(),
            GetActiveSkillsPreviewGroups(),
            GetActiveLanguagePreviewEntries(),
            GetActiveCertificatePreviewEntries());
    }

    private static string NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private Control BuildClassicSidebarTemplate(CvTemplateData data)
    {
        var root = CreatePreviewRoot();
        root.ColumnDefinitions = new ColumnDefinitions("0.36*,0.64*");

        var sidebarContent = new StackPanel { Spacing = 14 };
        sidebarContent.Children.Add(CreateNameBlock(data.FirstName, data.LastName, "#F47C2C", stacked: true));
        sidebarContent.Children.Add(CreateContactSection(data));

        var content = CreateContentStack();
        content.Children.Add(CreateSection(_localizer.Get(TranslationKeys.Summary), GetSummary(data)));
        AddWorkExperienceSection(content, data);
        AddEducationSection(content, data);
        AddSkillsSection(content, data);
        AddLanguagesSection(content, data);
        AddCertificatesSection(content, data);
        content.Children.Add(CreateSection(_localizer.Get(TranslationKeys.ContactLinks), BuildLines(_localizer.Get(TranslationKeys.LinkedInUrl), data.LinkedInUrl, _localizer.Get(TranslationKeys.PortfolioUrl), data.PortfolioUrl, _localizer.Get(TranslationKeys.GitHubUrl), data.GitHubUrl)));

        root.Children.Add(CreateSidebarPanel(Brush.Parse("#D8D8D8"), sidebarContent));
        var contentPanel = WrapContentPanel(content);
        Grid.SetColumn(contentPanel, 1);
        root.Children.Add(contentPanel);

        return root;
    }

    private Control BuildModernSidebarTemplate(CvTemplateData data)
    {
        var root = CreatePreviewRoot();
        root.ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*");

        var sidebarContent = new StackPanel { Spacing = 14 };
        sidebarContent.Children.Add(CreateSection(_localizer.Get(TranslationKeys.Contact), BuildLines(_localizer.Get(TranslationKeys.Phone), data.Phone, _localizer.Get(TranslationKeys.Email), data.Email, _localizer.Get(TranslationKeys.LinkedInUrl), data.LinkedInUrl)));

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
                Child = CreateText(data.FullName, 26, Brushes.White, FontWeight.Bold)
            });

        var body = CreateContentStack();
        body.Children.Add(CreateSection(_localizer.Get(TranslationKeys.Profile), GetSummary(data)));
        AddWorkExperienceSection(body, data);
        AddEducationSection(body, data);
        AddSkillsSection(body, data);
        AddLanguagesSection(body, data);
        AddCertificatesSection(body, data);
        body.Children.Add(CreateSection(_localizer.Get(TranslationKeys.Digital), BuildLines(_localizer.Get(TranslationKeys.PortfolioUrl), data.PortfolioUrl, _localizer.Get(TranslationKeys.GitHubUrl), data.GitHubUrl)));
        Grid.SetRow(WrapContentPanel(body), 1);
        content.Children.Add(WrapContentPanel(body));

        root.Children.Add(CreateSidebarPanel(Brush.Parse("#D7D7D7"), sidebarContent));
        Grid.SetColumn(content, 1);
        root.Children.Add(content);

        return root;
    }

    private Control BuildCleanTopHeaderTemplate(CvTemplateData data)
    {
        var root = new StackPanel
        {
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("0.55*,0.45*")
        };

        var namePanel = new StackPanel { Spacing = 6 };
        namePanel.Children.Add(CreateText(data.FullName, 30, Brushes.White, FontWeight.Bold));
        namePanel.Children.Add(CreateText(data.ProfessionalTitle, 14, Brushes.White, FontWeight.SemiBold));
        header.Children.Add(namePanel);

        var contact = new StackPanel { Spacing = 3 };
        contact.Children.Add(CreateText($"{_localizer.Get(TranslationKeys.Email)}: {data.Email}", 11, Brushes.White, FontWeight.SemiBold));
        contact.Children.Add(CreateText($"{_localizer.Get(TranslationKeys.Phone)}: {data.Phone}", 11, Brushes.White, FontWeight.SemiBold));
        contact.Children.Add(CreateText($"{_localizer.Get(TranslationKeys.Location)}: {data.Location}", 11, Brushes.White, FontWeight.SemiBold));
        Grid.SetColumn(contact, 1);
        header.Children.Add(contact);

        root.Children.Add(
            new Border
            {
                Background = Brush.Parse("#5A9BD5"),
                Padding = new Thickness(28),
                Child = header
            });

        var body = CreateContentStack();
        body.Children.Add(CreateSection(_localizer.Get(TranslationKeys.Summary), GetSummary(data)));
        AddWorkExperienceSection(body, data);
        AddEducationSection(body, data);
        AddSkillsSection(body, data);
        AddLanguagesSection(body, data);
        AddCertificatesSection(body, data);
        body.Children.Add(CreateSection(_localizer.Get(TranslationKeys.Links), BuildLines(_localizer.Get(TranslationKeys.LinkedInUrl), data.LinkedInUrl, _localizer.Get(TranslationKeys.PortfolioUrl), data.PortfolioUrl, _localizer.Get(TranslationKeys.GitHubUrl), data.GitHubUrl)));
        root.Children.Add(WrapContentPanel(body));

        return root;
    }

    private Control BuildDarkSidebarAccentTemplate(CvTemplateData data)
    {
        var root = CreatePreviewRoot();
        root.ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*");

        var sidebarContent = new StackPanel { Spacing = 16 };
        sidebarContent.Children.Add(CreateText(_localizer.Get(TranslationKeys.Contact).ToUpperInvariant(), 16, Brushes.White, FontWeight.Bold));
        sidebarContent.Children.Add(CreateText(BuildLines(_localizer.Get(TranslationKeys.Email), data.Email, _localizer.Get(TranslationKeys.Phone), data.Phone, _localizer.Get(TranslationKeys.Location), data.Location), 11, Brushes.White, FontWeight.Normal));

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
                        CreateText(data.FullName.ToUpperInvariant(), 28, Brushes.White, FontWeight.Bold),
                        CreateText(data.ProfessionalTitle.ToUpperInvariant(), 14, Brushes.White, FontWeight.SemiBold)
                    }
                }
            });

        var body = CreateContentStack();
        body.Background = Brush.Parse("#F2F2F2");
        body.Children.Add(CreateSection(_localizer.Get(TranslationKeys.Objective), GetSummary(data)));
        AddWorkExperienceSection(body, data);
        AddEducationSection(body, data);
        AddSkillsSection(body, data);
        AddLanguagesSection(body, data);
        AddCertificatesSection(body, data);
        body.Children.Add(CreateSection(_localizer.Get(TranslationKeys.Online), BuildLines(_localizer.Get(TranslationKeys.LinkedInUrl), data.LinkedInUrl, _localizer.Get(TranslationKeys.PortfolioUrl), data.PortfolioUrl, _localizer.Get(TranslationKeys.GitHubUrl), data.GitHubUrl)));
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

    private Control CreateContactSection(CvTemplateData data)
    {
        return CreateSection(_localizer.Get(TranslationKeys.Contact), BuildLines(_localizer.Get(TranslationKeys.Email), data.Email, _localizer.Get(TranslationKeys.Phone), data.Phone, _localizer.Get(TranslationKeys.Location), data.Location));
    }

    private void AddWorkExperienceSection(StackPanel panel, CvTemplateData data)
    {
        if (data.WorkExperienceEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(_localizer.Get(TranslationKeys.PreviewWorkExperience), BuildWorkExperiencePreviewContent(data)));
    }

    private string BuildWorkExperiencePreviewContent(CvTemplateData data)
    {
        var entries = new List<string>();
        foreach (var entry in data.WorkExperienceEntries)
        {
            var block = new List<string>
            {
                entry.JobTitle,
                BuildWorkExperienceMetaLine(entry)
            };

            if (!string.IsNullOrWhiteSpace(entry.Description))
            {
                block.Add(entry.Description);
            }

            if (!string.IsNullOrWhiteSpace(entry.Achievements))
            {
                block.Add($"{_localizer.Get(TranslationKeys.PreviewAchievements)}:{Environment.NewLine}{entry.Achievements}");
            }

            if (!string.IsNullOrWhiteSpace(entry.Technologies))
            {
                block.Add($"{_localizer.Get(TranslationKeys.PreviewTechnologies)}: {entry.Technologies}");
            }

            if (!string.IsNullOrWhiteSpace(entry.CompanyUrl))
            {
                block.Add($"{_localizer.Get(TranslationKeys.WorkExperienceCompanyUrl)}: {entry.CompanyUrl}");
            }

            entries.Add(string.Join(Environment.NewLine, block));
        }

        return string.Join($"{Environment.NewLine}{Environment.NewLine}", entries);
    }

    private void AddEducationSection(StackPanel panel, CvTemplateData data)
    {
        if (data.EducationEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(_localizer.Get(TranslationKeys.PreviewEducation), BuildEducationPreviewContent(data)));
    }

    private string BuildEducationPreviewContent(CvTemplateData data)
    {
        var entries = new List<string>();
        foreach (var entry in data.EducationEntries)
        {
            var block = new List<string>
            {
                entry.Degree,
                BuildEducationMetaLine(entry)
            };

            if (!string.IsNullOrWhiteSpace(entry.FieldOfStudy))
            {
                block.Add($"{_localizer.Get(TranslationKeys.PreviewFieldOfStudy)}: {entry.FieldOfStudy}");
            }

            if (!string.IsNullOrWhiteSpace(entry.Description))
            {
                block.Add(entry.Description);
            }

            if (!string.IsNullOrWhiteSpace(entry.Grade))
            {
                block.Add($"{_localizer.Get(TranslationKeys.PreviewGrade)}: {entry.Grade}");
            }

            if (!string.IsNullOrWhiteSpace(entry.InstitutionUrl))
            {
                block.Add($"{_localizer.Get(TranslationKeys.EducationInstitutionUrl)}: {entry.InstitutionUrl}");
            }

            entries.Add(string.Join(Environment.NewLine, block));
        }

        return string.Join($"{Environment.NewLine}{Environment.NewLine}", entries);
    }

    private void AddSkillsSection(StackPanel panel, CvTemplateData data)
    {
        if (data.SkillsGroups.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(_localizer.Get(TranslationKeys.PreviewSkills), BuildSkillsPreviewContent(data)));
    }

    private string BuildSkillsPreviewContent(CvTemplateData data)
    {
        var groups = new List<string>();
        foreach (var group in data.SkillsGroups)
        {
            var lines = new List<string> { group.Category };
            lines.AddRange(group.Skills.Select(FormatSkillPreviewLine));
            groups.Add(string.Join(Environment.NewLine, lines));
        }

        return string.Join($"{Environment.NewLine}{Environment.NewLine}", groups);
    }

    private void AddLanguagesSection(StackPanel panel, CvTemplateData data)
    {
        if (data.LanguageEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(_localizer.Get(TranslationKeys.PreviewLanguages), BuildLanguagesPreviewContent(data)));
    }

    private string BuildLanguagesPreviewContent(CvTemplateData data)
    {
        var entries = new List<string>();
        foreach (var entry in data.LanguageEntries)
        {
            var block = new List<string> { entry.MainLine };
            block.AddRange(entry.SubSkillLines);
            entries.Add(string.Join(Environment.NewLine, block));
        }

        return string.Join($"{Environment.NewLine}{Environment.NewLine}", entries);
    }

    private void AddCertificatesSection(StackPanel panel, CvTemplateData data)
    {
        if (data.CertificateEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(
            _localizer.Get(TranslationKeys.PreviewCertificates),
            BuildCertificatesPreviewContent(data)));
    }

    private string BuildCertificatesPreviewContent(CvTemplateData data)
    {
        var entries = new List<string>();
        foreach (var entry in data.CertificateEntries)
        {
            var block = new List<string> { entry.MainLine };
            block.AddRange(entry.DetailLines);
            entries.Add(string.Join(Environment.NewLine, block));
        }

        return string.Join($"{Environment.NewLine}{Environment.NewLine}", entries);
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

    private static string BuildLines(params string[] labelValuePairs)
    {
        var lines = new List<string>();
        for (var index = 0; index < labelValuePairs.Length; index += 2)
        {
            var label = labelValuePairs[index];
            var value = labelValuePairs[index + 1];
            if (!string.IsNullOrWhiteSpace(value) && value != "-")
            {
                lines.Add($"{label}: {value}");
            }
        }

        return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
    }

    private static string GetSummary(CvTemplateData data)
    {
        return string.IsNullOrWhiteSpace(data.ShortSummary) ? "-" : data.ShortSummary;
    }

    private static string NormalizeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private static byte[] CreatePdfBytes(IReadOnlyList<string> lines)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 14 Tf");
        content.AppendLine("72 760 Td");

        for (var index = 0; index < lines.Count; index++)
        {
            if (index > 0)
            {
                content.AppendLine("0 -24 Td");
            }

            content.Append('(');
            content.Append(EscapePdfText(lines[index]));
            content.AppendLine(") Tj");
        }

        content.AppendLine("ET");

        var contentText = content.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(contentText)} >>\nstream\n{contentText}endstream"
        };

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
        var offsets = new List<long> { 0 };

        writer.WriteLine("%PDF-1.4");

        for (var index = 0; index < objects.Length; index++)
        {
            writer.Flush();
            offsets.Add(stream.Position);
            writer.WriteLine($"{index + 1} 0 obj");
            writer.WriteLine(objects[index]);
            writer.WriteLine("endobj");
        }

        writer.Flush();
        var xrefOffset = stream.Position;

        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Length + 1}");
        writer.WriteLine("0000000000 65535 f ");

        for (var index = 1; index < offsets.Count; index++)
        {
            writer.WriteLine($"{offsets[index]:D10} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Length + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefOffset);
        writer.WriteLine("%%EOF");
        writer.Flush();

        return stream.ToArray();
    }

    private static string EscapePdfText(string text)
    {
        return text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }
}