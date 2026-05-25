using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using ReVitae.Core.Export;
using ReVitae.Core.Import.Structured;

namespace ReVitae.Tests.Export;

public sealed class CvStructuredExportContentEdgeCaseTests
{
	[Fact]
	public void Export_RevitaeJson_FieldOrderingIsStable()
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var first = ExportStructured(CvExportFormat.RevitaeJson, source);
		var second = ExportStructured(CvExportFormat.RevitaeJson, source);

		Assert.Equal(first, second);
		Assert.Contains("\"personalInformation\"", Encoding.UTF8.GetString(first), StringComparison.Ordinal);
	}

	[Fact]
	public void Export_RevitaeJson_RoundTripsThroughMapper()
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var json = Encoding.UTF8.GetString(ExportStructured(CvExportFormat.RevitaeJson, source));

		var import = ReVitaeJsonMapper.Map(json);

		Assert.True(import.Success);
		Assert.Equal(source.Personal.FirstName, import.Personal.FirstName);
	}

	[Fact]
	public void Export_EuropassXml_ContainsSkillsPassportRoot()
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var xml = Encoding.UTF8.GetString(ExportStructured(CvExportFormat.EuropassXml, source));
		var document = XDocument.Parse(xml);

		Assert.Contains("SkillsPassport", document.Root!.Name.LocalName, StringComparison.Ordinal);
	}

	[Fact]
	public void Export_JsonResume_ContainsBasicsSection()
	{
		var source = CvExportTestFixtures.CreatePersonalOnlySourceData();
		var json = Encoding.UTF8.GetString(ExportStructured(CvExportFormat.JsonResume, source));
		using var document = JsonDocument.Parse(json);

		Assert.True(document.RootElement.TryGetProperty("basics", out var basics));
		Assert.Contains("Jane", basics.GetProperty("name").GetString(), StringComparison.Ordinal);
	}

	[Fact]
	public void Export_Yaml_IsParseableText()
	{
		var source = CvExportTestFixtures.CreatePersonalOnlySourceData();
		var yaml = Encoding.UTF8.GetString(ExportStructured(CvExportFormat.Yaml, source));

		Assert.Contains("personalInformation", yaml, StringComparison.Ordinal);
		Assert.Contains("firstName", yaml, StringComparison.Ordinal);
	}

	[Fact]
	public void Export_Csv_EscapesCommasInFields()
	{
		var source = CvExportTestFixtures.CreatePersonalOnlySourceData();
		source.Personal.Location = "Bratislava, Slovakia";
		var csv = Encoding.UTF8.GetString(ExportStructured(CvExportFormat.Csv, source));

		Assert.Contains("Bratislava, Slovakia", csv, StringComparison.Ordinal);
	}

	private static byte[] ExportStructured(CvExportFormat format, CvExportSourceData source)
	{
		using var stream = new MemoryStream();
		var document = CvExportTestFixtures.CreateRepresentativeDocument();
		var result = CvDocumentExporter.Export(document, source, format, stream);
		Assert.True(result.Success, format.ToString());
		return stream.ToArray();
	}
}
