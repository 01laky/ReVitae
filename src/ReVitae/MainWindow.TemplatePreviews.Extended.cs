using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Export;
using ReVitae.Preview;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReVitae;

public partial class MainWindow
{
    private Control BuildCenteredMinimalTemplate(CvExportDocument document)
    {
        var content = new StackPanel
        {
            Spacing = 14,
            Background = Brushes.White
        };

        var centeredName = CreateText(document.FullName, 28, Brushes.Black, FontWeight.Bold);
        centeredName.TextAlignment = TextAlignment.Center;
        centeredName.HorizontalAlignment = HorizontalAlignment.Stretch;
        content.Children.Add(centeredName);

        content.Children.Add(
            new Border
            {
                Background = Brush.Parse("#E0E0E0"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Child = CreateText(CvExportPreviewContentBuilder.BuildSummary(document), 12, Brushes.Black, FontWeight.Normal)
            });

        var contactBarText = CreateText(BuildContactInline(document), 12, Brushes.White, FontWeight.SemiBold);
        contactBarText.TextAlignment = TextAlignment.Center;
        contactBarText.HorizontalAlignment = HorizontalAlignment.Stretch;

        content.Children.Add(
            new Border
            {
                Background = Brush.Parse("#212121"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10),
                Child = contactBarText
            });

        AddCenteredSection(content, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AddCenteredSection(content, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
        AddCenteredSection(content, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));

        return new Border
        {
            Background = Brushes.White,
            Padding = new Thickness(TemplateContentPadding),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = content
        };
    }

    private Control BuildPhotoLeftBandTemplate(CvExportDocument document)
    {
        var root = new StackPanel
        {
            Spacing = 14,
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*")
        };

        var photo = ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
            document,
            88,
            Brush.Parse("#E67E22"),
            Brushes.White);
        photo.Margin = new Thickness(0, 0, 12, 0);
        header.Children.Add(photo);

        var nameColumn = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };
        nameColumn.Children.Add(CreateText(document.FullName, 24, Brushes.Black, FontWeight.Bold));
        nameColumn.Children.Add(CreateText(document.ProfessionalTitle, 12, Brushes.Black, FontWeight.SemiBold));
        Grid.SetColumn(nameColumn, 1);
        header.Children.Add(nameColumn);
        root.Children.Add(header);

        root.Children.Add(
            new Border
            {
                Background = Brush.Parse("#E8E8E8"),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Child = CreateText(CvExportPreviewContentBuilder.BuildSummary(document), 12, Brushes.Black, FontWeight.Normal)
            });

        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*")
        };

        var sidebar = CreateContentStack();
        sidebar.Spacing = 14;
        sidebar.Children.Add(CreateContactSection(document));
        AddSkillsSection(sidebar, document);
        AddLanguagesSection(sidebar, document);
        AddCertificatesSection(sidebar, document);
        AddCustomLinksSection(sidebar, document);
        body.Children.Add(WrapContentPanel(sidebar));

        var main = CreateContentStack();
        main.Spacing = 14;
        AddWorkExperienceSection(main, document);
        AddProjectsSection(main, document);
        AddEducationSection(main, document);
        AddAdditionalInformationSection(main, document);
        var wrappedMain = WrapContentPanel(main);
        Grid.SetColumn(wrappedMain, 1);
        body.Children.Add(wrappedMain);

        root.Children.Add(body);

