using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Pdf;
using ReVitae.Core.Import;
using ReVitae.Core.Import.Pdf;
using ReVitae.Tests.Import.Fixtures.JohnDoe;
using UglyToad.PdfPig;

namespace ReVitae.Tests.Import.Pdf;

public sealed class ReVitaePdfExportHintsTests
{
    [Fact]
    public void Build_JohnDoeStressText_SetsLikelyReVitaeExport()
    {
        using var generated = JohnDoeVariantGenerator.Generate(JohnDoeVariantCatalog.All.First(spec => spec.Id == "19"));
        var text = File.ReadAllText(generated.Path);

        var hints = ReVitaePdfExportHintsBuilder.Build(null, false, text, false);

        Assert.True(hints.IsLikelyReVitaeExport);
    }

    [Fact]
    public void Build_GenericResumeSnippet_DoesNotSetReVitaeExport()
    {
        const string generic = """
            Jane Applicant
            jane@example.com
            Experience
            Developer at Example Corp
            Built web apps with C# and SQL.
            """;

        var hints = ReVitaePdfExportHintsBuilder.Build(null, false, generic, false);

        Assert.False(hints.IsLikelyReVitaeExport);
    }

    [Fact]
    public void Read_ExportedModernSidebarPdf_ReadsMetadataFingerprint()
    {
        var document = JohnDoeStressCvDataset.CreateDocument(CvExportTemplateId.ModernSidebar);
        var bytes = new QuestPdfCvExporter().Export(document);

        using var stream = new MemoryStream(bytes);
        using var pdf = PdfDocument.Open(stream);
        var (templateId, isReVitae) = ReVitaePdfMetadataReader.Read(pdf);

        Assert.True(isReVitae);
        Assert.Equal(CvExportTemplateId.ModernSidebar, templateId);
    }

    [Fact]
    public void Extract_ExportedPdf_ThreadsHintsThroughAdapter()
    {
        var path = Path.Combine(Path.GetTempPath(), $"revitae-hints-{Guid.NewGuid():N}.pdf");
        try
        {
            var document = JohnDoeStressCvDataset.CreateDocument(CvExportTemplateId.ClassicSidebar);
            File.WriteAllBytes(path, new QuestPdfCvExporter().Export(document));

            var extraction = new PdfTextExtractorAdapter(new PdfPigTextExtractor()).Extract(path);

            Assert.True(extraction.Success);
            Assert.NotNull(extraction.ReVitaeHints);
            Assert.True(extraction.ReVitaeHints.IsLikelyReVitaeExport);
            Assert.Equal(CvExportTemplateId.ClassicSidebar, extraction.ReVitaeHints.TemplateId);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void TryParseTemplateId_ParsesKeywordsToken()
    {
        var parsed = ReVitaePdfMetadataReader.TryParseTemplateId("template:ExecutiveBlueSidebar, cv");
        Assert.Equal(CvExportTemplateId.ExecutiveBlueSidebar, parsed);
    }
}
