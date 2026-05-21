using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Export;
using ReVitae.Preview;

namespace ReVitae;

public partial class MainWindow
{
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
}
