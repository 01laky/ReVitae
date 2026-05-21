using ReVitae.Core.Import;
using ReVitae.Core.Import.Patterns;
using ReVitae.Core.Import.Pdf;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import;

public sealed class CvTextNormalizerEdgeCaseTests
{
    [Fact]
    public void Normalize_ConvertsUnicodeDashesToHyphen()
    {
        var result = CvTextNormalizer.Normalize("2020 – 2024\nA — B\nC − D");

        Assert.Contains("2020 - 2024", result, StringComparison.Ordinal);
        Assert.Contains("A - B", result, StringComparison.Ordinal);
        Assert.Contains("C - D", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Normalize_CollapsesInlineWhitespace()
    {
        var result = CvTextNormalizer.Normalize("React      TypeScript\t\tNode.js");

        Assert.Equal("React TypeScript Node.js", result);
    }

    [Theory]
    [InlineData("◦ Item")]
    [InlineData("▪ Item")]
    [InlineData("* Item")]
    [InlineData("- Item")]
    public void Normalize_NormalizesCommonBulletVariants(string input)
    {
        var result = CvTextNormalizer.Normalize(input);

        Assert.Equal("- Item", result);
    }
}

public sealed class CvSectionSegmenterEdgeCaseTests
{
    [Fact]
    public void Segment_DetectsSlovakHeaders()
    {
        const string text = """
            Ján Novák
            jan@example.com

            Profil
            Backend developer.

            Pracovne skusenosti
            Developer at Acme

            Vzdelanie
            STU Bratislava
            """;

        var result = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));

        Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.Summary));
        Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.WorkExperience));
        Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.Education));
    }

    [Fact]
    public void Segment_DetectsContactSection()
    {
        const string text = """
            Jane Doe

            Contact
            jane@example.com
            +421 900 000 000
            """;

        var result = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));

        Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.Contact));
    }

    [Fact]
    public void Segment_MergesDuplicateSectionIds()
    {
        const string text = """
            Jane Doe

            Skills
            C#, SQL

            Technical Skills
            React, TypeScript
            """;

        var result = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));

        Assert.Single(result.SectionBodies, section => section.Key == CvImportSectionId.Skills);
        Assert.Contains("C#, SQL", result.SectionBodies[CvImportSectionId.Skills], StringComparison.Ordinal);
        Assert.Contains("React, TypeScript", result.SectionBodies[CvImportSectionId.Skills], StringComparison.Ordinal);
    }

    [Fact]
    public void Segment_DoesNotTreatExperienceInsideSentenceAsHeader()
    {
        const string text = """
            Jane Doe

            Summary
            Senior developer with 12 years of experience in web systems.
            """;

        var result = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));

        Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.Summary));
        Assert.False(result.SectionBodies.ContainsKey(CvImportSectionId.WorkExperience));
    }
}

public sealed class DateRangeParserEdgeCaseTests
{
    [Theory]
    [InlineData("Jan 2020 - Mar 2024", 1, 2020, 3, 2024, false)]
    [InlineData("2020 - 2024", null, 2020, null, 2024, false)]
    [InlineData("2020 - sucasnost", null, 2020, null, null, true)]
    [InlineData("2020 - current", null, 2020, null, null, true)]
    public void TryParse_ParsesAdditionalCvDateFormats(
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

    [Fact]
    public void TryParse_ReturnsFalseForNonDateLine()
    {
        Assert.False(DateRangeParser.TryParse("Senior developer at Acme", out _));
    }
}

public sealed class CvImportFieldExtractorEdgeCaseTests
{
    [Fact]
    public void Extract_ParsesWorkExperienceWithBulletsTechnologiesAndDateLineBelowHeader()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Work Experience
            Senior Developer at Acme
            Jan 2020 - Mar 2024
            - Built platform APIs
            - Improved CI pipeline
            Technologies: C#, .NET, SQL
            """;

        var result = Extract(text);
        var entry = Assert.Single(result.WorkExperienceEntries);

        Assert.Equal("Senior Developer", entry.JobTitle);
        Assert.Equal("Acme", entry.Company);
        Assert.Equal(1, entry.StartMonth);
        Assert.Equal(2020, entry.StartYear);
        Assert.Equal(3, entry.EndMonth);
        Assert.Equal(2024, entry.EndYear);
        Assert.Contains("Built platform APIs", entry.Achievements, StringComparison.Ordinal);
        Assert.Equal("C#, .NET, SQL", entry.Technologies);
    }

    [Fact]
    public void Extract_ParsesSkillsCategoryLineAndBulletList()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            Programming: C#, Go
            - Git
            - Docker
            """;

        var result = Extract(text);

