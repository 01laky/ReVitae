using System.Xml;
using System.Xml.Linq;

namespace ReVitae.Core.Import.Xml;

/// <summary>Shared XXE-safe <see cref="XmlReader"/> settings for all untrusted document XML imports.</summary>
public static class SecureXmlReaderFactory
{
    public static XmlReaderSettings CreateSecureSettings()
    {
        return new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreWhitespace = false,
            Async = false
        };
    }

    public static XmlReader CreateXmlReader(Stream input)
    {
        return XmlReader.Create(input, CreateSecureSettings());
    }

    public static XDocument LoadXDocument(Stream input)
    {
        using var reader = CreateXmlReader(input);
        return XDocument.Load(reader);
    }

    /// <inheritdoc cref="LoadXDocument(Stream)"/>
    public static XDocument LoadXDocument(Stream input, LoadOptions loadOptions)
    {
        using var reader = CreateXmlReader(input);
        return XDocument.Load(reader, loadOptions);
    }

    /// <summary>Loads a document from UTF-8 text using secure reader settings.</summary>
    public static XDocument ParseDocument(string utf8Markup)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(utf8Markup));
        return LoadXDocument(stream);
    }
}
