using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Export.Pdf;

namespace ReVitae.Core.Export;

internal static class CvVisualExportWriter
{
    public static void WritePlainText(CvExportDocument document, Stream output)
    {
        using var writer = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        writer.Write(BuildPlainBody(document));
        writer.Flush();
    }

    public static void WriteMarkdown(CvExportDocument document, Stream output)
    {
        using var writer = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        writer.Write(BuildMarkdownBody(document));
        writer.Flush();
    }

    public static void WriteHtml(CvExportDocument document, Stream output)
    {
        using var writer = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        writer.Write(BuildHtmlBody(document));
        writer.Flush();
    }

    public static void WriteRtf(CvExportDocument document, Stream output)
    {
        using var writer = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        writer.Write(BuildRtfBody(document));
        writer.Flush();
    }

    public static void WriteLatex(CvExportDocument document, Stream output)
    {
        using var writer = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        writer.Write(BuildLatexBody(document));
        writer.Flush();
    }

    public static void WriteDocx(CvExportDocument document, Stream output)
    {
        using var package = WordprocessingDocument.Create(output, WordprocessingDocumentType.Document, true);
        var mainPart = package.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;

        CvDocxPhotoInserter.TryAppendPhoto(mainPart, body, document.PhotoPath);
        AppendDocxHeading(body, document.FullName, 28);
        if (!string.IsNullOrWhiteSpace(document.ProfessionalTitle))
        {
            AppendDocxParagraph(body, document.ProfessionalTitle, bold: true);
        }

        AppendDocxSection(body, document.Labels.Contact, CvExportPreviewContentBuilder.BuildContactLines(document));
        AppendDocxSection(body, document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document));
        AppendDocxSection(body, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AppendDocxSection(body, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));
        AppendDocxSection(body, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
        AppendDocxSection(body, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document));
        AppendDocxSection(body, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document));
        AppendDocxSection(body, document.Labels.PreviewProjects, CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document));
        AppendDocxSection(body, document.Labels.PreviewCustomLinks, CvExportPreviewContentBuilder.BuildCustomLinksPreviewContent(document));
        AppendDocxSection(body, document.Labels.PreviewAdditionalInformation, CvExportPreviewContentBuilder.BuildAdditionalInformationPreviewContent(document));

        mainPart.Document.Save();
    }

    public static void WriteOdt(CvExportDocument document, Stream output)
    {
        var plain = BuildPlainBody(document);
        var contentXml = BuildOdtContentXml(document.FullName, plain);
        using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        WriteZipEntry(archive, "mimetype", "application/vnd.oasis.opendocument.text", compress: false);
        WriteZipEntry(archive, "content.xml", contentXml);
        WriteZipEntry(archive, "META-INF/manifest.xml", BuildOdtManifest());
    }

    private static string BuildPlainBody(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        if (!string.IsNullOrWhiteSpace(document.ProfessionalTitle))
        {
            sb.AppendLine(document.ProfessionalTitle);
        }

        sb.AppendLine();
        AppendSection(sb, document.Labels.Contact, CvExportPreviewContentBuilder.BuildContactLines(document));
        AppendSection(sb, document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AppendSection(sb, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewProjects, CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewCustomLinks, CvExportPreviewContentBuilder.BuildCustomLinksPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewAdditionalInformation, CvExportPreviewContentBuilder.BuildAdditionalInformationPreviewContent(document));
        return sb.ToString().TrimEnd();
    }

    private static string BuildMarkdownBody(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# " + EscapeMd(document.FullName));
        if (!string.IsNullOrWhiteSpace(document.ProfessionalTitle))
        {
            sb.AppendLine();
            sb.AppendLine("*" + EscapeMd(document.ProfessionalTitle) + "*");
        }

        AppendMdSection(sb, document.Labels.Contact, CvExportPreviewContentBuilder.BuildContactLines(document));
        AppendMdSection(sb, document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document));
        AppendMdSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AppendMdSection(sb, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));
        AppendMdSection(sb, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
        AppendMdSection(sb, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document));
        AppendMdSection(sb, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document));
        AppendMdSection(sb, document.Labels.PreviewProjects, CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document));
        AppendMdSection(sb, document.Labels.PreviewCustomLinks, CvExportPreviewContentBuilder.BuildCustomLinksPreviewContent(document));
        AppendMdSection(sb, document.Labels.PreviewAdditionalInformation, CvExportPreviewContentBuilder.BuildAdditionalInformationPreviewContent(document));
        return sb.ToString().TrimEnd();
    }

    private static string BuildHtmlBody(CvExportDocument document)
    {
        var accent = document.TemplateId switch
        {
            CvExportTemplateId.ClassicSidebar => "#F47C2C",
            CvExportTemplateId.ModernSidebar => "#444444",
            CvExportTemplateId.CleanTopHeader => "#2563EB",
            CvExportTemplateId.DarkSidebarAccent => "#1F2937",
            _ => "#2563EB"
        };

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.AppendLine("<title>" + EscapeHtml(document.FullName) + " — CV</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;line-height:1.5;margin:2rem;color:#111;}");
        sb.AppendLine("h1{color:" + accent + ";margin-bottom:0.2rem;}");
        sb.AppendLine("h2{border-bottom:2px solid " + accent + ";padding-bottom:0.25rem;margin-top:1.5rem;}");
        sb.AppendLine(".subtitle{color:#555;margin-top:0;}");
        sb.AppendLine(".profile-photo{width:96px;height:96px;border-radius:50%;object-fit:cover;margin-bottom:0.75rem;}");
        sb.AppendLine("pre{white-space:pre-wrap;font-family:inherit;}");
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body>");

        var photoDataUri = ProfilePhotoBytes.TryGetDataUri(document.PhotoPath);
        if (!string.IsNullOrWhiteSpace(photoDataUri))
        {
            sb.AppendLine("<img class=\"profile-photo\" src=\"" + photoDataUri + "\" alt=\"Profile photo\" />");
        }

        sb.AppendLine("<h1>" + EscapeHtml(document.FullName) + "</h1>");
        if (!string.IsNullOrWhiteSpace(document.ProfessionalTitle))
        {
            sb.AppendLine("<p class=\"subtitle\">" + EscapeHtml(document.ProfessionalTitle) + "</p>");
        }

        AppendHtmlSection(sb, document.Labels.Contact, CvExportPreviewContentBuilder.BuildContactLines(document));
        AppendHtmlSection(sb, document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document));
        AppendHtmlSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AppendHtmlSection(sb, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));
        AppendHtmlSection(sb, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
        AppendHtmlSection(sb, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document));
        AppendHtmlSection(sb, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document));
        AppendHtmlSection(sb, document.Labels.PreviewProjects, CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document));
        AppendHtmlSection(sb, document.Labels.PreviewCustomLinks, CvExportPreviewContentBuilder.BuildCustomLinksPreviewContent(document));
        AppendHtmlSection(sb, document.Labels.PreviewAdditionalInformation, CvExportPreviewContentBuilder.BuildAdditionalInformationPreviewContent(document));
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string BuildRtfBody(CvExportDocument document)
    {
        var plain = BuildPlainBody(document);
        var sb = new StringBuilder();
        sb.Append(@"{\rtf1\ansi\deff0");
        foreach (var ch in plain)
        {
            if (ch == '\\' || ch == '{' || ch == '}')
            {
                sb.Append('\\').Append(ch);
            }
            else if (ch <= 127)
            {
                sb.Append(ch);
            }
            else
            {
                sb.Append("\\u").Append((short)ch).Append('?');
            }
        }

        sb.Append('}');
        return sb.ToString();
    }

    private static string BuildLatexBody(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine("\\documentclass[11pt,a4paper]{article}");
        sb.AppendLine("\\usepackage[utf8]{inputenc}");
        sb.AppendLine("\\usepackage[T1]{fontenc}");
        sb.AppendLine("\\begin{document}");
        sb.AppendLine("\\section*{" + EscapeLatex(document.FullName) + "}");
        if (!string.IsNullOrWhiteSpace(document.ProfessionalTitle))
        {
            sb.AppendLine("\\textit{" + EscapeLatex(document.ProfessionalTitle) + "}");
            sb.AppendLine();
        }

        AppendLatexSection(sb, document.Labels.Contact, CvExportPreviewContentBuilder.BuildContactLines(document));
        AppendLatexSection(sb, document.Labels.Summary, CvExportPreviewContentBuilder.BuildSummary(document));
        AppendLatexSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AppendLatexSection(sb, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));
        AppendLatexSection(sb, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
        AppendLatexSection(sb, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document));
        AppendLatexSection(sb, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document));
        AppendLatexSection(sb, document.Labels.PreviewProjects, CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document));
        AppendLatexSection(sb, document.Labels.PreviewCustomLinks, CvExportPreviewContentBuilder.BuildCustomLinksPreviewContent(document));
        AppendLatexSection(sb, document.Labels.PreviewAdditionalInformation, CvExportPreviewContentBuilder.BuildAdditionalInformationPreviewContent(document));
        sb.AppendLine("\\end{document}");
        return sb.ToString();
    }

    private static void AppendSection(StringBuilder sb, string title, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content == "-")
        {
            return;
        }

        sb.AppendLine(title.ToUpperInvariant());
        sb.AppendLine(new string('-', Math.Min(title.Length, 40)));
        sb.AppendLine(WrapPlain(content));
        sb.AppendLine();
    }

    private static void AppendMdSection(StringBuilder sb, string title, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content == "-")
        {
            return;
        }

        sb.AppendLine("## " + EscapeMd(title));
        sb.AppendLine();
        sb.AppendLine(EscapeMd(content));
        sb.AppendLine();
    }

    private static void AppendHtmlSection(StringBuilder sb, string title, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content == "-")
        {
            return;
        }

        sb.AppendLine("<h2>" + EscapeHtml(title) + "</h2>");
        sb.AppendLine("<pre>" + EscapeHtml(content) + "</pre>");
    }

    private static void AppendLatexSection(StringBuilder sb, string title, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content == "-")
        {
            return;
        }

        sb.AppendLine("\\subsection*{" + EscapeLatex(title) + "}");
        sb.AppendLine("\\begin{verbatim}");
        sb.AppendLine(content);
        sb.AppendLine("\\end{verbatim}");
    }

    private static string WrapPlain(string content)
    {
        var lines = content.Split('\n');
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            var trimmed = line.TrimEnd();
            if (trimmed.Length <= 100)
            {
                sb.AppendLine(trimmed);
                continue;
            }

            for (var i = 0; i < trimmed.Length; i += 100)
            {
                sb.AppendLine(trimmed.Substring(i, Math.Min(100, trimmed.Length - i)));
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendDocxHeading(Body body, string text, int sizeHalfPoints)
    {
        var run = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        run.RunProperties = new RunProperties(new Bold(), new FontSize { Val = sizeHalfPoints.ToString(CultureInfo.InvariantCulture) });
        body.Append(new Paragraph(run));
    }

    private static void AppendDocxParagraph(Body body, string text, bool bold = false)
    {
        var run = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        if (bold)
        {
            run.RunProperties = new RunProperties(new Bold());
        }

        body.Append(new Paragraph(run));
    }

    private static void AppendDocxSection(Body body, string title, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content == "-")
        {
            return;
        }

        AppendDocxHeading(body, title, 24);
        foreach (var line in content.Split('\n'))
        {
            AppendDocxParagraph(body, line);
        }
    }

    private static string BuildOdtContentXml(string title, string plain)
    {
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Indent = true,
            Encoding = new UTF8Encoding(false)
        };

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("office", "document-content", "urn:oasis:names:tc:opendocument:xmlns:office:1.0");
            writer.WriteAttributeString("xmlns", "text", null, "urn:oasis:names:tc:opendocument:xmlns:text:1.0");
            writer.WriteStartElement("office", "body", null);
            writer.WriteStartElement("office", "text", null);
            writer.WriteStartElement("text", "h", null);
            writer.WriteAttributeString("outline-level", "1");
            writer.WriteString(title);
            writer.WriteEndElement();
            foreach (var line in plain.Split('\n'))
            {
                writer.WriteStartElement("text", "p", null);
                writer.WriteString(line);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static string BuildOdtManifest() =>
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <manifest:manifest xmlns:manifest="urn:oasis:names:tc:opendocument:xmlns:manifest:1.0">
          <manifest:file-entry manifest:media-type="application/vnd.oasis.opendocument.text" manifest:full-path="/"/>
          <manifest:file-entry manifest:media-type="text/xml" manifest:full-path="content.xml"/>
        </manifest:manifest>
        """;

    private static void WriteZipEntry(ZipArchive archive, string name, string content, bool compress = true)
    {
        var entry = archive.CreateEntry(name, compress ? CompressionLevel.Optimal : CompressionLevel.NoCompression);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string EscapeHtml(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);

    private static string EscapeMd(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static string EscapeLatex(string value) =>
        value.Replace("\\", "\\textbackslash{}", StringComparison.Ordinal)
            .Replace("{", "\\{", StringComparison.Ordinal)
            .Replace("}", "\\}", StringComparison.Ordinal)
            .Replace("#", "\\#", StringComparison.Ordinal)
            .Replace("$", "\\$", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("&", "\\&", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("^", "\\^{}", StringComparison.Ordinal)
            .Replace("~", "\\~{}", StringComparison.Ordinal);
}
