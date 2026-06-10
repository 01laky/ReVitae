using System.Text;
using ReVitae.Core.Export;
using ReVitae.Core.Export.Pdf;
using ReVitae.Core.Localization;
using UglyToad.PdfPig;

namespace ReVitae.Tests.Export;

public sealed class QuestPdfCvExporterTests
{
	private readonly QuestPdfCvExporter _exporter = new();

	public static IEnumerable<object[]> TemplateIds =>
		Enum.GetValues<CvExportTemplateId>().Select(templateId => new object[] { templateId });

	[Theory]
	[MemberData(nameof(TemplateIds))]
	public void Export_ProducesValidPdfBytesForEachTemplate(CvExportTemplateId templateId)
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument(templateId);

		var pdfBytes = _exporter.Export(document);

		Assert.NotNull(pdfBytes);
		Assert.NotEmpty(pdfBytes);
		Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdfBytes.AsSpan(0, 4)), StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(TemplateIds))]
	public void Export_ContainsUnicodeCvContent(CvExportTemplateId templateId)
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument(templateId);

		var pdfText = ExtractPdfText(_exporter.Export(document));

		// PdfPig's text segmentation differs across platforms: a large centered/spaced name
		// heading (e.g. MinimalCenter) is extracted with inter-letter whitespace on some
		// runners. The property under test is "diacritics survive the export", not exact
		// spacing — so compare against a whitespace-stripped copy.
		var compact = RemoveWhitespace(pdfText);

		Assert.True(
			compact.Contains("Kostolný", StringComparison.Ordinal)
				|| compact.Contains("KOSTOLNÝ", StringComparison.Ordinal),
			"Expected the exported PDF to contain the candidate name with preserved diacritics.");
		Assert.Contains("Košice", compact, StringComparison.Ordinal);
		Assert.Contains("súčasnosť", compact, StringComparison.Ordinal);
	}

	private static string RemoveWhitespace(string text) =>
		// Strip control characters as well as whitespace: Windows PdfPig runners interleave NUL
		// into the extracted text layer for some templates, which would otherwise break the
		// diacritic substring matches.
		string.Concat(text.Where(character => !char.IsWhiteSpace(character) && !char.IsControl(character)));

	[Fact]
	public void Export_LongContentProducesMultiplePages()
	{
		var document = CvExportTestFixtures.CreateLongContentDocument();

		using var pdfStream = new MemoryStream(_exporter.Export(document));
		using var pdfDocument = PdfDocument.Open(pdfStream);

		Assert.True(pdfDocument.NumberOfPages >= 2);
	}

	[Fact]
	public void Export_PersonalInfoOnlyOmitsEmptyOptionalSections()
	{
		var document = CvExportTestFixtures.CreatePersonalInfoOnlyDocument();
		var localizer = new AppLocalizer("en");
		var workExperienceLabel = localizer.Get(TranslationKeys.PreviewWorkExperience);
		var educationLabel = localizer.Get(TranslationKeys.PreviewEducation);

		var pdfText = ExtractPdfText(_exporter.Export(document));

		Assert.Contains("Jane Doe", pdfText, StringComparison.Ordinal);
		Assert.DoesNotContain(workExperienceLabel, pdfText, StringComparison.Ordinal);
		Assert.DoesNotContain(educationLabel, pdfText, StringComparison.Ordinal);
	}

	[Fact]
	public void Export_ToStream_WritesSameBytesAsByteExport()
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument();
		var expectedBytes = _exporter.Export(document);

		using var stream = new MemoryStream();
		_exporter.Export(document, stream);

		Assert.Equal(expectedBytes, stream.ToArray());
	}

	[Fact]
	public void Export_ToStream_ThrowsWhenDestinationIsNotWritable()
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument();
		using var stream = new MemoryStream();
		stream.Close();

		Assert.Throws<InvalidOperationException>(() => _exporter.Export(document, stream));
	}

	[Fact]
	public void Export_UsesSelectedTemplateId()
	{
		var classicBytes = _exporter.Export(
			CvExportTestFixtures.CreateRepresentativeDocument(CvExportTemplateId.ClassicSidebar));
		var modernBytes = _exporter.Export(
			CvExportTestFixtures.CreateRepresentativeDocument(CvExportTemplateId.ModernSidebar));

		Assert.NotEqual(classicBytes, modernBytes);
	}

	[Fact]
	public void Export_CarriesLocalizedSectionLabels()
	{
		var localizer = new AppLocalizer("en");
		var document = CvExportTestFixtures.CreateRepresentativeDocument(localizer: localizer);
		var pdfText = ExtractPdfText(_exporter.Export(document));

		Assert.Contains(localizer.Get(TranslationKeys.PreviewWorkExperience), pdfText, StringComparison.Ordinal);
		Assert.Contains(localizer.Get(TranslationKeys.PreviewSkills), pdfText, StringComparison.Ordinal);
	}

	private static string ExtractPdfText(byte[] pdfBytes)
	{
		using var stream = new MemoryStream(pdfBytes);
		using var document = PdfDocument.Open(stream);

		return string.Join(
			'\n',
			document.GetPages().Select(page => page.Text));
	}
}
