using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ReVitae.Core.Export;
using ReVitae.Preview;

namespace ReVitae;

public partial class MainWindow
{
    internal const double TemplateContentPadding = 18;

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

    private static void AddWorkExperienceSection(StackPanel panel, CvExportDocument document)
    {
        if (document.WorkExperienceEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(
            document.Labels.PreviewWorkExperience,
            CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document)));
    }

    private static void AddEducationSection(StackPanel panel, CvExportDocument document)
    {
        if (document.EducationEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(
            document.Labels.PreviewEducation,
            CvExportPreviewContentBuilder.BuildEducationPreviewContent(document)));
    }

    private static void AddSkillsSection(StackPanel panel, CvExportDocument document)
    {
        if (document.SkillsGroups.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(
            document.Labels.PreviewSkills,
            CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document)));
    }

    private static void AddLanguagesSection(StackPanel panel, CvExportDocument document)
    {
        if (document.LanguageEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(
            document.Labels.PreviewLanguages,
            CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document)));
    }

    private static void AddCertificatesSection(StackPanel panel, CvExportDocument document)
    {
        if (document.CertificateEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(
            document.Labels.PreviewCertificates,
            CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document)));
    }

    private static void AddProjectsSection(StackPanel panel, CvExportDocument document)
    {
        if (document.ProjectEntries.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(
            document.Labels.PreviewProjects,
            CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document)));
    }

    private static void AddCustomLinksSection(StackPanel panel, CvExportDocument document)
    {
        if (document.CustomLinkLines.Count == 0)
        {
            return;
        }

        panel.Children.Add(CreateSection(
            document.Labels.PreviewCustomLinks,
            CvExportPreviewContentBuilder.BuildCustomLinksPreviewContent(document)));
    }

    private static void AddAdditionalInformationSection(StackPanel panel, CvExportDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.AdditionalInformationContent))
        {
            return;
        }

        panel.Children.Add(CreateSection(
            document.Labels.PreviewAdditionalInformation,
            CvExportPreviewContentBuilder.BuildAdditionalInformationPreviewContent(document)));
    }

    internal static Control CreateSection(string title, string content)
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

    internal static TextBlock CreateText(string text, double fontSize, IBrush foreground, FontWeight fontWeight)
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
}
