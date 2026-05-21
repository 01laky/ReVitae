using Avalonia.Controls;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Export;
using ReVitae.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using ExportEducationEntry = ReVitae.Core.Export.EducationEntry;
using ExportWorkExperienceEntry = ReVitae.Core.Export.WorkExperienceEntry;
using ExportSkillsGroup = ReVitae.Core.Export.SkillsGroup;
using ExportLanguageEntry = ReVitae.Core.Export.LanguageEntry;
using ExportCertificateEntry = ReVitae.Core.Export.CertificateEntry;
using ExportProjectEntry = ReVitae.Core.Export.ProjectEntry;

namespace ReVitae;

public partial class MainWindow
{
    private IReadOnlyList<ExportEducationEntry> GetActiveEducationEntries()
    {
        return EducationSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(entry => CvExportDocumentMapper.MapEducation(entry, _localizer))
            .ToArray();
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
            .Select(group => CvExportDocumentMapper.MapSkillsGroup(group, _localizer))
            .ToArray();
    }

    private IReadOnlyList<ExportLanguageEntry> GetActiveLanguagePreviewEntries()
    {
        return LanguagesSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(entry => CvExportDocumentMapper.MapLanguage(entry, _localizer))
            .ToArray();
    }

    private IReadOnlyList<ExportCertificateEntry> GetActiveCertificatePreviewEntries()
    {
        return CertificatesSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(entry => CvExportDocumentMapper.MapCertificate(entry, _localizer))
            .ToArray();
    }

    private IReadOnlyList<ExportProjectEntry> GetActiveProjectPreviewEntries()
    {
        return ProjectsSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(entry => CvExportDocumentMapper.MapProject(entry, _localizer))
            .ToArray();
    }

    private IReadOnlyList<string> GetActiveCustomLinkLines()
    {
        return LinksSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(CvExportDocumentMapper.MapLinkLine)
            .ToArray();
    }

    private IReadOnlyList<ExportWorkExperienceEntry> GetActiveWorkExperienceEntries()
    {
        return WorkExperienceSection.Entries
            .Where(entry => entry.HasUserInput())
            .Select(entry => CvExportDocumentMapper.MapWorkExperience(entry, _localizer))
            .ToArray();
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
            CvExportTemplateId.CenteredMinimal => BuildCenteredMinimalTemplate(document),
            CvExportTemplateId.PhotoLeftBand => BuildPhotoLeftBandTemplate(document),
            CvExportTemplateId.ExecutiveBlueSidebar => BuildExecutiveBlueSidebarTemplate(document),
            CvExportTemplateId.PeachDesigner => BuildPeachDesignerTemplate(document),
            CvExportTemplateId.NavyProfileSplit => BuildNavyProfileSplitTemplate(document),
            CvExportTemplateId.ForestGreenSidebar => BuildForestGreenSidebarTemplate(document),
            CvExportTemplateId.YellowSkillDots => BuildYellowSkillDotsTemplate(document),
            CvExportTemplateId.RoyalBlueSidebar => BuildRoyalBlueSidebarTemplate(document),
            CvExportTemplateId.OrangeTimeline => BuildOrangeTimelineTemplate(document),
            CvExportTemplateId.BlueAccentSummary => BuildBlueAccentSummaryTemplate(document),
            CvExportTemplateId.PillHeaderSplit => BuildPillHeaderSplitTemplate(document),
            CvExportTemplateId.NavyOverlapPhoto => BuildNavyOverlapPhotoTemplate(document),
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

    private static string NormalizeValue(string? value) => CvExportDocumentMapper.NormalizeRequired(value);

    private static string NormalizeOptionalValue(string? value) => CvExportDocumentMapper.NormalizeOptional(value);
}
