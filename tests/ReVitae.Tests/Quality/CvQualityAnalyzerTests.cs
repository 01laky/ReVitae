using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Quality;
using CvCertificateEntry = ReVitae.Core.Cv.Certificates.CertificateEntry;
using CvEducationEntry = ReVitae.Core.Cv.Education.EducationEntry;
using CvLanguageEntry = ReVitae.Core.Cv.Languages.LanguageEntry;
using CvLinkEntry = ReVitae.Core.Cv.Links.LinkEntry;
using CvProjectEntry = ReVitae.Core.Cv.Projects.ProjectEntry;
using CvSkillsGroupEntry = ReVitae.Core.Cv.Skills.SkillsGroupEntry;
using CvWorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;

namespace ReVitae.Tests.Quality;

public sealed class CvQualityAnalyzerTests
{
    private static CvExportSourceData Snapshot(
        PersonalInformationImport? personal = null,
        IEnumerable<CvWorkExperienceEntry>? work = null,
        IEnumerable<CvEducationEntry>? education = null,
        IEnumerable<CvSkillsGroupEntry>? skills = null,
        IEnumerable<CvLanguageEntry>? languages = null,
        IEnumerable<CvCertificateEntry>? certificates = null,
        IEnumerable<CvProjectEntry>? projects = null,
        IEnumerable<CvLinkEntry>? links = null,
        string? additional = null) =>
        CvExportSourceDataFactory.Create(
            personal ?? new PersonalInformationImport(),
            work ?? [],
            education ?? [],
            skills ?? [],
            languages ?? [],
            certificates ?? [],
            projects ?? [],
            links ?? [],
            additional);

    [Fact]
    public void Analyze_EmptyCv_ReturnsNoHints()
    {
        var report = CvQualityAnalyzer.Analyze(Snapshot());

        Assert.Empty(report.Hints);
    }

    [Fact]
    public void Analyze_ShortSummary_ReturnsSummaryTooShortHint()
    {
        var personal = new PersonalInformationImport { ShortSummary = "Brief intro." };
        var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal));

        Assert.Contains(report.Hints, hint => hint.Id == CvQualityHintIds.PersonalSummaryTooShort);
    }

    [Fact]
    public void Analyze_MissingSummaryWithWork_ReturnsSummaryMissingHint()
    {
        var personal = new PersonalInformationImport { FirstName = "Jane" };
        var work = new[]
        {
            new CvWorkExperienceEntry
            {
                JobTitle = "Engineer",
                Company = "Acme",
                Description = "Built APIs."
            }
        };

        var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, work: work));

        Assert.Contains(report.Hints, hint => hint.Id == CvQualityHintIds.PersonalSummaryMissing);
    }

    [Fact]
    public void Analyze_GenericWorkDescription_ReturnsGenericHint()
    {
        var work = new[]
        {
            new CvWorkExperienceEntry
            {
                JobTitle = "Engineer",
                Company = "Acme",
                Description = "Responsible for various tasks and daily operations across the team."
            }
        };

        var report = CvQualityAnalyzer.Analyze(Snapshot(
            personal: new PersonalInformationImport { FirstName = "Jane" },
            work: work));

        Assert.Contains(report.Hints, hint => hint.Id == CvQualityHintIds.WorkGenericDescription);
    }

    [Fact]
    public void Analyze_MeasurableWorkDescription_DoesNotReturnGenericHint()
    {
        var work = new[]
        {
            new CvWorkExperienceEntry
            {
                JobTitle = "Engineer",
                Company = "Acme",
                Description = "Increased deployment frequency by 20% through CI automation."
            }
        };

        var report = CvQualityAnalyzer.Analyze(Snapshot(
            personal: new PersonalInformationImport { FirstName = "Jane" },
            work: work));

        Assert.DoesNotContain(report.Hints, hint => hint.Id == CvQualityHintIds.WorkGenericDescription);
    }

    [Fact]
    public void Analyze_WorkWithoutLanguages_ReturnsLanguagesEmptyHint()
    {
        var work = new[]
        {
            new CvWorkExperienceEntry { JobTitle = "Engineer", Company = "Acme", Description = "Built APIs." }
        };

        var report = CvQualityAnalyzer.Analyze(Snapshot(
            personal: new PersonalInformationImport { FirstName = "Jane" },
            work: work));

        Assert.Contains(report.Hints, hint => hint.Id == CvQualityHintIds.LanguagesSectionEmpty);
    }

    [Fact]
    public void Analyze_DuplicatePersonalLink_ReturnsDuplicateHint()
    {
        var personal = new PersonalInformationImport
        {
            FirstName = "Jane",
            LinkedInUrl = "https://linkedin.com/in/jane"
        };
        var links = new[]
        {
            new CvLinkEntry { Label = "LinkedIn", Url = "https://www.linkedin.com/in/jane/" }
        };

        var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, links: links));

        Assert.Contains(report.Hints, hint => hint.Id == CvQualityHintIds.LinksDuplicatePersonalUrl);
    }

    [Fact]
    public void Analyze_SectionEmptyRules_DoNotFireWithoutStartedCv()
    {
        var report = CvQualityAnalyzer.Analyze(Snapshot());

        Assert.DoesNotContain(report.Hints, hint => hint.Id == CvQualityHintIds.WorkSectionEmpty);
        Assert.DoesNotContain(report.Hints, hint => hint.Id == CvQualityHintIds.EducationSectionEmpty);
    }

    [Fact]
    public void Analyze_ImportLowConfidenceSection_ReturnsReviewSectionHint()
    {
        var personal = new PersonalInformationImport { FirstName = "Jane" };
        var work = new[]
        {
            new CvWorkExperienceEntry { JobTitle = "A", Company = "B", Description = "Did things." }
        };
        var confidences = new[]
        {
            new ImportedFieldConfidence(
                WorkExperienceFieldKeys.Build(work[0].Id, WorkExperienceFieldKeys.JobTitle),
                CvImportConfidence.Low),
            new ImportedFieldConfidence(
                WorkExperienceFieldKeys.Build(work[0].Id, WorkExperienceFieldKeys.Company),
                CvImportConfidence.Low)
        };

        var report = CvQualityAnalyzer.Analyze(
            Snapshot(personal: personal, work: work),
            new CvQualityAnalysisOptions(confidences));

        Assert.Contains(report.Hints, hint => hint.Id == CvQualityHintIds.ImportReviewSection);
    }

    [Fact]
    public void Analyze_DismissedHints_AreFilteredOut()
    {
        var personal = new PersonalInformationImport
        {
            FirstName = "Jane",
            ProfessionalTitle = string.Empty
        };
        var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal));
        var missingTitle = Assert.Single(report.Hints, hint => hint.Id == CvQualityHintIds.PersonalMissingTitle);
        var dismissKey = CvQualityAnalyzer.BuildDismissKey(missingTitle);

        var filtered = CvQualityAnalyzer.Analyze(
            Snapshot(personal: personal),
            new CvQualityAnalysisOptions(DismissedHintKeys: new HashSet<string> { dismissKey }));

        Assert.DoesNotContain(filtered.Hints, hint => hint.Id == CvQualityHintIds.PersonalMissingTitle);
    }
}
