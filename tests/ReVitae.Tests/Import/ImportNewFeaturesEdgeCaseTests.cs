using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Import;
using ReVitae.Core.Import.Pdf;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import;

public sealed class ImportLanguageProficiencyEdgeCaseTests
{
    [Fact]
    public void Extract_ReVitaeLanguageExport_ParsesTwelveLanguagesNotSkillBreakdownLines()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Languages
            English - Native
            Reading: C2
            Writing: C2
            Speaking: C2
            Listening: C2
            Slovak - Native
            Reading: C2
            Writing: C2
            Speaking: C2
            Listening: C2
            German - Fluent
            Reading: C1
            Writing: B2
            Speaking: C1
            Listening: C1
            French - Advanced
            Reading: B2
            Writing: B2
            Speaking: B1
            Listening: B2
            Spanish - Intermediate
            Reading: B1
            Writing: B1
            Speaking: B1
            Listening: B1
            Italian - Intermediate
            Reading: B1
            Writing: A2
            Speaking: B1
            Listening: B1
            Czech - Advanced
            Reading: C1
            Writing: B2
            Speaking: C1
            Listening: C1
            Polish - Intermediate
            Reading: B1
            Writing: A2
            Speaking: B1
            Listening: B1
            Portuguese - Beginner
            Reading: A2
            Writing: A2
            Speaking: A1
            Listening: A2
            Japanese - Beginner
            Reading: A1
            Writing: A1
            Speaking: A1
            Listening: A1
            Mandarin Chinese - Beginner
            Reading: A1
            Writing: A1
            Speaking: A1
            Listening: A1
            Latin - Academic
            Reading: B1
            Writing: A2
            Translation: B1
            """;

        var result = ImportTestHelpers.Extract(text);

        Assert.Equal(12, result.LanguageEntries.Count);
        Assert.Contains(result.LanguageEntries, entry => entry.Language.Equals("English", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.LanguageEntries, entry => entry.Language.Equals("Latin", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.LanguageEntries, entry => entry.Language.StartsWith("Reading", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.LanguageEntries, entry => entry.Language.StartsWith("Writing", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.LanguageEntries, entry => entry.Language.StartsWith("Translation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Extract_SimpleLanguageDashProficiencyFormat_StillWorks()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Languages
            Slovak - Native
            English - Fluent
            German - Intermediate
            """;

        var result = ImportTestHelpers.Extract(text);

