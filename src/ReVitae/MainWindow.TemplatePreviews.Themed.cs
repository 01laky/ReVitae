using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ReVitae.Core.Export;
using ReVitae.Preview;

namespace ReVitae;

public partial class MainWindow
{
	private Control BuildThemedTemplate(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return theme.Layout switch
		{
			CvThemedTemplateLayoutKind.LeftSidebarLight => BuildThemedLeftSidebar(document, theme, sidebarOnLeft: true),
			CvThemedTemplateLayoutKind.RightSidebarLight => BuildThemedLeftSidebar(document, theme, sidebarOnLeft: false),
			CvThemedTemplateLayoutKind.TopHeaderBand => BuildThemedTopHeaderBand(document, theme),
			CvThemedTemplateLayoutKind.TopHeaderSplit => BuildThemedTopHeaderSplit(document, theme),
			CvThemedTemplateLayoutKind.MinimalCenter => BuildThemedMinimalCenter(document, theme),
			CvThemedTemplateLayoutKind.TimelineLeft => BuildThemedTimeline(document, theme, timelineOnLeft: true),
			CvThemedTemplateLayoutKind.TimelineRight => BuildThemedTimeline(document, theme, timelineOnLeft: false),
			CvThemedTemplateLayoutKind.PhotoLeftAccent => BuildThemedPhotoLeftAccent(document, theme),
			CvThemedTemplateLayoutKind.FullSidebarDark => BuildThemedFullSidebarDark(document, theme),
			CvThemedTemplateLayoutKind.AccentBarLeft => BuildThemedAccentBarLeft(document, theme),
			_ => throw new ArgumentOutOfRangeException(nameof(theme), theme.Layout, null)
		};
	}

	private Control BuildThemedLeftSidebar(CvExportDocument document, CvThemedTemplateDefinition theme, bool sidebarOnLeft)
	{
		var root = CreatePreviewRoot();
		root.ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*");

		var sidebarContent = new StackPanel { Spacing = 12 };
		sidebarContent.Children.Add(ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
			document,
			80,
			Brush.Parse(theme.AccentColor),
			Brushes.White));
		sidebarContent.Children.Add(CreateText(document.FullName, 20, Brush.Parse(theme.AccentColor), FontWeight.Bold));
		sidebarContent.Children.Add(CreateContactSection(document));
		AddSkillsSection(sidebarContent, document);
		AddEducationSection(sidebarContent, document);
		AddLanguagesSection(sidebarContent, document);

		var mainContent = CreateContentStack();
		mainContent.Children.Add(new Border
		{
			Background = Brush.Parse(theme.HeaderColor),
			Padding = new Thickness(TemplateContentPadding, 10),
			Child = CreateText(document.ProfessionalTitle, 14, Brushes.White, FontWeight.SemiBold)
		});
		AddThemedMainSections(mainContent, document);

		var sidebar = CreateSidebarPanel(Brush.Parse(theme.SidebarColor), sidebarContent);
		var main = WrapContentPanel(mainContent);

		if (sidebarOnLeft)
		{
			root.Children.Add(sidebar);
			Grid.SetColumn(main, 1);
			root.Children.Add(main);
		}
		else
		{
			Grid.SetColumn(main, 0);
			root.Children.Add(main);
			Grid.SetColumn(sidebar, 1);
			root.Children.Add(sidebar);
		}

