using System.Text.Json;
using System.Xml.Linq;
using ReVitae.Core.Import.Xml;

namespace ReVitae.Core.Import;

public static class CvImportFormatDetector
{
    public static CvImportFormat DetectFormat(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return CvImportFormat.Unknown;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();

        switch (extension)
        {
            case ".pdf":
                return CvImportFormat.Pdf;
            case ".docx":
                return CvImportFormat.Docx;
            case ".doc":
                return CvImportFormat.Doc;
            case ".odt":
                return CvImportFormat.Odt;
            case ".rtf":
                return CvImportFormat.Rtf;
            case ".txt":
                return CvImportFormat.PlainText;
            case ".md":
            case ".markdown":
                return CvImportFormat.Markdown;
            case ".htm":
            case ".html":
                return CvImportFormat.Html;
            case ".tex":
                return CvImportFormat.Latex;
            case ".abw":
                return CvImportFormat.Abw;
            case ".wps":
                return CvImportFormat.Wps;
            case ".pages":
                return CvImportFormat.Pages;
            case ".csv":
            case ".tsv":
                return CvImportFormat.CsvTabular;
            case ".yaml":
            case ".yml":
                return CvImportFormat.YamlCv;
            case ".xml":
                return PeekStructuredXml(filePath);
            case ".json":
                return fileName.EndsWith(".revitae.json", StringComparison.Ordinal)
                    ? CvImportFormat.ReVitaeJson
                    : PeekStructuredJson(filePath);
            default:
                return CvImportFormat.Unknown;
        }
    }

    private static CvImportFormat PeekStructuredJson(string path)
    {
        try
        {
            using var reader = File.OpenText(path);
            var markup = reader.ReadToEnd();

            using var document = JsonDocument.Parse(markup);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return CvImportFormat.Unknown;
            }

            if (root.TryGetProperty("revitaeVersion", out _))
            {
                return CvImportFormat.ReVitaeJson;
            }

            if (root.TryGetProperty("$schema", out var schema)
                && schema.ValueKind == JsonValueKind.String
                && schema.GetString()?.Contains("jsonresume", StringComparison.OrdinalIgnoreCase) == true)
            {
                return CvImportFormat.JsonResume;
            }

            foreach (var token in new[] { "basics", "work", "education", "skills" })
            {
                if (root.TryGetProperty(token, out _))
                {
                    return CvImportFormat.JsonResume;
                }
            }

            return CvImportFormat.Unknown;
        }
        catch (JsonException)
        {
            return CvImportFormat.Unknown;
        }
        catch (IOException)
        {
            return CvImportFormat.Unknown;
        }
    }

    private static CvImportFormat PeekStructuredXml(string path)
    {
        try
        {
            using Stream stream = File.OpenRead(path);
            var markup = SecureXmlReaderFactory.LoadXDocument(stream);
            foreach (var element in markup.Descendants())
            {
                var namespaceLower = element.Name.NamespaceName.ToLowerInvariant();
                if (namespaceLower.Contains("europass", StringComparison.OrdinalIgnoreCase))
                {
                    return CvImportFormat.EuropassXml;
                }

                if (element.Name.LocalName.Equals("SkillsPassport", StringComparison.OrdinalIgnoreCase)
                    || element.Name.LocalName.Equals("Esp", StringComparison.OrdinalIgnoreCase))
                {
                    return CvImportFormat.EuropassXml;
                }
            }

            var flattened = $"{markup.Root}".ToLowerInvariant();

            foreach (var token in EuropaTokens)
            {
                if (flattened.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    return CvImportFormat.EuropassXml;
                }
            }

            foreach (var token in ResumeTokens)
            {
                if (!flattened.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return CvImportFormat.HrXml;
            }

            return CvImportFormat.Unknown;
        }
        catch (IOException)
        {
            return CvImportFormat.Unknown;
        }
        catch (System.Xml.XmlException)
        {
            return CvImportFormat.Unknown;
        }
    }

    private static readonly string[] EuropaTokens =
    [
        "europass.cedefop",
        "europass/skills",
        "europass"
    ];

    private static readonly string[] ResumeTokens =
    [
        "candidate",
        "resume",
        "employmenthistory",
        "educationhistory",
        "experience",
        "<positionhistory",
        "hr-xml",
        "hropen.org"
    ];
}
