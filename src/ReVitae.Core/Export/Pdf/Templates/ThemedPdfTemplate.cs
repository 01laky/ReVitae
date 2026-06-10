namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Export;
using ReVitae.Core.Export.Pdf;

internal static class ThemedPdfTemplate
{
	public static byte[] Render(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return theme.Layout switch
		{
			CvThemedTemplateLayoutKind.LeftSidebarLight => RenderLeftSidebarLight(document, theme),
			CvThemedTemplateLayoutKind.RightSidebarLight => RenderRightSidebarLight(document, theme),
			CvThemedTemplateLayoutKind.TopHeaderBand => RenderTopHeaderBand(document, theme),
			CvThemedTemplateLayoutKind.TopHeaderSplit => RenderTopHeaderSplit(document, theme),
			CvThemedTemplateLayoutKind.MinimalCenter => RenderMinimalCenter(document, theme),
			CvThemedTemplateLayoutKind.TimelineLeft => RenderTimeline(document, theme, timelineOnLeft: true),
			CvThemedTemplateLayoutKind.TimelineRight => RenderTimeline(document, theme, timelineOnLeft: false),
			CvThemedTemplateLayoutKind.PhotoLeftAccent => RenderPhotoLeftAccent(document, theme),
			CvThemedTemplateLayoutKind.FullSidebarDark => RenderFullSidebarDark(document, theme),
			CvThemedTemplateLayoutKind.AccentBarLeft => RenderAccentBarLeft(document, theme),
			_ => throw new ArgumentOutOfRangeException(nameof(theme), theme.Layout, null)
		};
	}

