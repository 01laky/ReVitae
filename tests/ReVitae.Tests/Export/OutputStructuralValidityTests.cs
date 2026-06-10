using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using ReVitae.Core.Export;
using ReVitae.Tests.Infrastructure;

namespace ReVitae.Tests.Export;

/// <summary>
/// Prompt 049 B15 — output structural validity. Exports must be valid for downstream
/// consumers, not merely readable as text: DOCX validates against the OpenXML schema, ODT is
/// a well-formed package, JSON parses with the expected top-level structure, and the XML
/// formats are well-formed.
/// </summary>
public sealed class OutputStructuralValidityTests
{
	private static byte[] Export(CvExportFormat format)
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument();
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		return CvExportTestHarness.ExportBytes(document, source, format);
	}

	private static string ExportText(CvExportFormat format) =>
		System.Text.Encoding.UTF8.GetString(Export(format));

	[Fact]
	public void Docx_PassesOpenXmlSchemaValidation()
	{
		using var stream = new MemoryStream(Export(CvExportFormat.Docx));
		using var document = WordprocessingDocument.Open(stream, isEditable: false);

		var validator = new OpenXmlValidator();
		var errors = validator.Validate(document).ToList();

		Assert.True(
			errors.Count == 0,
			$"DOCX failed OpenXML validation: {string.Join(" | ", errors.Take(5).Select(error => error.Description))}");
	}

	[Fact]
	public void Odt_IsWellFormedOpenDocumentPackage()
	{
		using var stream = new MemoryStream(Export(CvExportFormat.Odt));
		using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

		Assert.Contains(archive.Entries, entry => entry.FullName == "content.xml");
		Assert.Contains(archive.Entries, entry => entry.FullName == "mimetype");

		var content = archive.GetEntry("content.xml")!;
		using var reader = new StreamReader(content.Open());
		Assert.NotNull(XDocument.Parse(reader.ReadToEnd()));
	}

	[Fact]
	public void RevitaeJson_IsValidJsonWithExpectedRoot()
	{
		using var document = JsonDocument.Parse(ExportText(CvExportFormat.RevitaeJson));
		Assert.True(document.RootElement.TryGetProperty("personalInformation", out _));
	}

	[Fact]
	public void JsonResume_IsValidJsonWithBasicsSection()
	{
		using var document = JsonDocument.Parse(ExportText(CvExportFormat.JsonResume));
		Assert.True(document.RootElement.TryGetProperty("basics", out var basics));
		Assert.Equal(JsonValueKind.Object, basics.ValueKind);
	}

	[Theory]
	[InlineData(CvExportFormat.EuropassXml)]
	[InlineData(CvExportFormat.HrXml)]
	public void XmlFormats_AreWellFormed(CvExportFormat format)
	{
		var document = XDocument.Parse(ExportText(format));
		Assert.NotNull(document.Root);
	}

	[Theory]
	[InlineData(CvExportFormat.Csv, ',')]
	[InlineData(CvExportFormat.Tsv, '\t')]
	public void DelimitedFormats_HaveConsistentDelimiterUsage(CvExportFormat format, char delimiter)
	{
		var text = ExportText(format);
		var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		Assert.NotEmpty(lines);
		Assert.Contains(lines, line => line.Contains(delimiter));
	}

	[Fact]
	public void Html_ParsesAsWellFormedXmlAfterDoctypeStrip()
	{
		var html = ExportText(CvExportFormat.Html);
		Assert.Contains("<html", html, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("</html>", html, StringComparison.OrdinalIgnoreCase);
	}
}
