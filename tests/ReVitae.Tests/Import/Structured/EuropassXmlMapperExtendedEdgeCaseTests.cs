using System.Xml.Linq;
using ReVitae.Core.Import.Structured;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.Structured;

public sealed class EuropassXmlMapperExtendedEdgeCaseTests
{
	[Fact]
	public void Map_PartialXmlMissingIdentification_ReturnsNoStructuredData()
	{
		const string xml = """
            <SkillsPassport xmlns="http://europass.cedefop.europa.eu">
              <LearnerInfo />
            </SkillsPassport>
            """;

		var result = EuropassXmlMapper.Map(XDocument.Parse(xml));

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorNoStructuredData, result.ErrorMessageKey);
	}

	[Fact]
	public void Map_WrongNamespace_IsRejected()
	{
		var doc = XDocument.Parse("<SkillsPassport xmlns=\"http://example.com\"><Identification /></SkillsPassport>");

		var result = EuropassXmlMapper.Map(doc);

		Assert.False(result.Success);
	}

	[Fact]
	public void Map_MissingWorkSection_StillImportsPersonal()
	{
		const string xml = """
            <SkillsPassport xmlns="http://europass.cedefop.europa.eu">
              <Identification>
                <PersonName>
                  <FirstName>Ada</FirstName>
                  <Surname>Lovelace</Surname>
                </PersonName>
                <ContactInfo>
                  <Email>ada@example.com</Email>
                </ContactInfo>
              </Identification>
            </SkillsPassport>
            """;

		var result = EuropassXmlMapper.Map(XDocument.Parse(xml));

		Assert.True(result.Success);
		Assert.Equal("Ada", result.Personal.FirstName);
		Assert.Empty(result.WorkExperienceEntries);
	}

	[Fact]
	public void Map_EmptyEmail_DoesNotThrow()
	{
		const string xml = """
            <SkillsPassport xmlns="http://europass.cedefop.europa.eu">
              <Identification>
                <PersonName>
                  <FirstName>Empty</FirstName>
                  <Surname>Email</Surname>
                </PersonName>
                <ContactInfo>
                  <Email></Email>
                </ContactInfo>
              </Identification>
            </SkillsPassport>
            """;

		var result = EuropassXmlMapper.Map(XDocument.Parse(xml));

		Assert.True(result.Success);
		Assert.Equal(string.Empty, result.Personal.Email);
	}

	[Fact]
	public void Map_MalformedInnerXml_ThrowsDuringParse()
	{
		Assert.Throws<System.Xml.XmlException>(() => XDocument.Parse("<root><unclosed></root>"));
	}

	[Fact]
	public void Map_MinimalWorkExperienceEntry()
	{
		const string xml = """
            <SkillsPassport xmlns="http://europass.cedefop.europa.eu">
              <Identification>
                <PersonName>
                  <FirstName>Work</FirstName>
                  <Surname>Tester</Surname>
                </PersonName>
              </Identification>
              <WorkExperience>
                <WorkExperience>
                  <Period>
                    <From><Year>2020</Year></From>
                    <To><Year>2022</Year></To>
                  </Period>
                  <Position><Label>Engineer</Label></Position>
                  <Employer><Name>Acme</Name></Employer>
                </WorkExperience>
              </WorkExperience>
            </SkillsPassport>
            """;

		var result = EuropassXmlMapper.Map(XDocument.Parse(xml));

		Assert.True(result.Success);
		Assert.NotEmpty(result.WorkExperienceEntries);
		Assert.Contains(result.WorkExperienceEntries, entry => entry.JobTitle == "Engineer");
		Assert.Contains(result.WorkExperienceEntries, entry => entry.Company == "Acme");
	}
}
