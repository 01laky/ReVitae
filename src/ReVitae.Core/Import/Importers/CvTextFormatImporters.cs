using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Import.Pdf;

namespace ReVitae.Core.Import.Importers;

public sealed class PdfCvFormatImporter : TextCvFormatImporterBase
{
    public PdfCvFormatImporter(ICvTextExtractor extractor)
        : base(CvImportFormat.Pdf, extractor)
    {
    }

    public PdfCvFormatImporter()
        : base(CvImportFormat.Pdf, new PdfTextExtractorAdapter(new PdfPigTextExtractor()))
    {
    }
}

public sealed class PlainTextCvFormatImporter : TextCvFormatImporterBase
{
    public PlainTextCvFormatImporter()
        : base(CvImportFormat.PlainText, new PlainTextExtractor())
    {
    }
}

public sealed class MarkdownCvFormatImporter : TextCvFormatImporterBase
{
    public MarkdownCvFormatImporter()
        : base(CvImportFormat.Markdown, new MarkdownTextExtractor())
    {
    }
}

public sealed class HtmlCvFormatImporter : TextCvFormatImporterBase
{
    public HtmlCvFormatImporter()
        : base(CvImportFormat.Html, new HtmlTextExtractor())
    {
    }
}

public sealed class LatexCvFormatImporter : TextCvFormatImporterBase
{
    public LatexCvFormatImporter()
        : base(CvImportFormat.Latex, new LatexTextExtractor())
    {
    }
}

public sealed class RtfCvFormatImporter : TextCvFormatImporterBase
{
    public RtfCvFormatImporter()
        : base(CvImportFormat.Rtf, new RtfTextExtractor())
    {
    }
}

public sealed class DocxCvFormatImporter : TextCvFormatImporterBase
{
    public DocxCvFormatImporter()
        : base(CvImportFormat.Docx, new DocxTextExtractor())
    {
    }
}

public sealed class DocCvFormatImporter : TextCvFormatImporterBase
{
    public DocCvFormatImporter()
        : base(CvImportFormat.Doc, new DocTextExtractor())
    {
    }
}

public sealed class OdtCvFormatImporter : TextCvFormatImporterBase
{
    public OdtCvFormatImporter()
        : base(CvImportFormat.Odt, new OdtTextExtractor())
    {
    }
}

public sealed class AbwCvFormatImporter : TextCvFormatImporterBase
{
    public AbwCvFormatImporter()
        : base(CvImportFormat.Abw, new AbwTextExtractor())
    {
    }
}

public sealed class PagesCvFormatImporter : TextCvFormatImporterBase
{
    public PagesCvFormatImporter()
        : base(CvImportFormat.Pages, new PagesTextExtractor())
    {
    }
}

public sealed class WpsCvFormatImporter : TextCvFormatImporterBase
{
    public WpsCvFormatImporter()
        : base(CvImportFormat.Wps, new WpsTextExtractor())
    {
    }
}
