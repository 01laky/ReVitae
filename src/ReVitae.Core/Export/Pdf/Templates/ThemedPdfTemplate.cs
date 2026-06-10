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
			CvThemedTemplateLayoutKind.MonogramHeaderTwoColumn => RenderMonogramHeaderTwoColumn(document, theme),
			CvThemedTemplateLayoutKind.BannerContactStrip => RenderBannerContactStrip(document, theme),
			CvThemedTemplateLayoutKind.AsymmetricCornerBars => RenderAsymmetricCornerBars(document, theme),
			CvThemedTemplateLayoutKind.SkillChipSidebar => RenderSkillChipSidebar(document, theme),
			CvThemedTemplateLayoutKind.CardSectionsBody => RenderCardSectionsBody(document, theme),
			CvThemedTemplateLayoutKind.DualToneFullSplit => RenderDualToneFullSplit(document, theme),
			CvThemedTemplateLayoutKind.ModernistHeaderRule => RenderModernistHeaderRule(document, theme),
			CvThemedTemplateLayoutKind.CenteredMonogram => RenderCenteredMonogram(document, theme),
			CvThemedTemplateLayoutKind.RibbonHeaderCentered => RenderRibbonHeaderCentered(document, theme),
			CvThemedTemplateLayoutKind.HeaderTwoEqualColumns => RenderHeaderTwoEqualColumns(document, theme),
			CvThemedTemplateLayoutKind.AccentFooterBar => RenderAccentFooterBar(document, theme),
			CvThemedTemplateLayoutKind.BoxedHeaderSidebar => RenderBoxedHeaderSidebar(document, theme),
			CvThemedTemplateLayoutKind.DuoBandHeader => RenderDuoBandHeader(document, theme),
			CvThemedTemplateLayoutKind.InitialsSidebarDark => RenderInitialsSidebarDark(document, theme),
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
					sidebar.Item().Text(document.ProfessionalTitle).FontSize(11).FontColor(CvPdfPalette.MutedOnDark);
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

	// ---- Prompt 048 — new structural archetypes -------------------------------------------------

	private static byte[] RenderMonogramHeaderTwoColumn(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Spacing(12);
				root.Item().Row(header =>
				{
					header.ConstantItem(60).Element(c =>
						CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 54, theme.AccentColor, Colors.White, circular: false));
					header.RelativeItem().PaddingLeft(12).AlignMiddle().Column(info =>
					{
						info.Item().Text(document.FullName).FontSize(24).Bold().FontColor(theme.AccentColor);
						info.Item().Text(document.ProfessionalTitle).FontSize(12).SemiBold();
					});
				});
				root.Item().LineHorizontal(2).LineColor(theme.AccentColor);
				root.Item().Row(body =>
				{
					body.RelativeItem(34).Background(theme.SidebarColor).Padding(10).Column(side =>
					{
						side.Spacing(12);
						CvPdfLayoutHelpers.ComposeSection(side, document.Labels.Contact,
							CvExportPreviewContentBuilder.BuildContactLines(document), theme.AccentColor);
						CvPdfExtendedHelpers.ComposeSidebarSections(side, document);
					});
					body.RelativeItem(66).PaddingLeft(14).Column(main =>
						ComposePrimaryMain(main, document, document.Labels.Summary, includeEducation: false));
				});
			});
		});
	}

	private static byte[] RenderBannerContactStrip(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Item().Background(theme.HeaderColor).Padding(18).Column(h =>
				{
					h.Item().Text(document.FullName).FontSize(26).Bold().FontColor(Colors.White);
					h.Item().Text(document.ProfessionalTitle).FontSize(13).FontColor(Colors.White);
				});
				root.Item().Background("#F2F2F2").Padding(10)
					.Text(CvExportPreviewContentBuilder.BuildContactLines(document))
					.FontSize(10).FontColor(CvPdfPalette.Black);
				root.Item().PaddingTop(12).Column(sections =>
					CvPdfSectionContent.ComposeAllSections(sections, document, document.Labels.Summary,
						document.Labels.Links, CvExportPreviewContentBuilder.BuildLinksLines(document)));
			});
		});
	}

	private static byte[] RenderAsymmetricCornerBars(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Background().Column(bg =>
			{
				bg.Item().AlignRight().Width(150).Height(10).Background(theme.AccentColor);
				bg.Item().Extend();
				bg.Item().AlignLeft().Width(150).Height(10).Background(theme.AccentColor);
			});
			page.Content().Column(root =>
			{
				root.Spacing(12);
				root.Item().PaddingTop(6).Text(document.FullName).FontSize(26).Bold().FontColor(theme.AccentColor);
				root.Item().Text(document.ProfessionalTitle).FontSize(12).SemiBold();
				CvPdfExtendedHelpers.ComposeContactLine(root.Item(), document);
				root.Item().PaddingTop(4).Column(sections =>
					CvPdfSectionContent.ComposeAllSections(sections, document, document.Labels.Summary,
						document.Labels.Links, CvExportPreviewContentBuilder.BuildLinksLines(document)));
			});
		});
	}

	private static byte[] RenderSkillChipSidebar(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Spacing(12);
				root.Item().Row(header =>
				{
					header.ConstantItem(56).Element(c =>
						CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 50, theme.AccentColor, Colors.White));
					header.RelativeItem().PaddingLeft(12).AlignMiddle().Column(info =>
					{
						info.Item().Text(document.FullName).FontSize(23).Bold().FontColor(theme.AccentColor);
						info.Item().Text(document.ProfessionalTitle).SemiBold();
					});
				});
				root.Item().Row(body =>
				{
					body.RelativeItem(36).Background(theme.SidebarColor).Padding(10).Column(side =>
					{
						side.Spacing(10);
						ComposePillSection(side, document.Labels.Contact, CvExportPreviewContentBuilder.BuildContactLines(document), theme.AccentColor);
						ComposePillSection(side, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document), theme.AccentColor);
						ComposePillSection(side, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document), theme.AccentColor);
						ComposePillSection(side, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document), theme.AccentColor);
					});
					body.RelativeItem(64).PaddingLeft(14).Column(main =>
					{
						main.Spacing(12);
						CvPdfLayoutHelpers.ComposeSection(main, document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document), theme.AccentColor);
						CvPdfSectionContent.ComposeWorkExperienceOnly(main, document);
						CvPdfSectionContent.ComposeEducationPublic(main, document);
						CvPdfSectionContent.ComposeProjectsPublic(main, document);
						CvPdfSectionContent.ComposeAdditionalInformationPublic(main, document);
					});
				});
			});
		});
	}

	private static byte[] RenderCardSectionsBody(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Spacing(10);
				root.Item().Text(document.FullName).FontSize(24).Bold().FontColor(theme.AccentColor);
				root.Item().Text(document.ProfessionalTitle).SemiBold();
				CvPdfExtendedHelpers.ComposeContactLine(root.Item(), document);
				root.Item().PaddingTop(6).Column(cards =>
				{
					cards.Spacing(10);
					ComposeCardSection(cards, document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document), theme.AccentColor);
					ComposeCardSection(cards, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document), theme.AccentColor);
					ComposeCardSection(cards, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document), theme.AccentColor);
					ComposeCardSection(cards, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document), theme.AccentColor);
					ComposeCardSection(cards, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document), theme.AccentColor);
					ComposeCardSection(cards, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document), theme.AccentColor);
					ComposeCardSection(cards, document.Labels.PreviewProjects, CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document), theme.AccentColor);
					ComposeCardSection(cards, document.Labels.PreviewAdditionalInformation, CvExportPreviewContentBuilder.BuildAdditionalInformationPreviewContent(document), theme.AccentColor);
				});
			});
		});
	}

	private static byte[] RenderDualToneFullSplit(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Margin(0);
			page.Background().Row(bg =>
			{
				bg.RelativeItem(38).Background(theme.SidebarColor);
				bg.RelativeItem(62).Background(Colors.White);
			});
			page.Content().Column(root =>
			{
				root.Item().Background(theme.HeaderColor).Padding(14).AlignCenter().Column(h =>
				{
					h.Item().AlignCenter().Text(document.FullName).FontSize(24).Bold().FontColor(Colors.White);
					h.Item().AlignCenter().Text(document.ProfessionalTitle).FontSize(12).FontColor(Colors.White);
				});
				root.Item().Row(body =>
				{
					body.RelativeItem(38).PaddingVertical(16).PaddingHorizontal(14).Column(side =>
					{
						side.Spacing(12);
						CvPdfLayoutHelpers.ComposeSection(side, document.Labels.Contact,
							CvExportPreviewContentBuilder.BuildContactLines(document), theme.AccentColor);
						CvPdfExtendedHelpers.ComposeSidebarSections(side, document);
					});
					body.RelativeItem(62).PaddingVertical(16).PaddingHorizontal(16).Column(main =>
						ComposePrimaryMain(main, document, document.Labels.Summary, includeEducation: false));
				});
			});
		});
	}

	private static byte[] RenderModernistHeaderRule(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Spacing(14);
				root.Item().Column(head =>
				{
					head.Item().Text(document.FullName.ToUpperInvariant()).FontSize(28).Bold().FontColor(CvPdfPalette.Black);
					head.Item().PaddingTop(4).LineHorizontal(3).LineColor(theme.AccentColor);
					head.Item().PaddingTop(4).Text(document.ProfessionalTitle).FontSize(12).SemiBold().FontColor(theme.AccentColor);
					CvPdfExtendedHelpers.ComposeContactLine(head.Item(), document);
				});
				root.Item().Column(sections =>
					CvPdfSectionContent.ComposeAllSections(sections, document, document.Labels.Summary,
						document.Labels.Links, CvExportPreviewContentBuilder.BuildLinksLines(document)));
			});
		});
	}

	private static byte[] RenderCenteredMonogram(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Spacing(12);
				root.Item().AlignCenter().Element(c =>
					CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 72, theme.AccentColor, Colors.White));
				root.Item().AlignCenter().Text(document.FullName).FontSize(26).Bold().FontColor(theme.AccentColor);
				root.Item().AlignCenter().Text(document.ProfessionalTitle).FontSize(13).SemiBold();
				root.Item().AlignCenter().Background(theme.SidebarColor).Padding(8)
					.Text(CvExportPreviewContentBuilder.BuildContactLines(document)).FontSize(10);
				root.Item().PaddingTop(6).Column(sections =>
				{
					sections.Spacing(8);
					ComposeCenteredEntry(sections, document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document), theme.AccentColor);
					ComposeCenteredEntry(sections, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document), theme.AccentColor);
					ComposeCenteredEntry(sections, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document), theme.AccentColor);
					ComposeCenteredEntry(sections, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document), theme.AccentColor);
					ComposeCenteredEntry(sections, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document), theme.AccentColor);
					ComposeCenteredEntry(sections, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document), theme.AccentColor);
					ComposeCenteredEntry(sections, document.Labels.PreviewProjects, CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document), theme.AccentColor);
					ComposeCenteredEntry(sections, document.Labels.PreviewAdditionalInformation, CvExportPreviewContentBuilder.BuildAdditionalInformationPreviewContent(document), theme.AccentColor);
				});
			});
		});
	}

	private static byte[] RenderRibbonHeaderCentered(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Spacing(12);
				root.Item().AlignCenter().Background(theme.AccentColor).CornerRadius(14).PaddingVertical(8).PaddingHorizontal(22)
					.Text(document.FullName).FontSize(22).Bold().FontColor(Colors.White);
				root.Item().AlignCenter().Text(document.ProfessionalTitle).FontSize(13).SemiBold();
				root.Item().AlignCenter().Text(CvExportPreviewContentBuilder.BuildContactLines(document)).FontSize(10);
				root.Item().PaddingTop(6).Column(sections =>
					CvPdfSectionContent.ComposeAllSections(sections, document, document.Labels.Summary,
						document.Labels.Links, CvExportPreviewContentBuilder.BuildLinksLines(document)));
			});
		});
	}

	private static byte[] RenderHeaderTwoEqualColumns(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Spacing(12);
				root.Item().Background(theme.HeaderColor).Padding(16).Column(h =>
				{
					h.Item().Text(document.FullName).FontSize(24).Bold().FontColor(Colors.White);
					h.Item().Text(document.ProfessionalTitle).FontSize(12).FontColor(Colors.White);
					CvPdfExtendedHelpers.ComposeContactLine(h.Item(), document, Colors.White);
				});
				root.Item().Row(body =>
				{
					body.RelativeItem().PaddingRight(10).Column(left =>
					{
						left.Spacing(12);
						CvPdfLayoutHelpers.ComposeSection(left, document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document), theme.AccentColor);
						CvPdfSectionContent.ComposeWorkExperienceOnly(left, document);
						CvPdfSectionContent.ComposeEducationPublic(left, document);
					});
					body.RelativeItem().PaddingLeft(10).Column(right =>
					{
						right.Spacing(12);
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

	private static byte[] RenderAccentFooterBar(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Footer().Height(8).Background(theme.AccentColor);
			page.Content().Column(root =>
			{
				root.Spacing(12);
				root.Item().LineHorizontal(3).LineColor(theme.AccentColor);
				root.Item().Text(document.FullName).FontSize(25).Bold().FontColor(theme.AccentColor);
				root.Item().Text(document.ProfessionalTitle).SemiBold();
				CvPdfExtendedHelpers.ComposeContactLine(root.Item(), document);
				root.Item().Column(sections =>
					CvPdfSectionContent.ComposeAllSections(sections, document, document.Labels.Summary,
						document.Labels.Links, CvExportPreviewContentBuilder.BuildLinksLines(document)));
			});
		});
	}

	private static byte[] RenderBoxedHeaderSidebar(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Spacing(12);
				root.Item().Border(1.5f).BorderColor(theme.AccentColor).CornerRadius(8).Padding(12).Column(h =>
				{
					h.Item().Text(document.FullName).FontSize(23).Bold().FontColor(theme.AccentColor);
					h.Item().Text(document.ProfessionalTitle).SemiBold();
					CvPdfExtendedHelpers.ComposeContactLine(h.Item(), document);
				});
				root.Item().Row(body =>
				{
					body.RelativeItem(64).PaddingRight(12).Column(main =>
						ComposePrimaryMain(main, document, document.Labels.Summary, includeEducation: false));
					body.RelativeItem(36).Background(theme.SidebarColor).Padding(10).Column(side =>
					{
						side.Spacing(12);
						CvPdfExtendedHelpers.ComposeSidebarSections(side, document);
						CvPdfSectionContent.ComposeAdditionalInformationPublic(side, document);
					});
				});
			});
		});
	}

	private static byte[] RenderDuoBandHeader(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			page.Content().Column(root =>
			{
				root.Item().Background(theme.HeaderColor).PaddingHorizontal(16).PaddingVertical(14).Column(h =>
				{
					h.Item().Text(document.FullName).FontSize(25).Bold().FontColor(Colors.White);
					h.Item().Text(document.ProfessionalTitle).FontSize(12).FontColor(Colors.White);
				});
				root.Item().Background(theme.AccentColor).PaddingHorizontal(16).PaddingVertical(7)
					.Text(CvExportPreviewContentBuilder.BuildContactLines(document)).FontSize(10).FontColor(Colors.White);
				root.Item().PaddingTop(12).Column(sections =>
					CvPdfSectionContent.ComposeAllSections(sections, document, document.Labels.Summary,
						document.Labels.Links, CvExportPreviewContentBuilder.BuildLinksLines(document)));
			});
		});
	}

	private static byte[] RenderInitialsSidebarDark(CvExportDocument document, CvThemedTemplateDefinition theme)
	{
		return Generate(document, page =>
		{
			CvPdfLayoutHelpers.ComposeFullHeightSidebarPage(
				page,
				30,
				70,
				theme.SidebarColor,
				sidebarOnLeft: true,
				sidebar =>
				{
					sidebar.Spacing(12);
					sidebar.Item().Element(c =>
						CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 72, theme.AccentColor, theme.SidebarColor, circular: false));
					sidebar.Item().Text(document.FullName.ToUpperInvariant()).FontSize(15).Bold().FontColor(Colors.White);
					sidebar.Item().Text(document.ProfessionalTitle).FontSize(10).FontColor(CvPdfPalette.MutedOnDark);
					ComposeDarkSection(sidebar, document.Labels.Contact, CvExportPreviewContentBuilder.BuildContactLines(document), theme.AccentColor);
					ComposeDarkSection(sidebar, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document), theme.AccentColor);
					ComposeDarkSection(sidebar, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document), theme.AccentColor);
					ComposeDarkSection(sidebar, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document), theme.AccentColor);
				},
				main => ComposePrimaryMain(main, document, document.Labels.Summary, includeEducation: true));
		});
	}

	// ---- 048 small composition helpers ----------------------------------------------------------

	private static bool HasContent(string content) =>
		!string.IsNullOrWhiteSpace(content) && content != "-";

	/// <summary>
	/// Primary-column sections for a sidebar layout — summary, work, optional education, projects,
	/// additional info. The supporting sections (skills / languages / certificates) live in the
	/// sidebar, so they are deliberately omitted here to avoid the duplication the older themed
	/// layouts show when they pair <c>ComposeSidebarSections</c> with <c>ComposeMainSections</c>.
	/// </summary>
	private static void ComposePrimaryMain(ColumnDescriptor column, CvExportDocument document, string summaryTitle, bool includeEducation)
	{
		column.Spacing(14);
		CvPdfLayoutHelpers.ComposeSection(column, summaryTitle, CvExportPreviewContentBuilder.BuildSummary(document));
		CvPdfSectionContent.ComposeWorkExperienceOnly(column, document);
		if (includeEducation)
		{
			CvPdfSectionContent.ComposeEducationPublic(column, document);
		}

		CvPdfSectionContent.ComposeProjectsPublic(column, document);
		CvPdfSectionContent.ComposeAdditionalInformationPublic(column, document);
	}

	private static void ComposeCardSection(ColumnDescriptor column, string title, string content, string accent)
	{
		if (!HasContent(content))
		{
			return;
		}

		column.Item().Border(1).BorderColor(accent).CornerRadius(6).Background(Colors.White).Padding(10).Column(card =>
		{
			card.Spacing(4);
			card.Item().Text(title).FontSize(13).SemiBold().FontColor(accent);
			card.Item().Text(content).FontSize(CvPdfLayoutHelpers.BaseFontSize).FontColor(Colors.Black);
		});
	}

	private static void ComposePillSection(ColumnDescriptor column, string title, string content, string accent)
	{
		if (!HasContent(content))
		{
			return;
		}

		column.Item().Column(section =>
		{
			section.Spacing(4);
			section.Item().Background(accent).CornerRadius(8).PaddingVertical(3).PaddingHorizontal(8)
				.Text(title).FontSize(10).SemiBold().FontColor(Colors.White);
			section.Item().Text(content).FontSize(CvPdfLayoutHelpers.BaseFontSize).FontColor(Colors.Black);
		});
	}

	private static void ComposeCenteredEntry(ColumnDescriptor column, string title, string content, string accent)
	{
		if (!HasContent(content))
		{
			return;
		}

		column.Item().PaddingTop(4).AlignCenter().Column(section =>
		{
			section.Item().AlignCenter().Text(title).FontSize(13).SemiBold().FontColor(accent);
			section.Item().AlignCenter().Text(content).FontSize(CvPdfLayoutHelpers.BaseFontSize);
		});
	}

	private static void ComposeDarkSection(ColumnDescriptor column, string title, string content, string accent)
	{
		if (!HasContent(content))
		{
			return;
		}

		column.Item().Column(section =>
		{
			section.Spacing(4);
			section.Item().Text(title.ToUpperInvariant()).FontSize(11).SemiBold().FontColor(accent);
			section.Item().LineHorizontal(0.75f).LineColor(accent);
			section.Item().Text(content).FontSize(CvPdfLayoutHelpers.BaseFontSize).FontColor(Colors.White);
		});
	}

	private static byte[] Generate(CvExportDocument document, Action<PageDescriptor> composePage) =>
		CvPdfRenderHelper.RenderPage(document, composePage);
}
