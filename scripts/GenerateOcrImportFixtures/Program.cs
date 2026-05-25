using System.Text;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var fixtureDirectory = Path.Combine(
    repoRoot,
    "tests",
    "ReVitae.Tests",
    "Import",
    "Fixtures",
    "Ocr");
Directory.CreateDirectory(fixtureDirectory);

var emptyPdfPath = Path.Combine(fixtureDirectory, "MinimalCvEmptyText.pdf");
await File.WriteAllBytesAsync(emptyPdfPath, MinimalPdfWriter.CreateFromLines([]));

var scanPngPath = Path.Combine(fixtureDirectory, "MinimalCvScan.png");
await using (var stream = File.Create(scanPngPath))
{
    CreateScanPng().SaveAsPng(stream);
}

Console.WriteLine($"Generated: {emptyPdfPath}");
Console.WriteLine($"Generated: {scanPngPath}");

static Image<Rgba32> CreateScanPng()
{
    const int width = 800;
    const int height = 1100;
    var image = new Image<Rgba32>(width, height, Color.White);
    var family = SystemFonts.Get("Arial");
    var font = family.CreateFont(28, FontStyle.Regular);
    var brush = Brushes.Solid(Color.Black);
    var lines =
        """
        Jane Doe
        jane@example.com
        +1 555 0100

        Summary
        Product designer with ten years of experience building digital products.

        Work Experience
        Senior Designer at Acme Corp
        01/2020 - Present

        Education
        BA Design, Example University
        2012 - 2016

        Skills
        Figma, Sketch, UX Research
        """.Split('\n');

    var y = 80f;
    foreach (var line in lines)
    {
        image.Mutate(ctx => ctx.DrawText(line, font, brush, new PointF(60, y)));
        y += 36;
    }

    return image;
}

internal static class MinimalPdfWriter
{
    public static byte[] CreateFromLines(IReadOnlyList<string> lines)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 11 Tf");
        content.AppendLine("72 760 Td");

        for (var index = 0; index < lines.Count; index++)
        {
            if (index > 0)
            {
                content.AppendLine("0 -14 Td");
            }

            content.Append('(');
            content.Append(EscapePdfText(lines[index] + '\n'));
            content.AppendLine(") Tj");
        }

        content.AppendLine("ET");

        var contentText = content.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(contentText)} >>\nstream\n{contentText}endstream"
        };

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
        var offsets = new List<long> { 0 };

        writer.WriteLine("%PDF-1.4");

        for (var index = 0; index < objects.Length; index++)
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
        writer.WriteLine($"0 {objects.Length + 1}");
        writer.WriteLine("0000000000 65535 f ");

        for (var index = 1; index < offsets.Count; index++)
        {
            writer.WriteLine($"{offsets[index]:D10} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Length + 1} /Root 1 0 R >>");
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
