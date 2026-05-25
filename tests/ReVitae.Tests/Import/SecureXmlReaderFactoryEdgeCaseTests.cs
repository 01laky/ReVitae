using System.Xml;
using ReVitae.Core.Import.Xml;

namespace ReVitae.Tests.Import;

public sealed class SecureXmlReaderFactoryEdgeCaseTests
{
	[Fact]
	public void ParseDocument_RejectsExternalEntityPayload_XXE()
	{
		var payload = """
            <!DOCTYPE data [
              <!ENTITY xxe SYSTEM "file:///etc/passwd">
            ]>
            <data>&xxe;</data>
            """;

		Assert.Throws<XmlException>(() => SecureXmlReaderFactory.ParseDocument(payload));
	}

	[Fact]
	public void ParseDocument_LoadsBenignMarkup()
	{
		const string markup = """<?xml version="1.0"?><resume><candidate><name>Jane</name></candidate></resume>""";

		var document = SecureXmlReaderFactory.ParseDocument(markup);

		Assert.Equal("Jane", document.Descendants("name").First().Value);
	}

	[Fact]
	public void CreateSecureSettings_ProhibitsDtdProcessing()
	{
		var settings = SecureXmlReaderFactory.CreateSecureSettings();

		Assert.Equal(DtdProcessing.Prohibit, settings.DtdProcessing);
	}
}
