using ReVitae.Core.Export;
using ReVitae.Tests.Infrastructure;

namespace ReVitae.Tests.Export;

/// <summary>
/// Prompt 049 C1 — Unicode / i18n torture for the export pipeline. Text-based formats must
/// round-trip arbitrary scripts byte-for-byte; every format must export without throwing on
/// RTL, CJK, emoji, combining marks, surrogate pairs, and zero-width / bidi control chars.
/// (PDF uses the Latin Arimo face, so non-Latin glyphs are not asserted in the PDF text
/// layer — only that export stays graceful and Latin diacritics survive.)
/// </summary>
[Trait("Category", "Unicode")]
public sealed class UnicodeExportTortureTests
{
	private const string Cjk = "山田太郎";
	private const string Arabic = "محمد";
	private const string Emoji = "🚀";
	private const string LatinExtended = "Łódź Kraków café";

	private static readonly string TortureSummary =
		$"{Cjk} {Arabic} {Emoji} {LatinExtended} ​‮́ 🚀";

	private static CvExportDocument TortureDocument() =>
		CvExportTestFixtures.CreateRepresentativeDocument() with
		{
			FirstName = "山田",
			LastName = "太郎",
			ShortSummary = TortureSummary
		};

	public static IEnumerable<object[]> TextFormats =>
	[
		[CvExportFormat.Html],
		[CvExportFormat.Markdown],
		[CvExportFormat.Txt]
	];

	public static IEnumerable<object[]> AllDocumentFormats =>
		CvExportTestHarness.DocumentFormats.Select(format => new object[] { format });

	[Theory]
	[MemberData(nameof(TextFormats))]
	public void TextFormat_PreservesNonLatinScripts(CvExportFormat format)
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var text = CvExportTestHarness.ExportText(TortureDocument(), source, format);

		Assert.Contains(Cjk, text, StringComparison.Ordinal);
		Assert.Contains(Arabic, text, StringComparison.Ordinal);
		Assert.Contains(Emoji, text, StringComparison.Ordinal);
		Assert.Contains("Łódź", text, StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(AllDocumentFormats))]
	public void EveryDocumentFormat_ExportsTortureContentWithoutThrowing(CvExportFormat format)
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();

		Assert.True(
			CvExportTestHarness.TryExport(TortureDocument(), source, format, out var bytes),
			$"{format} failed to export Unicode torture content.");
		Assert.NotEmpty(bytes);
	}

	[Fact]
	public void Pdf_PreservesLatinExtendedDiacritics()
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var document = CvExportTestFixtures.CreateRepresentativeDocument() with
		{
			ShortSummary = LatinExtended
		};

		var compact = CvExportTestHarness.RemoveWhitespace(
			CvExportTestHarness.ExtractPdfText(
				CvExportTestHarness.ExportBytes(document, source, CvExportFormat.Pdf)));

		Assert.Contains("Łódź", compact, StringComparison.Ordinal);
	}
}
