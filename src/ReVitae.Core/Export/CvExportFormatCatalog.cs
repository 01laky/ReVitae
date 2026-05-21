using ReVitae.Core.Localization;

namespace ReVitae.Core.Export;

public static class CvExportFormatCatalog
{
    private static readonly IReadOnlyList<CvExportFormatDescriptor> All =
    [
        Desc(CvExportFormat.Pdf, CvExportFormatCategory.Documents, "pdf", TranslationKeys.ExportFormatPdf, TranslationKeys.ExportFormatPdfHint, recommended: true),
        Desc(CvExportFormat.Docx, CvExportFormatCategory.Documents, "docx", TranslationKeys.ExportFormatDocx),
        Desc(CvExportFormat.Odt, CvExportFormatCategory.Documents, "odt", TranslationKeys.ExportFormatOdt),
        Desc(CvExportFormat.Rtf, CvExportFormatCategory.Documents, "rtf", TranslationKeys.ExportFormatRtf),
        Desc(CvExportFormat.Html, CvExportFormatCategory.WebAndText, "html", TranslationKeys.ExportFormatHtml),
        Desc(CvExportFormat.Markdown, CvExportFormatCategory.WebAndText, "markdown", TranslationKeys.ExportFormatMarkdown),
        Desc(CvExportFormat.Txt, CvExportFormatCategory.WebAndText, "txt", TranslationKeys.ExportFormatTxt),
        Desc(CvExportFormat.Latex, CvExportFormatCategory.WebAndText, "latex", TranslationKeys.ExportFormatLatex),
        Desc(CvExportFormat.RevitaeJson, CvExportFormatCategory.Structured, "revitae-json", TranslationKeys.ExportFormatRevitaeJson),
        Desc(CvExportFormat.JsonResume, CvExportFormatCategory.Structured, "json-resume", TranslationKeys.ExportFormatJsonResume),
        Desc(CvExportFormat.Yaml, CvExportFormatCategory.Structured, "yaml", TranslationKeys.ExportFormatYaml),
        Desc(CvExportFormat.EuropassXml, CvExportFormatCategory.Structured, "europass-xml", TranslationKeys.ExportFormatEuropassXml),
        Desc(CvExportFormat.HrXml, CvExportFormatCategory.Structured, "hr-xml", TranslationKeys.ExportFormatHrXml),
        Desc(CvExportFormat.Csv, CvExportFormatCategory.Structured, "csv", TranslationKeys.ExportFormatCsv),
        Desc(CvExportFormat.Tsv, CvExportFormatCategory.Structured, "tsv", TranslationKeys.ExportFormatTsv)
    ];

    public static IReadOnlyList<CvExportFormatDescriptor> GetAvailableFormats() => All;

    public static IReadOnlyList<CvExportFormatDescriptor> GetEnabledFormats() =>
        All.Where(d => d.IsEnabled).ToArray();

    public static CvExportFormatDescriptor Get(CvExportFormat format) =>
        All.First(d => d.Format == format);

    public static string GetExtension(CvExportFormat format) => format switch
    {
        CvExportFormat.Pdf => ".pdf",
        CvExportFormat.Docx => ".docx",
        CvExportFormat.Odt => ".odt",
        CvExportFormat.Rtf => ".rtf",
        CvExportFormat.Html => ".html",
        CvExportFormat.Markdown => ".md",
        CvExportFormat.Txt => ".txt",
        CvExportFormat.Latex => ".tex",
        CvExportFormat.RevitaeJson => ".revitae.json",
        CvExportFormat.JsonResume => ".json",
        CvExportFormat.Yaml => ".yaml",
        CvExportFormat.EuropassXml => ".xml",
        CvExportFormat.HrXml => ".xml",
        CvExportFormat.Csv => ".csv",
        CvExportFormat.Tsv => ".tsv",
        _ => ".bin"
    };

    public static string GetFilenameSuffix(CvExportFormat format) => format switch
    {
        CvExportFormat.EuropassXml => "_europass",
        CvExportFormat.HrXml => "_hrxml",
        _ => string.Empty
    };

    private static CvExportFormatDescriptor Desc(
        CvExportFormat format,
        CvExportFormatCategory category,
        string iconSlug,
        string labelKey,
        string? hintKey = null,
        bool recommended = false,
        bool enabled = true,
        string? disabledTooltipKey = null) =>
        new(format, category, iconSlug, labelKey, hintKey, recommended, enabled, disabledTooltipKey);
}
