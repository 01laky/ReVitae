using ReVitae.Core.Import;
using ReVitae.Core.Import.Patterns;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import;

public sealed class CvTextNormalizerTests
{
    [Fact]
    public void Normalize_CollapsesCarriageReturnsAndBlankLines()
    {
        var result = CvTextNormalizer.Normalize("Line one\r\n\r\n\r\nLine two");

        Assert.Equal("Line one\n\nLine two", result);
    }

    [Fact]
    public void Normalize_NormalizesBulletPrefixes()
    {
        var result = CvTextNormalizer.Normalize("• Item one\n* Item two");

        Assert.Contains("- Item one", result, StringComparison.Ordinal);
        Assert.Contains("- Item two", result, StringComparison.Ordinal);
    }
}

public sealed class CvSectionSegmenterTests
{
    [Fact]
    public void Segment_DetectsWorkExperienceAndEducationHeaders()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Summary
            Product designer.

            Work Experience
            Designer at Acme
            2020 - 2024

            Education
            BA Design
            """;

        var result = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));

        Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.Summary));
        Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.WorkExperience));
        Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.Education));
    }

    [Fact]
    public void Segment_AddsWarningWhenNoHeadersFound()
    {
        var result = CvSectionSegmenter.Segment("Just some free text without headers.");

        Assert.Contains(result.Warnings, warning => warning.MessageKey == TranslationKeys.ImportWarningNoSectionsDetected);
    }
}

public sealed class DateRangeParserTests
{
    [Theory]
    [InlineData("01/2020 - 03/2024", 1, 2020, 3, 2024, false)]
    [InlineData("2020 - Present", null, 2020, null, null, true)]
    public void TryParse_ParsesCommonFormats(
        string input,
        int? startMonth,
        int startYear,
        int? endMonth,
        int? endYear,
        bool isPresent)
    {
        var parsed = DateRangeParser.TryParse(input, out var range);

        Assert.True(parsed);
        Assert.Equal(startMonth, range.StartMonth);
        Assert.Equal(startYear, range.StartYear);
        Assert.Equal(endMonth, range.EndMonth);
        Assert.Equal(endYear, range.EndYear);
        Assert.Equal(isPresent, range.IsPresent);
    }
}

public sealed class CvImportFieldExtractorTests
{
    [Fact]
    public void Extract_ParsesEmailAndLinkedInFromHeader()
    {
        const string text = """
            Jane Doe
            Frontend Developer
            jane.doe@example.com
            https://www.linkedin.com/in/janedoe

            Work Experience
            Developer at Acme
            2020 - 2024
            """;

        var segmentation = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));
        var result = CvImportFieldExtractor.Extract(segmentation);

        Assert.Equal("jane.doe@example.com", result.Personal.Email);
        Assert.Contains("linkedin.com", result.Personal.LinkedInUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Single(result.WorkExperienceEntries);
    }

    [Fact]
    public void Extract_ParsesSkillsCommaList()
    {
        const string text = """
            Name
            email@test.com

            Skills
            C#, .NET, SQL
            """;

        var segmentation = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));
        var result = CvImportFieldExtractor.Extract(segmentation);

        Assert.Single(result.SkillsGroups);
        Assert.Equal(3, result.SkillsGroups[0].Skills.Count);
    }

    [Fact]
    public void Extract_SkipsDuplicatePersonalUrlsInLinks()
    {
        const string text = """
            Jane Doe
            jane@example.com
            https://github.com/janedoe

            Links
            https://github.com/janedoe
            https://behance.net/jane
            """;

        var segmentation = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));
        var result = CvImportFieldExtractor.Extract(segmentation);

        Assert.Single(result.LinkEntries);
        Assert.Contains(result.Warnings, warning => warning.MessageKey == TranslationKeys.ImportWarningPersonalLinksDuplicatedSkipped);
    }
}

public sealed class CvPdfImporterTests
{
    public CvPdfImporterTests()
    {
        ImportPdfFixtureFactory.EnsureFixturesExist();
    }

    [Fact]
    public void ImportFromText_ParsesFixtureLikeEnglishCv()
    {
        var text = string.Join('\n', ImportPdfFixtureFactory.EnglishBasicLines);
        var importer = new CvPdfImporter();

        var result = importer.ImportFromText(text);

        Assert.True(result.Success);
        Assert.Equal("jane.doe@example.com", result.Personal.Email);
        Assert.NotEmpty(result.WorkExperienceEntries);
        Assert.True(result.SectionHasData[CvImportSectionId.WorkExperience]);
    }

    [Fact]
    public void ImportFromText_ParsesTwoColumnExtractedCvText()
    {
        const string text = """
            Ladislav
            Kostolný
            Senior Full Stack Developer
            C Contact
            Turček, Slovakia 03848
            Slovakia / Remote / EU
            (+421) 944159982
            01laky@gmail.com
            S Skills
            Node.js
            NestJS
            Go
            .NET Core

