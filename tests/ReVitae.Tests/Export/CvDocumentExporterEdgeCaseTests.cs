using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Import.Structured;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Export;

public sealed class CvDocumentExporterEdgeCaseTests
{
    [Theory]
    [MemberData(nameof(ExportAllFormatsMemberData))]
    public void Export_AllShippedFormats_SucceedWithNonEmptyOutput(CvExportFormat format)
    {
        var document = CvExportTestFixtures.CreateRepresentativeDocument();
        var source = CvExportTestFixtures.CreateRepresentativeSourceData();

        using var stream = new MemoryStream();
        var result = CvDocumentExporter.Export(document, source, format, stream);

        Assert.True(result.Success);
        Assert.True(stream.Length > 0);
    }

    [Theory]
    [MemberData(nameof(DeterministicFormatsMemberData))]
    public void Export_IsDeterministic(CvExportFormat format)
    {
        var document = CvExportTestFixtures.CreateRepresentativeDocument();
        var source = CvExportTestFixtures.CreateRepresentativeSourceData();

        var first = ExportToBytes(document, source, format);
        var second = ExportToBytes(document, source, format);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Export_NullDocument_ReturnsFailure()
    {
        using var stream = new MemoryStream();
        var result = CvDocumentExporter.Export(null!, CvExportTestFixtures.CreatePersonalOnlySourceData(), CvExportFormat.Pdf, stream);
        Assert.False(result.Success);
    }

    [Fact]
    public void Export_NullStream_ReturnsFailure()
    {
        var document = CvExportTestFixtures.CreateRepresentativeDocument();
        var result = CvDocumentExporter.Export(document, CvExportTestFixtures.CreatePersonalOnlySourceData(), CvExportFormat.Pdf, null!);
        Assert.False(result.Success);
    }

    [Fact]
    public void Export_PersonalOnlyStructuredFormats_ProduceImportableContent()
    {
        var document = CvExportTestFixtures.CreatePersonalInfoOnlyDocument();
        var source = CvExportTestFixtures.CreatePersonalOnlySourceData();
        var bytes = ExportToBytes(document, source, CvExportFormat.RevitaeJson);
        var json = Encoding.UTF8.GetString(bytes);
        var import = ReVitaeJsonMapper.Map(json);
        Assert.True(import.Success);
        Assert.Equal("Jane", import.Personal.FirstName);
    }

    [Fact]
    public void Export_WithPhotoPath_DoesNotThrowForVisualFormats()
    {
        var tempDirectory = ProfilePhotoTestHelpers.CreateTempDirectory();
        try
        {
            var storage = new ProfilePhotoStorage(tempDirectory);
            var saved = storage.TrySaveCopy(ProfilePhotoTestHelpers.WriteMinimalPng(tempDirectory));
            Assert.True(saved.Success);

            var document = CvExportTestFixtures.CreateRepresentativeDocument() with { PhotoPath = saved.StoredPath };
            var source = CvExportTestFixtures.CreateRepresentativeSourceData();
            source.Personal.ProfilePhotoPath = saved.StoredPath!;

            foreach (var format in new[] { CvExportFormat.Pdf, CvExportFormat.Html, CvExportFormat.Docx })
            {
                using var stream = new MemoryStream();
                var result = CvDocumentExporter.Export(document, source, format, stream);
                Assert.True(result.Success, format.ToString());
                Assert.True(stream.Length > 0);
            }
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void Export_WithMissingPhotoPath_SucceedsWithoutThrowing()
    {
        var document = CvExportTestFixtures.CreateRepresentativeDocument() with
        {
            PhotoPath = Path.Combine(Path.GetTempPath(), "missing-profile-photo.png")
        };
        var source = CvExportTestFixtures.CreateRepresentativeSourceData();

        foreach (var format in new[] { CvExportFormat.Pdf, CvExportFormat.Html, CvExportFormat.Docx, CvExportFormat.RevitaeJson })
        {
            using var stream = new MemoryStream();
            var result = CvDocumentExporter.Export(document, source, format, stream);
            Assert.True(result.Success, format.ToString());
            Assert.True(stream.Length > 0);
        }
    }

    [Theory]
    [MemberData(nameof(ExportAllFormatsMemberData))]
    public void Export_AllShippedFormats_WithPhoto_SucceedWithNonEmptyOutput(CvExportFormat format)
    {
        var tempDirectory = ProfilePhotoTestHelpers.CreateTempDirectory();
        try
        {
            var storage = new ProfilePhotoStorage(tempDirectory);
            var saved = storage.TrySaveCopy(ProfilePhotoTestHelpers.WriteMinimalPng(tempDirectory));
            Assert.True(saved.Success);

            var document = CvExportTestFixtures.CreateRepresentativeDocument() with { PhotoPath = saved.StoredPath };
            var source = CvExportTestFixtures.CreateRepresentativeSourceData();
            source.Personal.ProfilePhotoPath = saved.StoredPath!;

            using var stream = new MemoryStream();
            var result = CvDocumentExporter.Export(document, source, format, stream);

            Assert.True(result.Success);
            Assert.True(stream.Length > 0);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    public static IEnumerable<object[]> ExportAllFormatsMemberData() =>
        CvExportTestFixtures.AllShippedFormats.Select(format => new object[] { format });

    public static IEnumerable<object[]> DeterministicFormatsMemberData() =>
        CvExportTestFixtures.AllShippedFormats
            .Where(format => format is not CvExportFormat.Docx
                and not CvExportFormat.Odt
                and not CvExportFormat.Pdf)
            .Select(format => new object[] { format });

    private static byte[] ExportToBytes(CvExportDocument document, CvExportSourceData source, CvExportFormat format)
    {
        using var stream = new MemoryStream();
        var result = CvDocumentExporter.Export(document, source, format, stream);
        Assert.True(result.Success);
        return stream.ToArray();
    }
}

public sealed class CvExportFormatCatalogEdgeCaseTests
{
    [Fact]
    public void GetEnabledFormats_ContainsAllSixteenShippedFormats()
    {
        var formats = CvExportFormatCatalog.GetEnabledFormats();
        Assert.Equal(16, formats.Count);
        Assert.Contains(formats, descriptor => descriptor.Format == CvExportFormat.Images);
        foreach (var shipped in CvExportTestFixtures.AllShippedFormats)
        {
            Assert.Contains(formats, descriptor => descriptor.Format == shipped);
        }
    }

    [Fact]
    public void Pdf_IsRecommended()
    {
        var pdf = CvExportFormatCatalog.Get(CvExportFormat.Pdf);
        Assert.True(pdf.IsRecommended);
    }

    [Theory]
    [MemberData(nameof(CatalogMemberData))]
    public void EveryEnabledFormat_HasNonEmptyIconSlugAndLabel(CvExportFormat format)
    {
        var descriptor = CvExportFormatCatalog.Get(format);
        Assert.True(descriptor.IsEnabled);
        Assert.False(string.IsNullOrWhiteSpace(descriptor.IconSlug));
        Assert.False(string.IsNullOrWhiteSpace(descriptor.LabelKey));
    }

    [Theory]
    [InlineData(CvExportFormatCategory.Documents, 4)]
    [InlineData(CvExportFormatCategory.WebAndText, 4)]
    [InlineData(CvExportFormatCategory.Images, 1)]
    [InlineData(CvExportFormatCategory.Structured, 7)]
    public void Categories_GroupFormats(CvExportFormatCategory category, int expectedCount)
    {
        var count = CvExportFormatCatalog.GetEnabledFormats().Count(f => f.Category == category);
        Assert.Equal(expectedCount, count);
    }

    public static IEnumerable<object[]> CatalogMemberData() =>
        CvExportTestFixtures.AllShippedFormats.Select(format => new object[] { format });
}

public sealed class CvExportSaveDialogDefaultsEdgeCaseTests
{
    [Theory]
    [InlineData(CvExportFormat.Pdf, "*.pdf", "application/pdf")]
    [InlineData(CvExportFormat.Docx, "*.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData(CvExportFormat.Odt, "*.odt", "application/vnd.oasis.opendocument.text")]
    [InlineData(CvExportFormat.Rtf, "*.rtf", "application/rtf")]
    [InlineData(CvExportFormat.Html, "*.html", "text/html")]
    [InlineData(CvExportFormat.Markdown, "*.md", "text/markdown")]
    [InlineData(CvExportFormat.Txt, "*.txt", "text/plain")]
    [InlineData(CvExportFormat.Latex, "*.tex", "application/x-tex")]
    [InlineData(CvExportFormat.RevitaeJson, "*.revitae.json", "application/json")]
    [InlineData(CvExportFormat.JsonResume, "*.json", "application/json")]
    [InlineData(CvExportFormat.Yaml, "*.yaml", "application/x-yaml")]
    [InlineData(CvExportFormat.EuropassXml, "*.xml", "application/xml")]
    [InlineData(CvExportFormat.HrXml, "*.xml", "application/xml")]
    [InlineData(CvExportFormat.Csv, "*.csv", "text/csv")]
    [InlineData(CvExportFormat.Tsv, "*.tsv", "text/tab-separated-values")]
    public void ForEachFormat_PatternsAndMimeTypesAreConfigured(CvExportFormat format, string expectedPattern, string expectedMime)
    {
        Assert.Contains(expectedPattern, CvExportSaveDialogDefaults.GetPatterns(format));
        Assert.Contains(expectedMime, CvExportSaveDialogDefaults.GetMimeTypes(format));
    }

    [Theory]
    [MemberData(nameof(LabelKeyMemberData))]
    public void FileTypeLabelKey_ResolvesViaLocalizer(CvExportFormat format)
    {
        var localizer = new AppLocalizer("en");
        var label = localizer.Get(CvExportSaveDialogDefaults.GetFileTypeLabelKey(format));
        Assert.False(string.IsNullOrWhiteSpace(label));
    }

    public static IEnumerable<object[]> LabelKeyMemberData() =>
        CvExportTestFixtures.AllShippedFormats.Select(format => new object[] { format });
}

public sealed class CvExportPathHelperEdgeCaseTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/does/not/exist/file.pdf")]
    public void IsExistingFile_ReturnsFalseForInvalidPaths(string? path)
    {
        Assert.False(CvExportPathHelper.IsExistingFile(path));
    }

    [Fact]
    public void IsExistingFile_ReturnsTrueForExistingTempFile()
    {
        var path = Path.GetTempFileName();
        try
        {
            Assert.True(CvExportPathHelper.IsExistingFile(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}

public sealed class CvExportFilenameHelperEdgeCaseTests
{
    [Theory]
    [InlineData(CvExportFormat.Pdf, ".pdf")]
    [InlineData(CvExportFormat.Docx, ".docx")]
    [InlineData(CvExportFormat.Odt, ".odt")]
    [InlineData(CvExportFormat.Rtf, ".rtf")]
    [InlineData(CvExportFormat.Html, ".html")]
    [InlineData(CvExportFormat.Markdown, ".md")]
    [InlineData(CvExportFormat.Txt, ".txt")]
    [InlineData(CvExportFormat.Latex, ".tex")]
    [InlineData(CvExportFormat.RevitaeJson, ".revitae.json")]
    [InlineData(CvExportFormat.JsonResume, ".json")]
    [InlineData(CvExportFormat.Yaml, ".yaml")]
    [InlineData(CvExportFormat.EuropassXml, "_europass.xml")]
    [InlineData(CvExportFormat.HrXml, "_hrxml.xml")]
    [InlineData(CvExportFormat.Csv, ".csv")]
    [InlineData(CvExportFormat.Tsv, ".tsv")]
    public void SuggestFilename_UsesCorrectExtensionPerFormat(CvExportFormat format, string expectedSuffix)
    {
        var filename = CvExportFilenameHelper.SuggestFilename("Jane", "Doe", format);
        Assert.EndsWith(expectedSuffix, filename);
        Assert.StartsWith("Jane_Doe_CV", filename);
    }
}

public sealed class VisualFormatExporterEdgeCaseTests
{
    [Theory]
    [InlineData(CvExportFormat.Html, "<html", "<body")]
    [InlineData(CvExportFormat.Markdown, "# ")]
    [InlineData(CvExportFormat.Txt, "Ladislav")]
    [InlineData(CvExportFormat.Rtf, @"{\rtf")]
    [InlineData(CvExportFormat.Latex, @"\documentclass")]
    public void VisualTextFormats_ContainExpectedMarkers(CvExportFormat format, params string[] markers)
    {
        var text = ExportAsText(format);
        foreach (var marker in markers)
        {
            Assert.Contains(marker, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Docx_IsValidOpenXmlPackage()
    {
        var bytes = ExportToBytes(CvExportFormat.Docx);
        using var stream = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        Assert.NotNull(doc.MainDocumentPart);
    }

    [Fact]
    public void Odt_IsValidZipWithContentXml()
    {
        var bytes = ExportToBytes(CvExportFormat.Odt);
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.NotNull(archive.GetEntry("content.xml"));
    }

    [Fact]
    public void Export_PreservesDiacriticsInHtml()
    {
        var text = ExportAsText(CvExportFormat.Html);
        Assert.Contains("Kostolný", text);
        Assert.Contains("Košice", text);
    }

    private static string ExportAsText(CvExportFormat format) =>
        Encoding.UTF8.GetString(ExportToBytes(format));

    private static byte[] ExportToBytes(CvExportFormat format)
    {
        using var stream = new MemoryStream();
        var result = CvDocumentExporter.Export(
            CvExportTestFixtures.CreateRepresentativeDocument(),
            CvExportTestFixtures.CreateRepresentativeSourceData(),
            format,
            stream);
        Assert.True(result.Success);
        return stream.ToArray();
    }
}

public sealed class StructuredFormatExporterEdgeCaseTests
{
    [Fact]
    public void RevitaeJson_ContainsVersionKey()
    {
        var json = ExportAsText(CvExportFormat.RevitaeJson);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("revitaeVersion", out _));
    }

    [Fact]
    public void JsonResume_ContainsSchema()
    {
        var json = ExportAsText(CvExportFormat.JsonResume);
        Assert.Contains("\"schema\"", json);
        Assert.Contains("jsonresume.org", json);
    }

    [Fact]
    public void Yaml_ContainsPersonalInformationSection()
    {
        var yaml = ExportAsText(CvExportFormat.Yaml);
        Assert.Contains("personalInformation", yaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EuropassXml_ContainsNamespace()
    {
        var xml = ExportAsText(CvExportFormat.EuropassXml);
        var document = XDocument.Parse(xml);
        Assert.Contains("europass", document.Root?.Name.NamespaceName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HrXml_ContainsResumeRoot()
    {
        var xml = ExportAsText(CvExportFormat.HrXml);
        var document = XDocument.Parse(xml);
        Assert.Contains("Resume", document.Root?.Name.LocalName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Csv_HasHeaderAndSingleDataRow()
    {
        var csv = ExportAsText(CvExportFormat.Csv);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("firstName", lines[0]);
        Assert.Contains("Ladislav", lines[1]);
    }

    [Fact]
    public void Tsv_UsesTabDelimiter()
    {
        var tsv = ExportAsText(CvExportFormat.Tsv);
        Assert.Contains('\t', tsv);
    }

    private static string ExportAsText(CvExportFormat format)
    {
        using var stream = new MemoryStream();
        CvDocumentExporter.Export(
            CvExportTestFixtures.CreateRepresentativeDocument(),
            CvExportTestFixtures.CreateRepresentativeSourceData(),
            format,
            stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

public sealed class ExportImportRoundTripTests
{
    [Fact]
    public void RevitaeJson_RoundTripsPersonalAndWorkExperience()
    {
        var bytes = Export(CvExportFormat.RevitaeJson);
        var result = ReVitaeJsonMapper.Map(Encoding.UTF8.GetString(bytes));
        Assert.True(result.Success);
        Assert.Equal("Ladislav", result.Personal.FirstName);
        Assert.NotEmpty(result.WorkExperienceEntries);
    }

    [Fact]
    public void JsonResume_RoundTripsBasics()
    {
        var bytes = Export(CvExportFormat.JsonResume);
        var result = JsonResumeMapper.Map(Encoding.UTF8.GetString(bytes));
        Assert.True(result.Success);
        Assert.Contains("Ladislav", result.Personal.FirstName);
    }

    [Fact]
    public void RevitaeJson_RoundTripsViaDocumentImporter()
    {
        var bytes = Export(CvExportFormat.RevitaeJson);
        var tempPath = Path.Combine(Path.GetTempPath(), $"revitae-export-{Guid.NewGuid():N}.revitae.json");
        File.WriteAllBytes(tempPath, bytes);
        try
        {
            var result = CvDocumentImporter.Import(tempPath);
            Assert.True(result.Success, result.ErrorMessageKey ?? "unknown");
            Assert.Equal("ladislav@example.com", result.Personal.Email);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Yaml_Export_QuotesScalarsForSafeParsing()
    {
        var yaml = Encoding.UTF8.GetString(Export(CvExportFormat.Yaml));
        Assert.Contains("revitaeVersion", yaml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"technologies\"", yaml, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] Export(CvExportFormat format)
    {
        using var stream = new MemoryStream();
        CvDocumentExporter.Export(
            CvExportTestFixtures.CreateRepresentativeDocument(),
            CvExportTestFixtures.CreateRepresentativeSourceData(),
            format,
            stream);
        return stream.ToArray();
    }
}