        Assert.Equal(2, result.SkillsGroups.Count);
        Assert.Equal("Programming", result.SkillsGroups[0].Category);
        Assert.Equal(["C#", "Go"], result.SkillsGroups[0].Skills.Select(skill => skill.Name).ToArray());
        Assert.Contains(result.SkillsGroups, group => group.Skills.Any(skill => skill.Name == "Git"));
    }

    [Fact]
    public void Extract_ParsesLanguagesWithProficiencyAndCefr()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Languages
            English - Fluent
            German - B2
            """;

        var result = Extract(text);

        Assert.Equal(2, result.LanguageEntries.Count);
        Assert.Equal("English", result.LanguageEntries[0].Language);
        Assert.Equal(ReVitae.Core.Cv.Languages.LanguageProficiency.Fluent, result.LanguageEntries[0].Proficiency);
        Assert.Equal(ReVitae.Core.Cv.Languages.CefrLevel.B2, result.LanguageEntries[1].CefrLevel);
    }

    [Fact]
    public void Extract_ParsesCertificatesAndProjects()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Certificates
            AWS Certified Developer
            Amazon Web Services
            Credential note

            Projects
            Portfolio Website
            2022 - 2024
            https://example.dev
            Tech: React, TypeScript
            Built a personal portfolio.
            """;

        var result = Extract(text);

        var certificate = Assert.Single(result.CertificateEntries);
        Assert.Equal("AWS Certified Developer", certificate.Name);
        Assert.Equal("Amazon Web Services", certificate.Issuer);

        var project = Assert.Single(result.ProjectEntries);
        Assert.Equal("Portfolio Website", project.Name);
        Assert.Equal("https://example.dev", project.ProjectUrl);
        Assert.Equal(["React", "TypeScript"], project.Technologies.Select(technology => technology.Name).ToArray());
    }

    [Fact]
    public void Extract_EmitsExpectedConfidenceLevelsAndSectionHasData()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Contact
            Location: Bratislava, Slovakia

            Summary
            Developer summary.

            Work Experience
            Developer at Acme
            2020 - 2024
            Built systems.
            """;

        var result = Extract(text);

        Assert.True(result.SectionHasData[CvImportSectionId.PersonalInformation]);
        Assert.True(result.SectionHasData[CvImportSectionId.WorkExperience]);
        Assert.False(result.SectionHasData[CvImportSectionId.Education]);
        Assert.Contains(result.FieldConfidences, confidence =>
            confidence.FieldKey == "email" && confidence.Confidence == CvImportConfidence.High);
        Assert.Contains(result.FieldConfidences, confidence =>
            confidence.FieldKey == "firstName" && confidence.Confidence == CvImportConfidence.Low);
    }

    [Fact]
    public void Extract_EmitsNameUncertainWarningForSingleTokenName()
    {
        const string text = """
            Jane
            jane@example.com

            Skills
            C#
            """;

        var result = Extract(text);

        Assert.Contains(result.Warnings, warning => warning.MessageKey == TranslationKeys.ImportWarningNameUncertain);
    }

    private static CvImportResult Extract(string text)
    {
        return CvImportFieldExtractor.Extract(CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text)));
    }
}

public sealed class CvPdfImporterEdgeCaseTests
{
    [Fact]
    public void ImportFromText_ReturnsEmptyPdfErrorForWhitespaceOnlyInput()
    {
        var result = new CvPdfImporter().ImportFromText(" \n \t ");

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorEmptyPdf, result.ErrorMessageKey);
    }

    [Fact]
    public void ImportFromPdf_PropagatesExtractorFailure()
    {
        var importer = new CvPdfImporter(new FailingExtractor(TranslationKeys.ImportErrorUnreadablePdf));

        var result = importer.ImportFromPdf("/tmp/fake.pdf");

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorUnreadablePdf, result.ErrorMessageKey);
    }

    private sealed class FailingExtractor(string errorKey) : IPdfTextExtractor
    {
        public PdfTextExtractionResult Extract(string filePath)
        {
            return new PdfTextExtractionResult(false, string.Empty, 0, errorKey);
        }
    }
}

public sealed class PdfPigTextExtractorEdgeCaseTests
{
    [Fact]
    public void Extract_ReturnsFileNotFoundForMissingPath()
    {
        var result = new PdfPigTextExtractor().Extract(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf"));

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorFileNotFound, result.ErrorMessageKey);
    }

    [Fact]
    public void Extract_ReturnsUnreadableForCorruptPdf()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
        File.WriteAllText(path, "not a valid pdf");

        try
        {
            var result = new PdfPigTextExtractor().Extract(path);

            Assert.False(result.Success);
            Assert.Equal(TranslationKeys.ImportErrorUnreadablePdf, result.ErrorMessageKey);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
