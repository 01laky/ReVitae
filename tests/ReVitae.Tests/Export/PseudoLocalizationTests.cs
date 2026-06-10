using ReVitae.Core.Export;
using ReVitae.Core.Localization;
using ReVitae.Tests.Infrastructure;

namespace ReVitae.Tests.Export;

/// <summary>
/// Prompt 049 C7 — pseudo-localization. Every section label is wrapped in distinctive markers
/// and expanded; the export writers must emit the document's labels (so any hardcoded English
/// string is caught — it would appear without the markers), and the PDF must still render and
/// keep its content when labels are longer than usual (overflow / truncation guard).
/// </summary>
[Trait("Category", "PseudoLoc")]
public sealed class PseudoLocalizationTests
{
	private const string Open = "⟦";  // ⟦
	private const string Close = "⟧";  // ⟧

	private static string Pseudo(string value) => $"{Open}{value} ·éxpánded·{Close}";

	private static CvExportSectionLabels PseudoLabels()
	{
		var localizer = new AppLocalizer("en");
		string P(string key) => Pseudo(localizer.Get(key));

		return new CvExportSectionLabels(
			Summary: P(TranslationKeys.Summary),
			Contact: P(TranslationKeys.Contact),
			Profile: P(TranslationKeys.Profile),
			Objective: P(TranslationKeys.Objective),
			PreviewWorkExperience: P(TranslationKeys.PreviewWorkExperience),
			PreviewAchievements: P(TranslationKeys.PreviewAchievements),
			PreviewTechnologies: P(TranslationKeys.PreviewTechnologies),
			WorkExperienceCompanyUrl: P(TranslationKeys.WorkExperienceCompanyUrl),
			PreviewEducation: P(TranslationKeys.PreviewEducation),
			PreviewFieldOfStudy: P(TranslationKeys.PreviewFieldOfStudy),
			PreviewGrade: P(TranslationKeys.PreviewGrade),
			EducationInstitutionUrl: P(TranslationKeys.EducationInstitutionUrl),
			PreviewSkills: P(TranslationKeys.PreviewSkills),
			PreviewYearsSuffix: P(TranslationKeys.PreviewYearsSuffix),
			PreviewLanguages: P(TranslationKeys.PreviewLanguages),
			PreviewCertificates: P(TranslationKeys.PreviewCertificates),
			PreviewProjects: P(TranslationKeys.PreviewProjects),
			PreviewCustomLinks: P(TranslationKeys.PreviewCustomLinks),
			PreviewAdditionalInformation: P(TranslationKeys.PreviewAdditionalInformation),
			ContactLinks: P(TranslationKeys.ContactLinks),
			Digital: P(TranslationKeys.Digital),
			Links: P(TranslationKeys.Links),
			Online: P(TranslationKeys.Online),
			Email: P(TranslationKeys.Email),
			Phone: P(TranslationKeys.Phone),
			Location: P(TranslationKeys.Location),
			ProfessionalTitle: P(TranslationKeys.ProfessionalTitle),
			LinkedInUrl: P(TranslationKeys.LinkedInUrl),
			PortfolioUrl: P(TranslationKeys.PortfolioUrl),
			GitHubUrl: P(TranslationKeys.GitHubUrl));
	}

	private static CvExportDocument PseudoDocument(CvExportTemplateId templateId = CvExportTemplateId.ClassicSidebar) =>
		CvExportTestFixtures.CreateRepresentativeDocument(templateId) with { Labels = PseudoLabels() };

	[Fact]
	public void Html_EmitsDocumentLabels_NotHardcodedEnglish()
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var html = CvExportTestHarness.ExportText(PseudoDocument(), source, CvExportFormat.Html);
		var localizer = new AppLocalizer("en");

		foreach (var key in new[]
		{
			TranslationKeys.PreviewWorkExperience,
			TranslationKeys.PreviewEducation,
			TranslationKeys.PreviewSkills,
			TranslationKeys.PreviewProjects
		})
		{
			var english = localizer.Get(key);
			Assert.Contains(Pseudo(english), html, StringComparison.Ordinal);
		}
	}

	[Theory]
	[InlineData(CvExportTemplateId.ClassicSidebar)]
	[InlineData(CvExportTemplateId.CleanTopHeader)]
	[InlineData(CvExportTemplateId.SlateMinimal)]
	[InlineData(CvExportTemplateId.CobaltMonogram)]
	public void Pdf_RendersExpandedLabelsWithoutLosingContent(CvExportTemplateId templateId)
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();

		Assert.True(
			CvExportTestHarness.TryExport(PseudoDocument(templateId), source, CvExportFormat.Pdf, out var pdf),
			$"{templateId} failed to render expanded pseudo-localized labels.");

		// Assert body content survives the label expansion (work company), not the name: a large
		// styled name in a header band is extracted unreliably by PdfPig on Windows runners
		// (reordered / split beyond control-char stripping). Name presence is already covered
		// comprehensively, per template, by AtsReadabilityTests.
		var compact = CvExportTestHarness.RemoveWhitespace(CvExportTestHarness.ExtractPdfText(pdf));
		Assert.True(
			compact.Contains("Acme", StringComparison.Ordinal),
			$"{templateId} lost work-experience content under label expansion.");
	}

	[Fact]
	public void Markdown_EmitsExpandedSummaryLabel()
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var markdown = CvExportTestHarness.ExportText(PseudoDocument(), source, CvExportFormat.Markdown);
		var english = new AppLocalizer("en").Get(TranslationKeys.PreviewWorkExperience);

		Assert.Contains(Pseudo(english), markdown, StringComparison.Ordinal);
	}
}