	private static byte[] RenderLeftSidebarLight(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			CvPdfLayoutHelpers.ComposeFullHeightSidebarPage(
				page,
				34,
				66,
				theme.SidebarColor,
				sidebarOnLeft: true,
				sidebar =>
				{
					sidebar.Spacing(12);
					sidebar.Item().Element(c =>
						CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 80, theme.AccentColor, Colors.White));
					sidebar.Item().Text(document.FullName).FontSize(18).Bold().FontColor(theme.AccentColor);
					CvPdfLayoutHelpers.ComposeSection(
						sidebar,
						document.Labels.Contact,
						CvExportPreviewContentBuilder.BuildContactLines(document),
						theme.AccentColor);
					CvPdfExtendedHelpers.ComposeSidebarSections(sidebar, document);
				},
				main =>
				{
					main.Item().Background(theme.HeaderColor).Padding(10).Text(document.ProfessionalTitle)
						.FontSize(13)
						.SemiBold()
						.FontColor(Colors.White);
					main.Item().PaddingTop(10).Column(sections =>
					{
						CvPdfSectionContent.ComposeAllSections(
							sections,
							document,
							document.Labels.Summary,
							document.Labels.Links,
							CvExportPreviewContentBuilder.BuildLinksLines(document));
					});
				});
		});
	}

	private static byte[] RenderRightSidebarLight(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			CvPdfLayoutHelpers.ComposeFullHeightSidebarPage(
				page,
				34,
				66,
				theme.SidebarColor,
				sidebarOnLeft: false,
				sidebar =>
				{
					sidebar.Spacing(12);
					sidebar.Item().Element(c =>
						CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 72, theme.AccentColor, Colors.White));
					CvPdfLayoutHelpers.ComposeSection(
						sidebar,
						document.Labels.Contact,
						CvExportPreviewContentBuilder.BuildContactLines(document),
						theme.AccentColor);
					CvPdfExtendedHelpers.ComposeSidebarSections(sidebar, document);
				},
				main =>
				{
					main.Item().Text(document.FullName).FontSize(24).Bold().FontColor(theme.AccentColor);
					main.Item().PaddingTop(4).Text(document.ProfessionalTitle).SemiBold().FontColor("#333333");
					main.Item().PaddingTop(10).Column(sections =>
					{
						CvPdfSectionContent.ComposeAllSections(
							sections,
							document,
							document.Labels.Summary,
							document.Labels.Links,
							CvExportPreviewContentBuilder.BuildLinksLines(document));
					});
				});
		});
	}

	private static byte[] RenderTopHeaderBand(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		var hasPhoto = ProfilePhotoStorage.FileExists(document.PhotoPath);

		return Generate(document, page =>
		{
			page.Content().Column(content =>
			{
				content.Spacing(14);
				content.Item().Background(theme.HeaderColor).Padding(18).Row(header =>
				{
					if (hasPhoto)
					{
						header.ConstantItem(68).Element(c => CvPdfPhotoHelpers.ComposeHeaderPhoto(c, document, 60));
					}

					header.RelativeItem().Column(info =>
					{
						info.Spacing(4);
						info.Item().Text(document.FullName).FontSize(26).Bold().FontColor(Colors.White);
						info.Item().Text(document.ProfessionalTitle).FontSize(12).FontColor(Colors.White);
						CvPdfExtendedHelpers.ComposeContactLine(info.Item(), document, Colors.White);
					});
				});

				content.Item().Column(sections =>
				{
					CvPdfSectionContent.ComposeAllSections(
						sections,
						document,
						document.Labels.Summary,
						document.Labels.Links,
						CvExportPreviewContentBuilder.BuildLinksLines(document));
				});
			});
		});
	}

	private static byte[] RenderTopHeaderSplit(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Content().Column(content =>
			{
				content.Spacing(12);
				content.Item().Background(theme.HeaderColor).Padding(16).Row(header =>
				{
					header.ConstantItem(72).Element(c =>
						CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 64, theme.AccentColor, Colors.White));
					header.RelativeItem().PaddingLeft(10).Column(info =>
					{
						info.Item().Text(document.FullName).FontSize(24).Bold().FontColor(Colors.White);
						info.Item().Text(document.ProfessionalTitle).FontSize(12).FontColor(Colors.White);
					});
					header.RelativeItem().AlignRight().Column(contact =>
					{
						contact.Item().Text(document.Email).FontSize(10).FontColor(Colors.White);
						contact.Item().Text(document.Phone).FontSize(10).FontColor(Colors.White);
						contact.Item().Text(document.Location).FontSize(10).FontColor(Colors.White);
					});
				});

				content.Item().Row(body =>
				{
					body.RelativeItem().PaddingRight(8).Column(left =>
					{
						CvPdfSectionContent.ComposeWorkExperienceOnly(left, document);
						CvPdfSectionContent.ComposeEducationPublic(left, document);
					});
					body.RelativeItem().PaddingLeft(8).Column(right =>
					{
						CvPdfLayoutHelpers.ComposeSection(
							right,
							document.Labels.Summary,
							CvExportPreviewContentBuilder.BuildSummary(document),
							theme.AccentColor);
						CvPdfSectionContent.ComposeSkillsPublic(right, document);
						CvPdfSectionContent.ComposeLanguagesPublic(right, document);
						CvPdfSectionContent.ComposeCertificatesPublic(right, document);
						CvPdfSectionContent.ComposeProjectsPublic(right, document);
						CvPdfSectionContent.ComposeAdditionalInformationPublic(right, document);
					});
				});
			});
		});
	}

	private static byte[] RenderMinimalCenter(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Content().Column(content =>
			{
				content.Spacing(14);
				content.Item().AlignCenter().Column(header =>
				{
					header.Item().AlignCenter().Text(document.FullName).FontSize(28).Bold().FontColor(theme.AccentColor);
					header.Item().AlignCenter().Text(document.ProfessionalTitle).FontSize(13).SemiBold();
					header.Item().PaddingTop(6).AlignCenter().Background(theme.SidebarColor).Padding(8)
						.Text(CvExportPreviewContentBuilder.BuildContactLines(document))
						.FontSize(10);
				});

				content.Item().Column(sections =>
				{
					CvPdfExtendedHelpers.ComposeCenteredSection(
						sections,
						document.Labels.Summary,
						CvExportPreviewContentBuilder.BuildSummary(document),
						theme.AccentColor);
					sections.Item().PaddingTop(8).Column(body =>
					{
						CvPdfSectionContent.ComposeWorkExperienceOnly(body, document);
						CvPdfSectionContent.ComposeEducationPublic(body, document);
						CvPdfSectionContent.ComposeSkillsPublic(body, document);
						CvPdfSectionContent.ComposeLanguagesPublic(body, document);
						CvPdfSectionContent.ComposeCertificatesPublic(body, document);
						CvPdfSectionContent.ComposeProjectsPublic(body, document);
						CvPdfSectionContent.ComposeAdditionalInformationPublic(body, document);
						CvPdfSectionContent.ComposeCustomLinksPublic(body, document);
						CvPdfSectionContent.ComposeLinksPublic(body, document);
					});
				});
			});
		});
	}

	private static byte[] RenderTimeline(CvExportDocument document, CvThemedTemplateDefinition theme, bool timelineOnLeft)
	{
		return Generate(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Spacing(10);
				root.Item().Row(header =>
				{
					header.ConstantItem(68).Element(c =>
						CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 60, theme.AccentColor, Colors.White));
					header.RelativeItem().PaddingLeft(10).Column(info =>
					{
						info.Item().Text(document.FullName).FontSize(22).Bold().FontColor(theme.AccentColor);
						info.Item().Text(document.ProfessionalTitle).SemiBold();
						CvPdfExtendedHelpers.ComposeContactLine(info.Item(), document);
					});
				});

				root.Item().Row(timeline =>
				{
					if (timelineOnLeft)
					{
						timeline.ConstantItem(14).Column(line => line.Item().Width(3).Background(theme.AccentColor));
						timeline.RelativeItem().PaddingLeft(8).Element(c => ComposeTimelineSections(c, document, theme));
					}
					else
					{
						timeline.RelativeItem().PaddingRight(8).Element(c => ComposeTimelineSections(c, document, theme));
						timeline.ConstantItem(14).Column(line => line.Item().Width(3).Background(theme.AccentColor));
					}
				});
			});
		});
	}

	private static void ComposeTimelineSections(IContainer container, CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		container.Column(content =>
		{
			ComposeTimelineSection(content, document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document), theme.AccentColor);
			ComposeTimelineSection(content, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document), theme.AccentColor);
			ComposeTimelineSection(content, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document), theme.AccentColor);
			ComposeTimelineSection(content, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document), theme.AccentColor);
			ComposeTimelineSection(content, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document), theme.AccentColor);
			ComposeTimelineSection(content, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document), theme.AccentColor);
			ComposeTimelineSection(content, document.Labels.PreviewProjects, CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document), theme.AccentColor);
		});
	}

	private static void ComposeTimelineSection(ColumnDescriptor column, string title, string content, string accent)
	{
		if (string.IsNullOrWhiteSpace(content) || content == "-")
		{
			return;
		}

		column.Item().PaddingBottom(10).Column(section =>
		{
			section.Item().Text(title).SemiBold().FontColor(accent);
			section.Item().Text(content);
		});
	}

	private static byte[] RenderPhotoLeftAccent(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Spacing(12);
				root.Item().Row(header =>
				{
					header.ConstantItem(78).Element(c =>
						CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 70, theme.AccentColor, Colors.White));
					header.RelativeItem().PaddingLeft(12).Column(info =>
					{
						info.Item().Text(text =>
						{
							text.Span(document.FirstName + " ").FontSize(22).Bold();
							text.Span(document.LastName).FontSize(22).Bold().FontColor(theme.AccentColor);
						});
						info.Item().Text(document.ProfessionalTitle).SemiBold();
						CvPdfExtendedHelpers.ComposeContactLine(info.Item(), document);
					});
				});

				root.Item().Background(theme.SidebarColor).Padding(12).Column(summary =>
				{
					CvPdfLayoutHelpers.ComposeSection(
						summary,
						document.Labels.Summary,
						CvExportPreviewContentBuilder.BuildSummary(document),
						theme.AccentColor);
				});

				root.Item().Column(sections =>
				{
					CvPdfSectionContent.ComposeWorkExperienceOnly(sections, document);
					CvPdfSectionContent.ComposeEducationPublic(sections, document);
					CvPdfSectionContent.ComposeSkillsPublic(sections, document);
					CvPdfSectionContent.ComposeLanguagesPublic(sections, document);
					CvPdfSectionContent.ComposeCertificatesPublic(sections, document);
					CvPdfSectionContent.ComposeProjectsPublic(sections, document);
					CvPdfSectionContent.ComposeAdditionalInformationPublic(sections, document);
				});
			});
		});
	}

	private static byte[] RenderFullSidebarDark(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			CvPdfLayoutHelpers.ComposeFullHeightSidebarPage(
				page,
				36,
				64,
				theme.SidebarColor,
				sidebarOnLeft: true,
				sidebar =>
				{
					sidebar.Spacing(12);
					sidebar.Item().Element(c =>
						CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 76, theme.AccentColor, Colors.White));
					sidebar.Item().Text(document.FullName.ToUpperInvariant()).FontSize(16).Bold().FontColor(Colors.White);
					sidebar.Item().Text(document.ProfessionalTitle).FontSize(11).FontColor("#E8E8E8");
					CvPdfLayoutHelpers.ComposeSection(
						sidebar,
						document.Labels.Contact,
						CvExportPreviewContentBuilder.BuildContactLines(document),
						Colors.White);
					CvPdfExtendedHelpers.ComposeSidebarSections(sidebar, document, uppercaseHeadings: true);
				},
				main =>
				{
					CvPdfExtendedHelpers.ComposeMainSections(main, document, document.Labels.Summary);
				});
		});
	}

	private static byte[] RenderAccentBarLeft(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			// Full-height accent bar via page background so it reaches the bottom of every page.
			page.Margin(0);
			page.Background().Row(bg =>
			{
				bg.ConstantItem(8).Background(theme.AccentColor);
				bg.RelativeItem();
			});

			page.Content().Row(row =>
			{
				row.ConstantItem(8);
				row.RelativeItem().PaddingVertical(24).PaddingLeft(20).PaddingRight(24).Column(content =>
				{
					content.Spacing(10);
					content.Item().Text(document.FullName).FontSize(24).Bold().FontColor(theme.AccentColor);
					content.Item().Text(document.ProfessionalTitle).SemiBold();
					CvPdfExtendedHelpers.ComposeContactLine(content.Item(), document);
					content.Item().PaddingTop(6).Column(sections =>
					{
						CvPdfSectionContent.ComposeAllSections(
							sections,
							document,
							document.Labels.Summary,
							document.Labels.Links,
							CvExportPreviewContentBuilder.BuildLinksLines(document));
					});
				});
			});
		});
	}

	private static byte[] Generate(CvExportDocument document, Action<PageDescriptor> composePage) =>
		CvPdfRenderHelper.Generate(document, container =>
		{
			container.Page(page =>
			{
				CvPdfLayoutHelpers.ConfigureA4Page(page);
				composePage(page);
			});
		});
}
