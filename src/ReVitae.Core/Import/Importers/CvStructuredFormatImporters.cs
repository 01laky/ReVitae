using System.Text;
using System.Xml.Linq;
using ReVitae.Core.Import.Structured;
using ReVitae.Core.Import.Xml;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Importers;

public sealed class JsonResumeCvFormatImporter : ICvFormatImporter
{
    public CvImportFormat Format => CvImportFormat.JsonResume;

    public CvImportResult Import(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                return CvImportResult.Failed(TranslationKeys.ImportErrorEmptyDocument);
            }

            return JsonResumeMapper.Map(json);

        }

        catch (IOException)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);
        }

    }

}

public sealed class ReVitaeJsonCvFormatImporter : ICvFormatImporter
{
    public CvImportFormat Format => CvImportFormat.ReVitaeJson;

    public CvImportResult Import(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                return CvImportResult.Failed(TranslationKeys.ImportErrorEmptyDocument);
            }

            return ReVitaeJsonMapper.Map(json);

        }

        catch (IOException)
        {

            return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);


        }

    }

}

public sealed class YamlCvFormatImporter : ICvFormatImporter

{

    public CvImportFormat Format => CvImportFormat.YamlCv;



    public CvImportResult Import(string filePath)

    {

        try

        {

            var yaml = File.ReadAllText(filePath, Encoding.UTF8);

            if (string.IsNullOrWhiteSpace(yaml))

            {

                return CvImportResult.Failed(TranslationKeys.ImportErrorEmptyDocument);

            }



            return ParseYaml(yaml);



        }

        catch (IOException)


        {

            return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);

        }

    }



    private static CvImportResult ParseYaml(string yaml)

    {


        try

        {


            var flavor = YamlStructuredBridge.SniffRootKeys(yaml);

            switch (flavor)

            {

                case StructuredYamlFlavor.ReVitaeNative:

                    return ReVitaeJsonMapper.Map(YamlStructuredBridge.MappingRootToJson(yaml));

                case StructuredYamlFlavor.JsonResume:

                    return JsonResumeMapper.Map(YamlStructuredBridge.MappingRootToJson(yaml));

                default:

                    return CvImportResult.Failed(TranslationKeys.ImportErrorUnsupportedStructuredFormat);


            }

        }

        catch (Exception)


        {

            return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);

        }

    }


}




public sealed class EuropassXmlCvFormatImporter : ICvFormatImporter
{
    public CvImportFormat Format => CvImportFormat.EuropassXml;

    public CvImportResult Import(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var document = SecureXmlReaderFactory.LoadXDocument(stream);
            return EuropassXmlMapper.Map(document);
        }
        catch (IOException)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);
        }
        catch (System.Xml.XmlException)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);
        }
    }
}

public sealed class HrXmlCvFormatImporter : ICvFormatImporter
{
    public CvImportFormat Format => CvImportFormat.HrXml;

    public CvImportResult Import(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var document = SecureXmlReaderFactory.LoadXDocument(stream);
            return HrXmlMapper.Map(document);
        }
        catch (IOException)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);
        }
        catch (System.Xml.XmlException)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);
        }
    }
}

public sealed class TabularCvFormatImporter : ICvFormatImporter
{
    public CvImportFormat Format => CvImportFormat.CsvTabular;

    public CvImportResult Import(string filePath)
    {
        try
        {
            var raw = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return CvImportResult.Failed(TranslationKeys.ImportErrorEmptyDocument);
            }

            var useTabs = Path.GetExtension(filePath).Equals(".tsv", StringComparison.OrdinalIgnoreCase);
            return TabularCvMapper.Map(raw, useTabs);
        }
        catch (IOException)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);
        }
    }
}