        Assert.Equal(3, result.LanguageEntries.Count);
        Assert.Equal(LanguageProficiency.Native, result.LanguageEntries[0].Proficiency);
        Assert.Equal(LanguageProficiency.Fluent, result.LanguageEntries[1].Proficiency);
        Assert.Equal(LanguageProficiency.Intermediate, result.LanguageEntries[2].Proficiency);
    }

    [Fact]
    public void Extract_LanguageSectionWithOnlySubLines_DoesNotCreateEntries()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Languages
            Reading: C2
            Writing: B2
            Speaking: B1
            Listening: B1
            """;

        var result = ImportTestHelpers.Extract(text);

        Assert.Empty(result.LanguageEntries);
    }
}

public sealed class ImportNameFromOtherSectionsEdgeCaseTests
{
    [Fact]
    public void Extract_FindsPersonNameBuriedInWorkExperienceSection()
    {
        const string text = """
            Skills
            Senior developer with many years of experience.
            jane@example.com

            Work Experience
            full stack developer
            Example s.r.o. · Kosice · Full-time · 01 / 2024 - 05 / 2026
            Delivered backend modules.
            Ladislav Kostolný
            Senior Full Stack Developer with 12+ years of experience.

            Education
            High School
            """;

        var result = ImportTestHelpers.Extract(text);

        Assert.Equal("Ladislav", result.Personal.FirstName);
        Assert.Equal("Kostolný", result.Personal.LastName);
    }

    [Fact]
    public void Extract_FindsPersonNameInContactSectionWhenHeaderStartsWithSkills()
    {
        const string text = """
            Skills
            Summary prose without a person name.
            jane@example.com

            Work Experience
            Developer
            Acme · 2020 - 2024

            Contact
            Maria Nováková
            Email: maria@example.com
            """;

        var result = ImportTestHelpers.Extract(text);

        Assert.Equal("Maria", result.Personal.FirstName);
        Assert.Equal("Nováková", result.Personal.LastName);
        Assert.Equal("maria@example.com", result.Personal.Email);
    }

    [Fact]
    public void Extract_DoesNotPromoteJobTitleFromWorkSectionToPersonName()
    {
        const string text = """
            Skills
            Platform engineer with 10+ years of experience.
            jane@example.com

            Work Experience
            Principal Software Engineer
            Nimbus Cloud Systems · 03 / 2025 - Present
            Owned architecture decisions.

            Education
            MSc Computer Science
            """;

        var result = ImportTestHelpers.Extract(text);

        Assert.False(string.Equals("Principal", result.Personal.FirstName, StringComparison.OrdinalIgnoreCase));
        Assert.False(string.Equals("Software", result.Personal.LastName, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class PdfPigDeferredSidebarEdgeCaseTests
{
    [Fact]
    public void Extract_DeferredSidebarPdf_ContactAppearsAfterAllMainPages()
    {
        using var temp = new TempImportDirectory();
        var path = temp.FilePath("deferred-sidebar.pdf", SidebarLayoutPdfWriter.Create(
            SidebarLayoutPdfWriter.CreateDeferredSidebarStressLayout()));

        var result = new PdfPigTextExtractor().Extract(path);

        Assert.True(result.Success);
        Assert.Equal(2, result.PageCount);
        Assert.Contains("Work Experience", result.Text, StringComparison.Ordinal);

        var workIndex = result.Text.IndexOf("Work Experience", StringComparison.Ordinal);
        var contactIndex = result.Text.IndexOf("Contact", StringComparison.Ordinal);
        var staffIndex = result.Text.IndexOf("Staff Engineer", StringComparison.Ordinal);
        var educationIndex = result.Text.IndexOf("Education", StringComparison.Ordinal);

        Assert.True(workIndex >= 0);
        Assert.True(staffIndex > workIndex);
        Assert.True(educationIndex > staffIndex);
        Assert.True(contactIndex > educationIndex);
    }

    [Fact]
    public void Import_DeferredSidebarPdf_ParsesPersonalInfoAndAtLeastOneWorkEntry()
    {
        using var temp = new TempImportDirectory();
        var path = temp.FilePath("deferred-sidebar-import.pdf", SidebarLayoutPdfWriter.Create(
            SidebarLayoutPdfWriter.CreateDeferredSidebarStressLayout()));

        var result = CvDocumentImporter.Import(path);

        Assert.True(result.Success, result.ErrorMessageKey);
        Assert.Equal("Jane", result.Personal.FirstName);
        Assert.Equal("Sidebar", result.Personal.LastName);
        Assert.Equal("jane.sidebar@example.com", result.Personal.Email);
        Assert.True(result.WorkExperienceEntries.Count >= 1);
        Assert.Equal("Senior Developer", result.WorkExperienceEntries[0].JobTitle);
        Assert.True(result.SectionHasData[CvImportSectionId.WorkExperience]);
        Assert.True(result.SectionHasData[CvImportSectionId.Education]);
    }

    [Fact]
    public void Extract_DeferredSidebarPdf_WorkExperienceSectionIsNotCutByEarlyContactHeader()
    {
        using var temp = new TempImportDirectory();
        var path = temp.FilePath("deferred-sidebar-segment.pdf", SidebarLayoutPdfWriter.Create(
            SidebarLayoutPdfWriter.CreateDeferredSidebarStressLayout()));

        var text = new PdfPigTextExtractor().Extract(path).Text;
        var segmentation = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));

        Assert.True(segmentation.SectionBodies.TryGetValue(CvImportSectionId.WorkExperience, out var workBody));
        Assert.Contains("Senior Developer", workBody, StringComparison.Ordinal);
        Assert.Contains("Staff Engineer", workBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Phone:", workBody, StringComparison.Ordinal);

        Assert.True(segmentation.SectionBodies.TryGetValue(CvImportSectionId.Contact, out var contactBody));
        Assert.Contains("jane.sidebar@example.com", contactBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Staff Engineer", contactBody, StringComparison.Ordinal);
        Assert.True(contactBody.Length < 200);
    }

    [Fact]
    public void Extract_SinglePageTwoColumnPdf_KeepsWorkExperienceHeaderIntact()
    {
        using var temp = new TempImportDirectory();
        var path = temp.FilePath("single-two-column.pdf", SidebarLayoutPdfWriter.Create(
            SidebarLayoutPdfWriter.CreateSinglePageTwoColumnLayout()));

        var result = new PdfPigTextExtractor().Extract(path);

        Assert.True(result.Success);
        Assert.Contains("Work Experience", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Work\nExperience", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Import_ReVitaeExportedSidebarPdf_ContactIsDeferredAfterMainPages()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Import", "Fixtures", "ReVitaeExportedSidebarCv.pdf");
        if (!File.Exists(path))
        {
            return;
        }

        var text = new PdfPigTextExtractor().Extract(path).Text;
        var workIndex = text.IndexOf("Work Experience", StringComparison.Ordinal);
        var contactIndex = text.LastIndexOf("Contact", StringComparison.Ordinal);
        var educationIndex = text.IndexOf("Education", StringComparison.Ordinal);

        Assert.True(workIndex >= 0);
        Assert.True(educationIndex > workIndex);
        Assert.True(contactIndex > educationIndex);
    }
}

public sealed class CvImportDiagnosticsLoggerEdgeCaseTests
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ReVitae",
        "import-debug.log");

    [Fact]
    public void Import_WritesDebugLogContainingPipelineStages()
    {
        var previous = Environment.GetEnvironmentVariable("REVITAE_IMPORT_DEBUG");
        Environment.SetEnvironmentVariable("REVITAE_IMPORT_DEBUG", "1");

        using var temp = new TempImportDirectory();
        var path = temp.FilePath("debug-log.pdf", SidebarLayoutPdfWriter.Create(
            SidebarLayoutPdfWriter.CreateSinglePageTwoColumnLayout()));
        var marker = Guid.NewGuid().ToString("N");
        var snapshot = File.Exists(LogPath) ? File.GetAttributes(LogPath) : FileAttributes.Normal;
        if (File.Exists(LogPath))
        {
            File.WriteAllText(LogPath, $"marker:{marker}\n");
        }

        try
        {
            var result = CvDocumentImporter.Import(path);

            Assert.True(result.Success, result.ErrorMessageKey);
            Assert.True(File.Exists(LogPath));

            var log = File.ReadAllText(LogPath);
            Assert.StartsWith($"marker:{marker}", log, StringComparison.Ordinal);
            Assert.Contains("--- 1. Text extraction ---", log, StringComparison.Ordinal);
            Assert.Contains("--- 2. Normalization ---", log, StringComparison.Ordinal);
            Assert.Contains("--- 3. Section segmentation ---", log, StringComparison.Ordinal);
            Assert.Contains("--- 4. Parsed result ---", log, StringComparison.Ordinal);
            Assert.Contains("Import finished", log, StringComparison.Ordinal);
            Assert.Contains(path, log, StringComparison.Ordinal);
            Assert.Contains("Work experience (1 entries):", log, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("REVITAE_IMPORT_DEBUG", previous);
            if (File.Exists(LogPath))
            {
                File.SetAttributes(LogPath, snapshot);
            }
        }
    }

    [Fact]
    public void Import_SkipsDebugLogAppendWhenEnvVarDisabled()
    {
        var previous = Environment.GetEnvironmentVariable("REVITAE_IMPORT_DEBUG");
        Environment.SetEnvironmentVariable("REVITAE_IMPORT_DEBUG", "0");

        using var temp = new TempImportDirectory();
        var path = temp.FilePath("debug-disabled.pdf", MinimalPdfWriter.CreateFromLines(["Jane Doe", "jane@example.com"]));
        var marker = Guid.NewGuid().ToString("N");
        File.WriteAllText(LogPath, $"marker:{marker}\n");

        try
        {
            var result = CvDocumentImporter.Import(path);

            Assert.True(result.Success, result.ErrorMessageKey);
            Assert.Equal($"marker:{marker}", File.ReadAllText(LogPath).TrimEnd());
        }
        finally
        {
            Environment.SetEnvironmentVariable("REVITAE_IMPORT_DEBUG", previous);
        }
    }
}

public sealed class JohnDoeStressPdfImportEdgeCaseTests
{
    [Fact]
    public void Import_JohnDoeStressPdf_ParsesCoreCountsWhenFixturePresent()
    {
        var path = ResolveJohnDoeStressPdfPath();
        if (path is null)
        {
            return;
        }

        var result = CvDocumentImporter.Import(path);

        Assert.True(result.Success, result.ErrorMessageKey);
        Assert.Equal("John", result.Personal.FirstName);
        Assert.Equal("Doe", result.Personal.LastName);
        Assert.Equal("john.doe@example.com", result.Personal.Email);
        Assert.Equal(20, result.WorkExperienceEntries.Count);
        Assert.Equal(12, result.LanguageEntries.Count);
        Assert.True(result.SkillsGroups.Sum(group => group.Skills.Count) >= 80);
    }

    [Fact]
    public void Extract_JohnDoeStressPdf_DeferredSidebarKeepsWorkSectionIntactWhenFixturePresent()
    {
        var path = ResolveJohnDoeStressPdfPath();
        if (path is null)
        {
            return;
        }

        var text = new PdfPigTextExtractor().Extract(path).Text;
        var segmentation = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));

        Assert.True(segmentation.SectionBodies.TryGetValue(CvImportSectionId.WorkExperience, out var workBody));
        Assert.True(workBody.Length > 10_000);
        Assert.Contains("Principal Software Engineer", workBody, StringComparison.Ordinal);
        Assert.Contains("Staff Full Stack Developer", workBody, StringComparison.Ordinal);

        Assert.True(segmentation.SectionBodies.TryGetValue(CvImportSectionId.Contact, out var contactBody));
        Assert.True(contactBody.Length < 500);
        Assert.DoesNotContain("Staff Full Stack Developer", contactBody, StringComparison.Ordinal);
    }

    private static string? ResolveJohnDoeStressPdfPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Import", "Fixtures", "JohnDoeStressCv.pdf"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "John Doe.pdf"))
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}

internal static class ImportTestHelpers
{
    public static CvImportResult Extract(string text)
    {
        return CvImportFieldExtractor.Extract(CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text)));
    }
}
