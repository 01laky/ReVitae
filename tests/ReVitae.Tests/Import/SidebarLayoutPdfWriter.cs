using System.Globalization;
using System.Text;

namespace ReVitae.Tests.Import;

/// <summary>Builds minimal multi-page PDFs with a left sidebar and right main column for import tests.</summary>
internal static class SidebarLayoutPdfWriter
{
    private const double PageWidth = 595;
    private const double PageHeight = 842;
    private const double SidebarX = 48;
    public const double MainColumnX = 250;
    private const double MainX = MainColumnX;
    private const double LineHeight = 14;
    private const double TopY = 760;

    public sealed record PlacedLine(double X, double Y, string Text);

    public sealed record PageLayout(IReadOnlyList<PlacedLine> SidebarLines, IReadOnlyList<PlacedLine> MainLines);

    public sealed record HyperlinkAnnotation(double X, double Y, string Uri);

    public static byte[] Create(IReadOnlyList<PageLayout> pages) =>
        CreateWithHyperlinks(pages, []);

    public static byte[] CreateWithHyperlinks(
        IReadOnlyList<PageLayout> pages,
        IReadOnlyList<HyperlinkAnnotation> hyperlinks)
    {
        var pageCount = pages.Count;
        var fontObjectNumber = 3 + pageCount;
        var firstContentObjectNumber = fontObjectNumber + 1;
        var firstLinkObjectNumber = firstContentObjectNumber + pageCount;

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            BuildPagesObject(pageCount),
        };

        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            var contentObjectNumber = firstContentObjectNumber + pageIndex;
            var annots = hyperlinks.Count == 0
                ? string.Empty
                : $" /Annots [{string.Join(' ', Enumerable.Range(0, hyperlinks.Count).Select(index => $"{firstLinkObjectNumber + index} 0 R"))}]";
            objects.Add(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {ToPdfNumber(PageWidth)} {ToPdfNumber(PageHeight)}] " +
                $"/Resources << /Font << /F1 {fontObjectNumber} 0 R >> >> /Contents {contentObjectNumber} 0 R{annots} >>");
        }

        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        foreach (var layout in pages)
        {
            var stream = BuildContentStream(layout);
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream");
        }

        foreach (var link in hyperlinks)
        {
            objects.Add(
                $"<< /Type /Annot /Subtype /Link /Rect [{ToPdfNumber(link.X)} {ToPdfNumber(link.Y)} {ToPdfNumber(link.X + 120)} {ToPdfNumber(link.Y + 14)}] " +
                $"/Border [0 0 0] /A << /S /URI /URI ({EscapePdfText(link.Uri)}) >> >>");
        }

        return BuildPdf(objects);
    }

    public static IReadOnlyList<PageLayout> CreateDeferredSidebarStressLayout()
    {
        var pageOneMain = PlaceColumn(MainX, TopY,
        [
            "Jane Sidebar",
            "Profile",
            "Engineer focused on platform delivery.",
            "Work Experience",
            "Senior Developer",
            "Acme Corp · Berlin · Full-time · 01 / 2022 – 12 / 2023",
            "Built APIs and led delivery."
        ]);

        var pageOneSidebar = PlaceColumn(SidebarX, TopY,
        [
            "Contact",
            "Phone: +1 555 000 1111",
            "Email: jane.sidebar@example.com"
        ]);

        var pageTwoMain = PlaceColumn(MainX, TopY,
        [
            "Staff Engineer",
            "Globex Inc · Berlin · Contract · 03 / 2024 – Present",
            "Owned platform modernization.",
            "Education",
            "MSc Computer Science",
            "Example University · 2016 - 2018"
        ]);

        return
        [
            new PageLayout(pageOneSidebar, pageOneMain),
            new PageLayout([], pageTwoMain)
        ];
    }

    public static IReadOnlyList<PageLayout> CreateSinglePageTwoColumnLayout()
    {
        var main = PlaceColumn(MainX, TopY,
        [
            "Work Experience",
            "Lead Engineer",
            "Example Labs · 2020 - 2024"
        ]);

        var sidebar = PlaceColumn(SidebarX, TopY,
        [
            "Contact",
            "Email: lead@example.com"
        ]);

        return [new PageLayout(sidebar, main)];
    }

    private static string BuildPagesObject(int pageCount)
    {
        var kids = string.Join(' ', Enumerable.Range(0, pageCount).Select(index => $"{3 + index} 0 R"));
        return $"<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>";
    }

    private static IReadOnlyList<PlacedLine> PlaceColumn(double x, double startY, IReadOnlyList<string> lines)
    {
        var placed = new List<PlacedLine>();
        for (var index = 0; index < lines.Count; index++)
        {
            placed.Add(new PlacedLine(x, startY - (index * LineHeight), lines[index]));
        }

        return placed;
    }

    private static string BuildContentStream(PageLayout layout)
    {
        var builder = new StringBuilder();
        builder.AppendLine("BT");
        builder.AppendLine("/F1 11 Tf");

        foreach (var line in layout.MainLines.Concat(layout.SidebarLines).OrderByDescending(line => line.Y).ThenBy(line => line.X))
        {
            builder.AppendLine("1 0 0 1 " + ToPdfNumber(line.X) + " " + ToPdfNumber(line.Y) + " Tm");
            builder.Append('(');
            builder.Append(EscapePdfText(line.Text));
            builder.AppendLine(") Tj");
        }

        builder.AppendLine("ET");
        return builder.ToString();
    }

    private static string ToPdfNumber(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static byte[] BuildPdf(IReadOnlyList<string> objects)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
        var offsets = new List<long> { 0 };

        writer.WriteLine("%PDF-1.4");

        for (var index = 0; index < objects.Count; index++)
        {
            writer.Flush();
            offsets.Add(stream.Position);
            writer.WriteLine($"{index + 1} 0 obj");
            writer.WriteLine(objects[index]);
            writer.WriteLine("endobj");
        }

        writer.Flush();
        var xrefOffset = stream.Position;

        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Count + 1}");
        writer.WriteLine("0000000000 65535 f ");

        for (var index = 1; index < offsets.Count; index++)
        {
            writer.WriteLine($"{offsets[index]:D10} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefOffset);
        writer.WriteLine("%%EOF");
        writer.Flush();

        return stream.ToArray();
    }

    private static string EscapePdfText(string text) =>
        text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
}