        return new Border
        {
            Background = Brushes.White,
            Padding = new Thickness(TemplateContentPadding),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = root
        };
    }

    private Control BuildExecutiveBlueSidebarTemplate(CvExportDocument document)
    {
        var root = CreatePreviewRoot();
        root.ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*");

        var sidebar = new StackPanel { Spacing = 12 };
        sidebar.Children.Add(new Border { Height = 8, Background = Brush.Parse("#1E3A5F") });
        sidebar.Children.Add(CreateText(document.FullName.ToUpperInvariant(), 16, Brush.Parse("#1E3A5F"), FontWeight.Bold));
        sidebar.Children.Add(ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
            document,
            72,
            Brush.Parse("#1E3A5F"),
            Brushes.White));
        sidebar.Children.Add(CreateSection(document.Labels.Contact, CvExportPreviewContentBuilder.BuildContactLines(document)));
        AddLanguagesSection(sidebar, document);
        sidebar.Children.Add(new Border { Height = 8, Background = Brush.Parse("#1E3A5F") });

        root.Children.Add(CreateSidebarPanel(Brush.Parse("#E5E5E5"), sidebar));

        var main = CreateContentStack();
        main.Spacing = 16;
        main.Children.Add(CreateSection(document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document)));
        AddWorkExperienceSection(main, document);
        AddSkillsSection(main, document);
        AddEducationSection(main, document);
        AddProjectsSection(main, document);
        AddAdditionalInformationSection(main, document);
        var wrappedMain = WrapContentPanel(main);
        Grid.SetColumn(wrappedMain, 1);
        root.Children.Add(wrappedMain);

        return root;
    }

    private Control BuildPeachDesignerTemplate(CvExportDocument document)
    {
        var root = new StackPanel
        {
            Spacing = 14,
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*")
        };

        var headerPhoto = ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
            document,
            68,
            Brush.Parse("#E9B083"),
            Brushes.White);
        headerPhoto.Margin = new Thickness(0, 0, 10, 0);
        header.Children.Add(headerPhoto);

        var headerCard = new Border
        {
            Background = Brush.Parse("#E9B083"),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(14),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    CreateText(document.FullName.ToUpperInvariant(), 22, Brushes.Black, FontWeight.Bold),
                    CreateText(BuildContactInline(document), 12, Brushes.Black, FontWeight.SemiBold)
                }
            }
        };
        Grid.SetColumn(headerCard, 1);
        header.Children.Add(headerCard);
        root.Children.Add(header);

        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*")
        };

        var sidebar = CreateContentStack();
        sidebar.Spacing = 14;
        sidebar.Children.Add(CreateSection(document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document)));
        AddSkillsSection(sidebar, document);
        var wrappedSidebar = new Border
        {
            Background = Brush.Parse("#E5E5E5"),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(12),
            Child = sidebar
        };
        body.Children.Add(wrappedSidebar);

        var main = CreateContentStack();
        main.Spacing = 14;
        AddWorkExperienceSection(main, document);
        AddEducationSection(main, document);
        AddProjectsSection(main, document);
        var wrappedMain = WrapContentPanel(main);
        Grid.SetColumn(wrappedMain, 1);
        body.Children.Add(wrappedMain);
        root.Children.Add(body);

        return new Border
        {
            Background = Brushes.White,
            Padding = new Thickness(TemplateContentPadding),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = root
        };
    }

    private Control BuildNavyProfileSplitTemplate(CvExportDocument document)
    {
        var root = new StackPanel
        {
            Spacing = 14,
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var headerName = new TextBlock
        {
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        if (headerName.Inlines is { } headerNameInlines)
        {
            headerNameInlines.Add(new Run($"{document.FirstName} ") { Foreground = Brush.Parse("#E67E22") });
            headerNameInlines.Add(new Run(document.LastName) { Foreground = Brushes.White });
        }

        var headerContact = CreateText(BuildContactInline(document), 12, Brushes.White, FontWeight.SemiBold);
        headerContact.TextAlignment = TextAlignment.Center;
        headerContact.HorizontalAlignment = HorizontalAlignment.Stretch;

        root.Children.Add(
            new Border
            {
                Background = Brush.Parse("#1B2A41"),
                Padding = new Thickness(16),
                Child = new StackPanel
                {
                    Spacing = 6,
                    Children =
                    {
                        headerName,
                        headerContact
                    }
                }
            });

        var summaryRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        var summaryLead = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };
        if (summaryLead.Inlines is { } summaryLeadInlines)
        {
            summaryLeadInlines.Add(new Run($"{document.ProfessionalTitle} — ") { FontWeight = FontWeight.Bold });
            summaryLeadInlines.Add(new Run(CvExportPreviewContentBuilder.BuildSummary(document)));
        }
        summaryRow.Children.Add(summaryLead);

        var summaryPhoto = ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
            document,
            68,
            Brush.Parse("#CCCCCC"),
            Brush.Parse("#1B2A41"));
        Grid.SetColumn(summaryPhoto, 1);
        summaryRow.Children.Add(summaryPhoto);
        root.Children.Add(summaryRow);

        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("0.64*,0.36*")
        };

        var left = new StackPanel { Spacing = 16 };
        AddAccentSection(left, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document), "#E67E22");
        body.Children.Add(left);

        var right = new StackPanel { Spacing = 16 };
        AddAccentSection(right, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document), "#E67E22");
        AddAccentSection(right, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document), "#E67E22");
        Grid.SetColumn(right, 1);
        body.Children.Add(right);
        root.Children.Add(body);

        return new Border
        {
            Background = Brushes.White,
            Padding = new Thickness(TemplateContentPadding),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = root
        };
    }

    private Control BuildForestGreenSidebarTemplate(CvExportDocument document)
    {
        var root = CreatePreviewRoot();
        root.ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*");

        var sidebar = new StackPanel { Spacing = 14 };
        sidebar.Children.Add(CreateRectangularPhotoOrInitials(document, 96, "#2F5D3A"));
        sidebar.Children.Add(
            new Border
            {
                Background = Brush.Parse("#2F5D3A"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Child = CreateText(document.FullName, 18, Brushes.White, FontWeight.Bold)
            });

        var summaryAndContact = new StackPanel { Spacing = 12 };
        summaryAndContact.Children.Add(CreateSection(document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document)));
        summaryAndContact.Children.Add(CreateContactSection(document));
        sidebar.Children.Add(CreateSidebarPanel(Brushes.White, summaryAndContact));
        sidebar.Children.Add(
            new Border
            {
                Background = Brush.Parse("#2F5D3A"),
                Height = 24,
                CornerRadius = new CornerRadius(12)
            });

        root.Children.Add(CreateSidebarPanel(Brushes.White, sidebar));

        var main = CreateContentStack();
        main.Spacing = 16;
        AddSkillsSection(main, document);
        AddWorkExperienceSection(main, document);
        AddEducationSection(main, document);
        AddProjectsSection(main, document);
        AddAdditionalInformationSection(main, document);
        var wrappedMain = WrapContentPanel(main);
        Grid.SetColumn(wrappedMain, 1);
        root.Children.Add(wrappedMain);

        return root;
    }

    private Control BuildYellowSkillDotsTemplate(CvExportDocument document)
    {
        var root = CreatePreviewRoot();
        root.ColumnDefinitions = new ColumnDefinitions("0.64*,0.36*");

        var main = new StackPanel
        {
            Spacing = 14,
            Background = Brushes.White
        };

        var titleRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            VerticalAlignment = VerticalAlignment.Center
        };
        titleRow.Children.Add(
            new Border
            {
                Width = 12,
                Height = 12,
                Background = Brush.Parse("#F5C400"),
                VerticalAlignment = VerticalAlignment.Center
            });
        var titleText = CreateText(document.FullName, 24, Brushes.Black, FontWeight.Bold);
        titleText.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(titleText, 1);
        titleRow.Children.Add(titleText);
        main.Children.Add(titleRow);

        main.Children.Add(CreateSection(document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document)));
        AddWorkExperienceSection(main, document);
        AddEducationSection(main, document);

        var wrappedMain = WrapContentPanel(main);
        root.Children.Add(wrappedMain);

        var sidebar = new StackPanel
        {
            Spacing = 12,
            Background = Brushes.White
        };
        sidebar.Children.Add(CreateRectangularPhotoOrInitials(document, 96, "#F5C400"));
        sidebar.Children.Add(CreateContactSection(document));
        sidebar.Children.Add(CreateText(document.Labels.PreviewSkills, 16, Brushes.Black, FontWeight.Bold));
        foreach (var row in BuildSkillDotRows(document, "#F5C400"))
        {
            sidebar.Children.Add(row);
        }

        var wrappedSidebar = WrapContentPanel(sidebar);
        Grid.SetColumn(wrappedSidebar, 1);
        root.Children.Add(wrappedSidebar);

        return root;
    }

    private Control BuildRoyalBlueSidebarTemplate(CvExportDocument document)
    {
        var root = CreatePreviewRoot();
        root.ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*");

        var sidebar = new StackPanel { Spacing = 14 };
        sidebar.Children.Add(CreateText(document.Labels.Summary, 16, Brushes.White, FontWeight.Bold));
        sidebar.Children.Add(CreateText(CvExportPreviewContentBuilder.BuildSummary(document), 12, Brushes.White, FontWeight.Normal));
        sidebar.Children.Add(CreateText(document.Labels.PreviewSkills, 16, Brushes.White, FontWeight.Bold));
        sidebar.Children.Add(CreateText(CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document), 12, Brushes.White, FontWeight.Normal));
        root.Children.Add(CreateSidebarPanel(Brush.Parse("#4A76C0"), sidebar));

        var main = new StackPanel
        {
            Spacing = 0,
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Background = Brush.Parse("#333A45")
        };
        header.Children.Add(
            new Border
            {
                Padding = new Thickness(14),
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        CreateText(document.FullName, 22, Brushes.White, FontWeight.Bold),
                        CreateText(document.Location, 12, Brushes.White, FontWeight.Normal),
                        CreateText(BuildContactInline(document), 12, Brushes.White, FontWeight.SemiBold)
                    }
                }
            });
        var photo = ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
            document,
            68,
            Brush.Parse("#666666"),
            Brushes.White);
        photo.Margin = new Thickness(0, 14, 14, 14);
        Grid.SetColumn(photo, 1);
        header.Children.Add(photo);
        main.Children.Add(header);

        var body = CreateContentStack();
        body.Spacing = 14;
        AddWorkExperienceSection(body, document);
        AddEducationSection(body, document);
        main.Children.Add(WrapContentPanel(body));

        Grid.SetColumn(main, 1);
        root.Children.Add(main);

        return root;
    }

    private Control BuildOrangeTimelineTemplate(CvExportDocument document)
    {
        var root = new StackPanel
        {
            Spacing = 12,
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*")
        };

        var photo = ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
            document,
            68,
            Brush.Parse("#E67E22"),
            Brushes.White);
        photo.Margin = new Thickness(0, 0, 10, 0);
        header.Children.Add(photo);

        var splitName = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };
        if (splitName.Inlines is { } splitNameInlines)
        {
            splitNameInlines.Add(new Run($"{document.FirstName.ToUpperInvariant()} ") { FontSize = 22, FontWeight = FontWeight.Bold });
            splitNameInlines.Add(new Run(document.LastName.ToUpperInvariant()) { FontSize = 22, FontWeight = FontWeight.Bold, Foreground = Brush.Parse("#E67E22") });
        }

        var headerInfo = new StackPanel { Spacing = 4 };
        headerInfo.Children.Add(splitName);
        headerInfo.Children.Add(CreateText(BuildContactInline(document), 12, Brushes.Black, FontWeight.Normal));
        Grid.SetColumn(headerInfo, 1);
        header.Children.Add(headerInfo);
        root.Children.Add(header);

        var timeline = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("16,*")
        };
        timeline.Children.Add(
            new Border
            {
                Width = 2,
                Background = Brush.Parse("#E67E22"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch
            });

        var timelineContent = new StackPanel { Spacing = 12 };
        AddTimelineSection(timelineContent, document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document), "#E67E22");
        AddTimelineSection(timelineContent, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document), "#E67E22");
        AddTimelineSection(timelineContent, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document), "#E67E22");
        AddTimelineSection(timelineContent, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document), "#E67E22");
        Grid.SetColumn(timelineContent, 1);
        timelineContent.Margin = new Thickness(8, 0, 0, 0);
        timeline.Children.Add(timelineContent);
        root.Children.Add(timeline);

        return new Border
        {
            Background = Brushes.White,
            Padding = new Thickness(TemplateContentPadding),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = root
        };
    }

    private Control BuildBlueAccentSummaryTemplate(CvExportDocument document)
    {
        var root = new StackPanel
        {
            Spacing = 12,
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
        };
        header.Children.Add(
            new Border
            {
                Width = 12,
                Height = 12,
                Background = Brush.Parse("#2C4A93"),
                VerticalAlignment = VerticalAlignment.Top
            });

        var title = new StackPanel { Spacing = 4 };
        title.Children.Add(CreateText(document.FullName, 24, Brushes.Black, FontWeight.Bold));
        title.Children.Add(CreateText($"{document.Phone} // {document.Email}", 12, Brushes.Black, FontWeight.Normal));
        title.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(title, 1);
        header.Children.Add(title);

        var photo = ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
            document,
            68,
            Brush.Parse("#2C4A93"),
            Brushes.White);
        Grid.SetColumn(photo, 2);
        header.Children.Add(photo);
        root.Children.Add(header);

        root.Children.Add(
            new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brush.Parse("#2C4A93"),
                Padding = new Thickness(12),
                Child = CreateText(CvExportPreviewContentBuilder.BuildSummary(document), 12, Brushes.Black, FontWeight.Normal)
            });

        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*")
        };

        var left = CreateContentStack();
        left.Spacing = 14;
        AddSkillsSection(left, document);
        AddEducationSection(left, document);
        body.Children.Add(WrapContentPanel(left));

        var right = CreateContentStack();
        right.Spacing = 14;
        AddWorkExperienceSection(right, document);
        AddProjectsSection(right, document);
        var wrappedRight = WrapContentPanel(right);
        Grid.SetColumn(wrappedRight, 1);
        body.Children.Add(wrappedRight);
        root.Children.Add(body);

        return new Border
        {
            Background = Brushes.White,
            Padding = new Thickness(TemplateContentPadding),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = root
        };
    }

    private Control BuildPillHeaderSplitTemplate(CvExportDocument document)
    {
        var root = new StackPanel
        {
            Spacing = 12,
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*")
        };

        var photo = ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
            document,
            68,
            Brush.Parse("#E9967A"),
            Brushes.White);
        photo.Margin = new Thickness(0, 0, 10, 0);
        header.Children.Add(photo);

        var pill = new Border
        {
            Background = Brush.Parse("#E8E8E8"),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(14),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    CreateText(document.FullName, 22, Brushes.Black, FontWeight.Bold),
                    CreateText($"{document.Phone}  {document.Email}  {document.LinkedInUrl}", 12, Brush.Parse("#E9967A"), FontWeight.SemiBold)
                }
            }
        };
        Grid.SetColumn(pill, 1);
        header.Children.Add(pill);
        root.Children.Add(header);

        root.Children.Add(CreateText(CvExportPreviewContentBuilder.BuildSummary(document), 12, Brushes.Black, FontWeight.Normal));

        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*")
        };

        var left = CreateContentStack();
        left.Spacing = 14;
        AddSkillsSection(left, document);
        body.Children.Add(
            new Border
            {
                Background = Brush.Parse("#E8E8E8"),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(12),
                Child = left
            });

        var right = CreateContentStack();
        right.Spacing = 14;
        AddWorkExperienceSection(right, document);
        AddEducationSection(right, document);
        var wrappedRight = WrapContentPanel(right);
        Grid.SetColumn(wrappedRight, 1);
        body.Children.Add(wrappedRight);
        root.Children.Add(body);

        return new Border
        {
            Background = Brushes.White,
            Padding = new Thickness(TemplateContentPadding),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = root
        };
    }

    private Control BuildNavyOverlapPhotoTemplate(CvExportDocument document)
    {
        var root = new StackPanel
        {
            Spacing = 10,
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var headerHost = new Grid
        {
            Height = 96,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        headerHost.Children.Add(
            new Border
            {
                Height = 72,
                Background = Brush.Parse("#1E3A5F"),
                VerticalAlignment = VerticalAlignment.Top
            });

        var photo = ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
            document,
            72,
            Brush.Parse("#CCCCCC"),
            Brush.Parse("#1E3A5F"));
        photo.HorizontalAlignment = HorizontalAlignment.Right;
        photo.VerticalAlignment = VerticalAlignment.Top;
        photo.Margin = new Thickness(0, 24, 0, 0);
        headerHost.Children.Add(photo);
        root.Children.Add(headerHost);

        var info = new StackPanel { Spacing = 4 };
        info.Children.Add(CreateText(document.FullName, 24, Brushes.Black, FontWeight.Bold));
        info.Children.Add(CreateText($"{document.Phone}    {document.Email}", 12, Brushes.Black, FontWeight.Normal));
        root.Children.Add(info);

        var summaryLead = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };
        if (summaryLead.Inlines is { } overlapSummaryInlines)
        {
            overlapSummaryInlines.Add(new Run($"{document.ProfessionalTitle} ") { FontWeight = FontWeight.Bold });
            overlapSummaryInlines.Add(new Run(CvExportPreviewContentBuilder.BuildSummary(document)));
        }
        root.Children.Add(summaryLead);

        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("0.64*,0.36*")
        };

        var left = new StackPanel { Spacing = 14 };
        AddAccentSection(left, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document), "#1E3A5F");
        body.Children.Add(left);

        var right = new StackPanel { Spacing = 14 };
        AddAccentSection(right, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document), "#1E3A5F");
        AddAccentSection(right, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document), "#1E3A5F");
        Grid.SetColumn(right, 1);
        body.Children.Add(right);
        root.Children.Add(body);

        return new Border
        {
            Background = Brushes.White,
            Padding = new Thickness(TemplateContentPadding),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = root
        };
    }

    private static string BuildContactInline(CvExportDocument document)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(document.Phone) && document.Phone != "-")
        {
            parts.Add(document.Phone);
        }

        if (!string.IsNullOrWhiteSpace(document.Email) && document.Email != "-")
        {
            parts.Add(document.Email);
        }

        if (!string.IsNullOrWhiteSpace(document.LinkedInUrl) && document.LinkedInUrl != "-")
        {
            parts.Add(document.LinkedInUrl);
        }

        if (!string.IsNullOrWhiteSpace(document.Location) && document.Location != "-")
        {
            parts.Add(document.Location);
        }

        return parts.Count == 0 ? "-" : string.Join("  ", parts);
    }

    private static void AddCenteredSection(StackPanel panel, string title, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content == "-")
        {
            return;
        }

        var section = new StackPanel { Spacing = 6 };
        var heading = CreateText(title, 18, Brushes.Black, FontWeight.Bold);
        heading.TextAlignment = TextAlignment.Center;
        heading.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Children.Add(heading);
        section.Children.Add(
            new Border
            {
                Height = 1,
                Background = Brush.Parse("#B8B8B8")
            });

        var body = CreateText(content, 12, Brushes.Black, FontWeight.Normal);
        body.TextAlignment = TextAlignment.Center;
        body.HorizontalAlignment = HorizontalAlignment.Stretch;
        section.Children.Add(body);

        panel.Children.Add(section);
    }

    private static void AddAccentSection(StackPanel panel, string title, string content, string accentColor)
    {
        if (string.IsNullOrWhiteSpace(content) || content == "-")
        {
            return;
        }

        panel.Children.Add(
            new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    CreateText(title, 18, Brush.Parse(accentColor), FontWeight.Bold),
                    new Border
                    {
                        Height = 1,
                        Background = Brush.Parse(accentColor)
                    },
                    CreateText(content, 12, Brushes.Black, FontWeight.Normal)
                }
            });
    }

    private static void AddTimelineSection(StackPanel panel, string title, string content, string accentColor)
    {
        if (string.IsNullOrWhiteSpace(content) || content == "-")
        {
            return;
        }

        panel.Children.Add(
            new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    CreateText(title, 16, Brush.Parse(accentColor), FontWeight.SemiBold),
                    CreateText(content, 12, Brushes.Black, FontWeight.Normal)
                }
            });
    }

    private static Control CreateRectangularPhotoOrInitials(CvExportDocument document, double size, string accentColor)
    {
        if (ProfilePhotoStorage.FileExists(document.PhotoPath))
        {
            return new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new Image
                {
                    Source = new Bitmap(document.PhotoPath!),
                    Width = size,
                    Height = size,
                    Stretch = Stretch.UniformToFill
                }
            };
        }

        return new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(6),
            Background = Brush.Parse("#F3F3F3"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
                document,
                size - 20,
                Brush.Parse(accentColor),
                Brushes.White)
        };
    }

    private static IReadOnlyList<Control> BuildSkillDotRows(CvExportDocument document, string accentColor)
    {
        var rows = new List<Control>();
        var skills = document.SkillsGroups
            .SelectMany(group => group.Skills)
            .Select(skill => skill.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Take(8)
            .ToArray();

        if (skills.Length == 0)
        {
            skills = new[] { document.Labels.PreviewSkills };
        }

        for (var i = 0; i < skills.Length; i++)
        {
            rows.Add(CreateSkillDotRow(skills[i], 4 + (i % 6), accentColor));
        }

        return rows;
    }

    private static Control CreateSkillDotRow(string label, int filledDots, string accentColor)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            VerticalAlignment = VerticalAlignment.Center
        };
        row.Children.Add(CreateText(label, 11, Brushes.Black, FontWeight.Normal));

        var dots = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            VerticalAlignment = VerticalAlignment.Center
        };
        for (var i = 0; i < 10; i++)
        {
            dots.Children.Add(
                new Border
                {
                    Width = 7,
                    Height = 7,
                    CornerRadius = new CornerRadius(3.5),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brush.Parse(accentColor),
                    Background = i < filledDots ? Brush.Parse(accentColor) : Brushes.Transparent
                });
        }

        Grid.SetColumn(dots, 1);
        row.Children.Add(dots);
        return row;
    }
}