		return root;
	}

	private Control BuildThemedTopHeaderBand(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		var root = CreatePreviewRoot();
		root.RowDefinitions = new RowDefinitions("Auto,*");

		var header = new Border
		{
			Background = Brush.Parse(theme.HeaderColor),
			Padding = new Thickness(TemplateContentPadding)
		};
		var headerContent = new StackPanel { Spacing = 6 };
		headerContent.Children.Add(CreateText(document.FullName, 26, Brushes.White, FontWeight.Bold));
		headerContent.Children.Add(CreateText(document.ProfessionalTitle, 13, Brushes.White, FontWeight.SemiBold));
		headerContent.Children.Add(CreateText(CvExportPreviewContentBuilder.BuildContactLines(document), 11, Brushes.White, FontWeight.Normal));
		header.Child = headerContent;
		root.Children.Add(header);

		var body = CreateContentStack();
		AddThemedMainSections(body, document);
		var wrapped = WrapContentPanel(body);
		Grid.SetRow(wrapped, 1);
		root.Children.Add(wrapped);

		return root;
	}

	private Control BuildThemedTopHeaderSplit(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		var root = CreatePreviewRoot();
		root.RowDefinitions = new RowDefinitions("Auto,*");

		var header = new Border
		{
			Background = Brush.Parse(theme.HeaderColor),
			Padding = new Thickness(TemplateContentPadding),
			Child = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,*") }
		};
		var headerGrid = (Grid)header.Child!;
		headerGrid.Children.Add(ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
			document,
			64,
			Brush.Parse(theme.AccentColor),
			Brushes.White));
		var namePanel = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
		namePanel.Children.Add(CreateText(document.FullName, 22, Brushes.White, FontWeight.Bold));
		namePanel.Children.Add(CreateText(document.ProfessionalTitle, 12, Brushes.White, FontWeight.Normal));
		Grid.SetColumn(namePanel, 1);
		headerGrid.Children.Add(namePanel);
		var contactPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
		contactPanel.Children.Add(CreateText(document.Email, 11, Brushes.White, FontWeight.Normal));
		contactPanel.Children.Add(CreateText(document.Phone, 11, Brushes.White, FontWeight.Normal));
		Grid.SetColumn(contactPanel, 2);
		headerGrid.Children.Add(contactPanel);
		root.Children.Add(header);

		var body = CreatePreviewRoot();
		body.ColumnDefinitions = new ColumnDefinitions("0.5*,0.5*");
		var left = CreateContentStack();
		AddWorkExperienceSection(left, document);
		AddEducationSection(left, document);
		var right = CreateContentStack();
		right.Children.Add(CreateSection(document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document)));
		AddSkillsSection(right, document);
		AddLanguagesSection(right, document);
		AddCertificatesSection(right, document);
		AddProjectsSection(right, document);
		root.Children.Add(body);
		Grid.SetRow(body, 1);
		body.Children.Add(WrapContentPanel(left));
		var rightPanel = WrapContentPanel(right);
		Grid.SetColumn(rightPanel, 1);
		body.Children.Add(rightPanel);

		return root;
	}

	private Control BuildThemedMinimalCenter(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		var root = CreatePreviewRoot();
		var content = CreateContentStack();

		var header = new StackPanel { Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center };
		header.Children.Add(CreateText(document.FullName, 28, Brush.Parse(theme.AccentColor), FontWeight.Bold));
		header.Children.Add(CreateText(document.ProfessionalTitle, 14, Brushes.Black, FontWeight.SemiBold));
		header.Children.Add(new Border
		{
			Background = Brush.Parse(theme.SidebarColor),
			Padding = new Thickness(10),
			Child = CreateText(CvExportPreviewContentBuilder.BuildContactLines(document), 11, Brushes.Black, FontWeight.Normal)
		});
		content.Children.Add(header);
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

		return WrapContentPanel(content);
	}

	private Control BuildThemedTimeline(CvExportDocument document, CvThemedTemplateDefinition theme, bool timelineOnLeft)
	{
		var root = CreatePreviewRoot();
		root.RowDefinitions = new RowDefinitions("Auto,*");

		var header = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
		header.Children.Add(ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
			document,
			60,
			Brush.Parse(theme.AccentColor),
			Brushes.White));
		var info = new StackPanel { Spacing = 4, Margin = new Thickness(10, 0, 0, 0) };
		info.Children.Add(CreateText(document.FullName, 22, Brush.Parse(theme.AccentColor), FontWeight.Bold));
		info.Children.Add(CreateText(document.ProfessionalTitle, 13, Brushes.Black, FontWeight.SemiBold));
		Grid.SetColumn(info, 1);
		header.Children.Add(info);
		root.Children.Add(header);

		var body = CreatePreviewRoot();
		body.ColumnDefinitions = timelineOnLeft
			? new ColumnDefinitions("8*,92*")
			: new ColumnDefinitions("92*,8*");
		var rail = new Border { Background = Brush.Parse(theme.AccentColor) };
		var sections = CreateContentStack();
		sections.Children.Add(CreateSection(document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document)));
		AddSkillsSection(sections, document);
		AddWorkExperienceSection(sections, document);
		AddEducationSection(sections, document);

		if (timelineOnLeft)
		{
			body.Children.Add(rail);
			var wrapped = WrapContentPanel(sections);
			Grid.SetColumn(wrapped, 1);
			body.Children.Add(wrapped);
		}
		else
		{
			body.Children.Add(WrapContentPanel(sections));
			Grid.SetColumn(rail, 1);
			body.Children.Add(rail);
		}

		Grid.SetRow(body, 1);
		root.Children.Add(body);
		return root;
	}

	private Control BuildThemedPhotoLeftAccent(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		var root = CreatePreviewRoot();
		var content = CreateContentStack();

		var header = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
		header.Children.Add(ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
			document,
			70,
			Brush.Parse(theme.AccentColor),
			Brushes.White));
		var info = new StackPanel { Spacing = 4, Margin = new Thickness(12, 0, 0, 0) };
		info.Children.Add(CreateNameBlock(document.FirstName, document.LastName, theme.AccentColor, stacked: false));
		info.Children.Add(CreateText(document.ProfessionalTitle, 13, Brushes.Black, FontWeight.SemiBold));
		Grid.SetColumn(info, 1);
		header.Children.Add(info);
		content.Children.Add(header);

		content.Children.Add(new Border
		{
			Background = Brush.Parse(theme.SidebarColor),
			Padding = new Thickness(TemplateContentPadding),
			Child = CreateSection(document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document))
		});

		AddWorkExperienceSection(content, document);
		AddEducationSection(content, document);
		AddSkillsSection(content, document);
		AddLanguagesSection(content, document);

		return WrapContentPanel(content);
	}

	private Control BuildThemedFullSidebarDark(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		var root = CreatePreviewRoot();
		root.ColumnDefinitions = new ColumnDefinitions("0.36*,0.64*");

		var sidebarContent = new StackPanel { Spacing = 12 };
		sidebarContent.Children.Add(ProfilePhotoPreviewFactory.CreateSidebarPhotoOrInitials(
			document,
			76,
			Brush.Parse(theme.AccentColor),
			Brushes.White));
		sidebarContent.Children.Add(CreateText(document.FullName.ToUpperInvariant(), 16, Brushes.White, FontWeight.Bold));
		sidebarContent.Children.Add(CreateText(document.ProfessionalTitle, 12, Brush.Parse("#E8E8E8"), FontWeight.Normal));
		sidebarContent.Children.Add(CreateContactSection(document));
		AddSkillsSection(sidebarContent, document);
		AddEducationSection(sidebarContent, document);

		var main = CreateContentStack();
		AddThemedMainSections(main, document);

		root.Children.Add(CreateSidebarPanel(Brush.Parse(theme.SidebarColor), sidebarContent));
		var mainPanel = WrapContentPanel(main);
		Grid.SetColumn(mainPanel, 1);
		root.Children.Add(mainPanel);

		return root;
	}

	private Control BuildThemedAccentBarLeft(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		var root = CreatePreviewRoot();
		root.ColumnDefinitions = new ColumnDefinitions("8*,92*");
		root.Children.Add(new Border { Background = Brush.Parse(theme.AccentColor) });

		var content = CreateContentStack();
		content.Children.Add(CreateText(document.FullName, 24, Brush.Parse(theme.AccentColor), FontWeight.Bold));
		content.Children.Add(CreateText(document.ProfessionalTitle, 13, Brushes.Black, FontWeight.SemiBold));
		content.Children.Add(CreateText(CvExportPreviewContentBuilder.BuildContactLines(document), 11, Brushes.Black, FontWeight.Normal));
		AddThemedMainSections(content, document);

		var panel = WrapContentPanel(content);
		Grid.SetColumn(panel, 1);
		root.Children.Add(panel);
		return root;
	}

	private static void AddThemedMainSections(StackPanel panel, CvExportDocument document)
	{
		panel.Children.Add(CreateSection(document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document)));
		AddWorkExperienceSection(panel, document);
		AddEducationSection(panel, document);
		AddSkillsSection(panel, document);
		AddLanguagesSection(panel, document);
		AddCertificatesSection(panel, document);
		AddProjectsSection(panel, document);
		AddCustomLinksSection(panel, document);
		AddAdditionalInformationSection(panel, document);
		panel.Children.Add(CreateSection(document.Labels.ContactLinks, CvExportPreviewContentBuilder.BuildContactLinksLines(document)));
	}
}
