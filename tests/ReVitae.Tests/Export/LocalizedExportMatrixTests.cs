using ReVitae.Core.Export;
using ReVitae.Core.Localization;
using ReVitae.Tests.Infrastructure;

namespace ReVitae.Tests.Export;

/// <summary>
/// Prompt 049 B13 — localized export matrix. The exported document must carry the
/// candidate's chosen UI-language section labels into every document format, not silently
/// fall back to English. Cross of B2 (export) × B12 (localization).
/// </summary>
public sealed class LocalizedExportMatrixTests
{
	public static IEnumerable<object[]> Languages =>
		CvExportTestHarness.SupportedLanguageCodes.Select(code => new object[] { code });

	private static CvExportDocument LocalizedDocument(string languageCode)
	{
		var localizer = new AppLocalizer(languageCode);
		return CvExportTestFixtures.CreateRepresentativeDocument(
			CvExportTemplateId.ClassicSidebar,
			localizer);
	}

	private static (string Work, string Skills) ExpectedLabels(string languageCode)
	{
		var localizer = new AppLocalizer(languageCode);
		return (
			localizer.Get(TranslationKeys.PreviewWorkExperience),
			localizer.Get(TranslationKeys.PreviewSkills));
	}

	[Theory]
	[MemberData(nameof(Languages))]
	public void Pdf_CarriesLocalizedSectionLabels(string languageCode)
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var pdf = CvExportTestHarness.ExportBytes(LocalizedDocument(languageCode), source, CvExportFormat.Pdf);
		var compact = CvExportTestHarness.RemoveWhitespace(CvExportTestHarness.ExtractPdfText(pdf));
		var (work, skills) = ExpectedLabels(languageCode);

		Assert.Contains(CvExportTestHarness.RemoveWhitespace(work), compact, StringComparison.Ordinal);
		Assert.Contains(CvExportTestHarness.RemoveWhitespace(skills), compact, StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(Languages))]
	public void Html_CarriesLocalizedSectionLabels(string languageCode)
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var html = CvExportTestHarness.ExportText(LocalizedDocument(languageCode), source, CvExportFormat.Html);
		var (work, skills) = ExpectedLabels(languageCode);

		Assert.Contains(work, html, StringComparison.Ordinal);
		Assert.Contains(skills, html, StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(Languages))]
	public void Markdown_CarriesLocalizedSectionLabels(string languageCode)
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var markdown = CvExportTestHarness.ExportText(LocalizedDocument(languageCode), source, CvExportFormat.Markdown);
		var (work, _) = ExpectedLabels(languageCode);

		Assert.Contains(work, markdown, StringComparison.Ordinal);
	}

	[Fact]
	public void EverySupportedLanguage_ExportsAllDocumentFormatsWithoutError()
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();

		foreach (var languageCode in CvExportTestHarness.SupportedLanguageCodes)
		{
			var document = LocalizedDocument(languageCode);
			foreach (var format in CvExportTestHarness.DocumentFormats)
			{
				Assert.True(
					CvExportTestHarness.TryExport(document, source, format, out var bytes),
					$"{languageCode}/{format} export failed.");
				Assert.NotEmpty(bytes);
			}
		}
	}
}
