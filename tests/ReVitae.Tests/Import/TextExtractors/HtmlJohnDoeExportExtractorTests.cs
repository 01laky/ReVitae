using ReVitae.Core.Import;
using ReVitae.Core.Import.Extraction;
using ReVitae.Tests.Import.Fixtures.JohnDoe;

namespace ReVitae.Tests.Import.TextExtractors;

public sealed class HtmlJohnDoeExportExtractorTests
{
    [Fact]
    public void Extract_ReVitaeHtmlExport_PreservesWorkSectionLineBreaks()
    {
        var spec = JohnDoeVariantCatalog.All.First(item => item.Id == "29");
        using var generated = JohnDoeVariantGenerator.Generate(spec);
        var extracted = new HtmlTextExtractor().Extract(generated.Path);
        Assert.True(extracted.Success);

        var workHeaderIndex = extracted.Text.IndexOf("Work Experience", StringComparison.OrdinalIgnoreCase);
        Assert.True(workHeaderIndex >= 0, "Work Experience header missing from extracted HTML text.");

        var following = extracted.Text[workHeaderIndex..];
        var newlineCount = following.Take(2_000).Count(ch => ch == '\n');
        Assert.True(newlineCount >= 20, $"Expected multiline work section, found {newlineCount} newlines near header.");

        var normalized = CvTextNormalizer.Normalize(extracted.Text);
        var workLines = normalized
            .Split('\n')
            .Where(line => line.Contains("Work Experience", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToArray();
        Assert.True(
            workLines.Any(line => line.Trim().Equals("Work Experience", StringComparison.OrdinalIgnoreCase)),
            $"Work Experience must appear as its own header line. Sample lines: {string.Join(" | ", workLines.Select(line => $"[{line.Length}] {line[..Math.Min(line.Length, 80)]}"))}");

        var segmentation = CvSectionSegmenter.Segment(normalized);
        Assert.True(
            segmentation.SectionBodies.TryGetValue(CvImportSectionId.WorkExperience, out var workBody),
            "Work experience section body missing after HTML segmentation.");
        Assert.True(workBody.Length > 100, $"Work section too short: {workBody.Length} chars.");

        var result = CvDocumentImporter.Import(generated.Path);
        Assert.True(result.Success);
        Assert.True(result.WorkExperienceEntries.Count >= 10, $"Expected work entries, got {result.WorkExperienceEntries.Count}.");
    }
}