            P Personal Summary
            Senior Full Stack Developer with 12+ years of experience building web systems.
            E Experience
            Excalibur s.r.o. - Senior full stack developer
            Kosice, Slovakia
            01/2024 - 05/2026
            Developed backend services.
            Devcity s.r.o. - Senior full stack developer
            Prague, Czechia
            03/2023 - 01/2024
            Worked on web application development.
            merkatos.cz s.r.o. - Frontend development leader
            Prague, Czechia
            03/2022 - 12/2022
            ReactJS, TypeScript, .NET Core
            Project www.vinisto.cz
            D Education and Training
            06/2006
            High School of Electrical Engineering
            """;
        var importer = new CvPdfImporter();

        var result = importer.ImportFromText(text);

        Assert.True(result.Success);
        Assert.Equal("Ladislav", result.Personal.FirstName);
        Assert.Equal("Kostolný", result.Personal.LastName);
        Assert.Equal("Senior Full Stack Developer", result.Personal.ProfessionalTitle);
        Assert.Equal("01laky@gmail.com", result.Personal.Email);
        Assert.Equal("(+421) 944159982", result.Personal.Phone);
        Assert.Equal(3, result.WorkExperienceEntries.Count);

        var excalibur = result.WorkExperienceEntries[0];
        Assert.Equal("Senior full stack developer", excalibur.JobTitle);
        Assert.Equal("Excalibur s.r.o.", excalibur.Company);
        Assert.Equal("Kosice, Slovakia", excalibur.Location);
        Assert.Equal(1, excalibur.StartMonth);
        Assert.Equal(2024, excalibur.StartYear);
        Assert.Equal(5, excalibur.EndMonth);
        Assert.Equal(2026, excalibur.EndYear);
        Assert.Contains("Developed backend services", excalibur.Description, StringComparison.Ordinal);

        var devcity = result.WorkExperienceEntries[1];
        Assert.Equal("Prague, Czechia", devcity.Location);
        Assert.Equal(3, devcity.StartMonth);
        Assert.Equal(2023, devcity.StartYear);
        Assert.Equal(1, devcity.EndMonth);
        Assert.Equal(2024, devcity.EndYear);
        Assert.Contains("web application development", devcity.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PostgreSQL", devcity.Description, StringComparison.Ordinal);

        var merkatos = result.WorkExperienceEntries[2];
        Assert.Equal("ReactJS, TypeScript, .NET Core", merkatos.Technologies);
        Assert.Equal("Project www.vinisto.cz", merkatos.Description);
    }

    [Fact]
    public void ImportFromPdf_ParsesEnglishBasicFixture()
    {
        var importer = new CvPdfImporter();
        var path = ImportPdfFixtureFactory.GetFixturePath("sample-cv-en-basic.pdf");

        var result = importer.ImportFromPdf(path);

        Assert.True(result.Success);
        Assert.Equal("jane.doe@example.com", result.Personal.Email);
        Assert.NotEmpty(result.WorkExperienceEntries);
    }

    [Fact]
    public void ImportFromPdf_ParsesSlovakBasicFixture()
    {
        var importer = new CvPdfImporter();
        var path = ImportPdfFixtureFactory.GetFixturePath("sample-cv-sk-basic.pdf");

        var result = importer.ImportFromPdf(path);

        Assert.True(result.Success);
        Assert.Equal("peter.novak@example.com", result.Personal.Email);
        Assert.NotEmpty(result.LanguageEntries);
    }

    [Fact]
    public void ImportFromPdf_ParsesMessyFixtureWithLowConfidenceFields()
    {
        var importer = new CvPdfImporter();
        var path = ImportPdfFixtureFactory.GetFixturePath("sample-cv-en-messy.pdf");

        var result = importer.ImportFromPdf(path);

        Assert.True(result.Success);
        Assert.Equal("alex@example.com", result.Personal.Email);
        Assert.NotEmpty(result.FieldConfidences);
    }
}

public sealed class PdfPigTextExtractorTests
{
    public PdfPigTextExtractorTests()
    {
        ImportPdfFixtureFactory.EnsureFixturesExist();
    }

    [Fact]
    public void Extract_ReturnsNonEmptyTextForEnglishBasicFixture()
    {
        var extractor = new ReVitae.Core.Import.Pdf.PdfPigTextExtractor();
        var path = ImportPdfFixtureFactory.GetFixturePath("sample-cv-en-basic.pdf");

        var result = extractor.Extract(path);

        Assert.True(result.Success);
        Assert.Equal(1, result.PageCount);
        Assert.Contains("jane.doe@example.com", result.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Work Experience", result.Text, StringComparison.Ordinal);
    }
}
