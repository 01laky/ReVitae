using System.Reflection;
using System.Xml.Linq;
using ReVitae.Core;
using ReVitae.Core.Export;
using ReVitae.Core.Import.Structured;
using ReVitae.Core.Localization;
using ReVitae.Tests.Export;

namespace ReVitae.Tests.Import.Structured;

public sealed class EuropassXmlMapperEdgeCaseTests
{
	[Fact]
	public void Map_RejectsNonEuropassXml()
	{
		var doc = XDocument.Parse("<root><name>Jane</name></root>");

		var result = EuropassXmlMapper.Map(doc);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorUnsupportedStructuredFormat, result.ErrorMessageKey);
	}

	[Fact]
	public void Map_ImportsPersonalInformationFromMinimalEuropassFixture()
	{
		const string xml = """
            <SkillsPassport xmlns="http://europass.cedefop.europa.eu">
              <Identification>
                <PersonName>
                  <FirstName>Jane</FirstName>
                  <Surname>Doe</Surname>
                </PersonName>
                <ContactInfo>
                  <Email>jane@example.com</Email>
                </ContactInfo>
              </Identification>
            </SkillsPassport>
            """;

		var result = EuropassXmlMapper.Map(XDocument.Parse(xml));

		Assert.True(result.Success);
		Assert.Equal("Jane", result.Personal.FirstName);
		Assert.Equal("Doe", result.Personal.LastName);
		Assert.Equal("jane@example.com", result.Personal.Email);
	}
}

public sealed class HrXmlMapperEdgeCaseTests
{
	[Fact]
	public void Map_RejectsXmlWithoutHrSignals()
	{
		var doc = XDocument.Parse("<catalog><item>test</item></catalog>");

		var result = HrXmlMapper.Map(doc);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorUnsupportedStructuredFormat, result.ErrorMessageKey);
	}

	[Fact]
	public void Map_ImportsPersonalInformationFromPositionHistoryFixture()
	{
		const string xml = """
            <Resume xmlns="http://www.hr-xml.org/3">
              <GivenName>Jane</GivenName>
              <FamilyName>Doe</FamilyName>
              <CommunicationAddress>jane@example.com</CommunicationAddress>
              <PositionHistory>
                <PositionTitle>Engineer</PositionTitle>
                <EmployerOrgName>Acme Corp</EmployerOrgName>
              </PositionHistory>
            </Resume>
            """;

		var result = HrXmlMapper.Map(XDocument.Parse(xml));

		Assert.True(result.Success);
		Assert.Equal("Jane", result.Personal.FirstName);
		Assert.Equal("Doe", result.Personal.LastName);
		Assert.Equal("jane@example.com", result.Personal.Email);
		Assert.Single(result.WorkExperienceEntries);
		Assert.Equal("Engineer", result.WorkExperienceEntries[0].JobTitle);
		Assert.Equal("Acme Corp", result.WorkExperienceEntries[0].Company);
	}

	[Fact]
	public void Map_RoundTripsExportedHrXmlWorkExperienceAndEmail()
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();

		using var stream = new MemoryStream();
		var exportResult = CvDocumentExporter.Export(
			CvExportTestFixtures.CreateRepresentativeDocument(),
			source,
			CvExportFormat.HrXml,
			stream);
		Assert.True(exportResult.Success);
		stream.Position = 0;

		var result = HrXmlMapper.Map(XDocument.Load(stream));

		Assert.True(result.Success);
		Assert.Equal("Ladislav", result.Personal.FirstName);
		Assert.Equal("Kostolný", result.Personal.LastName);
		Assert.Equal("ladislav@example.com", result.Personal.Email);
		Assert.NotEmpty(result.WorkExperienceEntries);
		Assert.Contains(result.WorkExperienceEntries, entry => entry.Company.Contains("Acme", StringComparison.OrdinalIgnoreCase));
	}
}

public sealed class AppVersionEdgeCaseTests
{
	[Fact]
	public void GetSemVerBase_StripsBuildMetadata()
	{
		var method = typeof(AppVersion).GetMethod("GetSemVerBase", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.NotNull(method);

		var stripped = method!.Invoke(null, ["1.2.3+build.42"]) as string;

		Assert.Equal("1.2.3", stripped);
	}

	[Fact]
	public void HasPreReleaseLabel_DetectsPrereleaseSuffix()
	{
		var method = typeof(AppVersion).GetMethod("HasPreReleaseLabel", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.NotNull(method);

		Assert.True((bool)method!.Invoke(null, ["1.0.0-beta.1"])!);
		Assert.False((bool)method.Invoke(null, ["1.0.0"])!);
	}
}
