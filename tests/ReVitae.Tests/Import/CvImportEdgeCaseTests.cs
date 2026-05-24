using ReVitae.Core.Cv.Education;
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
    public void Extract_ParsesWorkExperienceLocationBeforeDateLine()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Work Experience
            Excalibur s.r.o. - Senior full stack developer
            Kosice, Slovakia
            01/2024 - 05/2026
            Developed backend services in Go and Node.js.
            """;

        var result = Extract(text);
        var entry = Assert.Single(result.WorkExperienceEntries);

        Assert.Equal("Senior full stack developer", entry.JobTitle);
        Assert.Equal("Excalibur s.r.o.", entry.Company);
        Assert.Equal("Kosice, Slovakia", entry.Location);
        Assert.Equal(5, entry.EndMonth);
        Assert.Equal(2026, entry.EndYear);
        Assert.Contains("Developed backend services", entry.Description, StringComparison.Ordinal);
    }

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
    public void Extract_ParsesEducationWithLeadingGraduationDateLocationAndInstitution()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            06/2006
            Bratislava, Slovakia
            High School of Electrical Engineering
            """;

        var result = Extract(text);

        Assert.Single(result.EducationEntries);
        var education = result.EducationEntries[0];
        Assert.Equal("High School", education.Degree);
        Assert.Equal("High School of Electrical Engineering", education.Institution);
        Assert.Equal("Bratislava, Slovakia", education.Location);
        Assert.Equal(9, education.StartMonth);
        Assert.Equal(2002, education.StartYear);
        Assert.Equal(6, education.EndMonth);
        Assert.Equal(2006, education.EndYear);
    }

    [Fact]
    public void Extract_ParsesEducationWithDegreeInstitutionAndTrailingDateRange()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            BSc Computer Science
            Technical University
            09/2016 - 06/2020
            Graduated with honors.
            """;

        var result = Extract(text);

        Assert.Single(result.EducationEntries);
        var education = result.EducationEntries[0];
        Assert.Equal("BSc Computer Science", education.Degree);
        Assert.Equal("Technical University", education.Institution);
        Assert.Equal(9, education.StartMonth);
        Assert.Equal(2016, education.StartYear);
        Assert.Equal(6, education.EndMonth);
        Assert.Equal(2020, education.EndYear);
        Assert.Equal("Graduated with honors.", education.Description);
    }

    [Fact]
    public void Extract_MarksInferredEducationStartDatesAsLowConfidence()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            06/2006
            High School of Electrical Engineering
            """;

        var result = Extract(text);
        var education = Assert.Single(result.EducationEntries);

        Assert.Equal(9, education.StartMonth);
        Assert.Equal(2002, education.StartYear);
        Assert.Contains(
            result.FieldConfidences,
            confidence => confidence.FieldKey == EducationFieldKeys.Build(education.Id, EducationFieldKeys.StartMonth)
                && confidence.Confidence == CvImportConfidence.Low);
        Assert.Contains(
            result.FieldConfidences,
            confidence => confidence.FieldKey == EducationFieldKeys.Build(education.Id, EducationFieldKeys.StartYear)
                && confidence.Confidence == CvImportConfidence.Low);
    }

    [Fact]
    public void Extract_ParsesCertificateIssueDateOnThirdLine()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Certificates
            AWS Solutions Architect
            Amazon Web Services
            2023
            """;

        var result = Extract(text);

        Assert.Single(result.CertificateEntries);
        var certificate = result.CertificateEntries[0];
        Assert.Equal("AWS Solutions Architect", certificate.Name);
        Assert.Equal("Amazon Web Services", certificate.Issuer);
        Assert.Null(certificate.IssueMonth);
        Assert.Equal(2023, certificate.IssueYear);
    }

    [Fact]
    public void Extract_ParsesInlineCertificateHeaderWithIssuerAndDates()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Certificates
            Professional Certification #01 — Enterprise Platform Engineering · Global Tech Certification Board 1 · Feb 2021 · Valid until Feb 2024
            Credential ID: JD-CERT-0001-2021
            """;

        var result = Extract(text);

        var certificate = Assert.Single(result.CertificateEntries);
        Assert.Contains("Professional Certification #01", certificate.Name, StringComparison.Ordinal);
        Assert.Contains("Enterprise Platform Engineering", certificate.Name, StringComparison.Ordinal);
        Assert.Equal("Global Tech Certification Board 1", certificate.Issuer);
        Assert.Equal(2, certificate.IssueMonth);
        Assert.Equal(2021, certificate.IssueYear);
        Assert.Equal(2, certificate.ExpirationMonth);
        Assert.Equal(2024, certificate.ExpirationYear);
    }

    [Fact]
    public void Extract_ParsesProjectWithLeadingDateRange()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Projects
            01/2022 - 12/2022
            ReVitae
            Local-first CV builder.
            """;

        var result = Extract(text);

        Assert.Single(result.ProjectEntries);
        var project = result.ProjectEntries[0];
        Assert.Equal("ReVitae", project.Name);
        Assert.Equal(1, project.StartMonth);
        Assert.Equal(2022, project.StartYear);
        Assert.Equal(12, project.EndMonth);
        Assert.Equal(2022, project.EndYear);
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
            confidence.FieldKey == "firstName" && confidence.Confidence == CvImportConfidence.Medium);
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

    [Fact]
    public void Extract_ParsesContactLocationWithoutExplicitLabel()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Contact
            Turček, Slovakia 03848
            Slovakia / Remote / EU
            (+421) 944159982
            """;

        var result = Extract(text);

        Assert.Equal("Turček, Slovakia 03848", result.Personal.Location);
    }

    [Fact]
    public void Extract_ParsesLinkedInAndGitHubFromHyperlinkUrls()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Contact
            LinkedIn
            GitHub
            """;

        var result = CvImportFieldExtractor.Extract(
            CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text)),
            [
                "https://www.linkedin.com/in/jane-doe",
                "https://github.com/janedoe"
            ]);

        Assert.Equal("https://www.linkedin.com/in/jane-doe", result.Personal.LinkedInUrl);
        Assert.Equal("https://github.com/janedoe", result.Personal.GitHubUrl);
    }

    [Fact]
    public void Extract_FiltersSidebarSkillBleedFromWorkExperience()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            PostgreSQL
            Redis
            React
            AI Feature Integration
            Team leadership

            Work Experience
            Devcity s.r.o. - Senior full stack developer
            Prague, Czechia
            03/2023 - 01/2024
            Worked on web application development.
            PostgreSQL
            Redis
            React
            AI Feature Integration
            Team leadership
            merkatos.cz s.r.o. - Frontend development leader
            Prague, Czechia
            03/2022 - 12/2022
            ReactJS, TypeScript, .NET Core
            Project www.vinisto.cz
            """;

        var result = Extract(text);

        Assert.Equal(2, result.WorkExperienceEntries.Count);

        var devcity = result.WorkExperienceEntries[0];
        Assert.Contains("web application development", devcity.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PostgreSQL", devcity.Technologies, StringComparison.Ordinal);
        Assert.DoesNotContain("Team leadership", devcity.Description, StringComparison.Ordinal);

        var merkatos = result.WorkExperienceEntries[1];
        Assert.Equal("ReactJS, TypeScript, .NET Core", merkatos.Technologies);
        Assert.Equal("Project www.vinisto.cz", merkatos.Description);
    }

    [Fact]
    public void Extract_DoesNotTreatWorkDescriptionCommasAsTechnologies()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Work Experience
            Excalibur s.r.o. - Senior full stack developer
            Kosice, Slovakia
            01/2024 - 05/2026
            Contributed to software architecture decisions, API boundaries,
            and technical design for backend, frontend, and AI-assisted
            product features.
            Designed and iterated prompts for AI-driven functionality and
            reviewed AI outputs in the context of product behavior,
            correctness, and security.
            """;

        var result = Extract(text);
        var entry = Assert.Single(result.WorkExperienceEntries);

        Assert.Equal(string.Empty, entry.Technologies);
        Assert.Contains("software architecture decisions", entry.Description, StringComparison.Ordinal);
        Assert.Contains("correctness, and security", entry.Description, StringComparison.Ordinal);
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
        Assert.Equal(TranslationKeys.ImportErrorEmptyDocument, result.ErrorMessageKey);
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
