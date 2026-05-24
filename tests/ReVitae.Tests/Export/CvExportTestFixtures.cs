using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ExportWorkExperienceEntry = ReVitae.Core.Export.WorkExperienceEntry;
using ExportEducationEntry = ReVitae.Core.Export.EducationEntry;
using ExportSkillItem = ReVitae.Core.Export.SkillItem;
using ExportSkillsGroup = ReVitae.Core.Export.SkillsGroup;
using ExportLanguageEntry = ReVitae.Core.Export.LanguageEntry;
using ExportCertificateEntry = ReVitae.Core.Export.CertificateEntry;
using ExportProjectEntry = ReVitae.Core.Export.ProjectEntry;

namespace ReVitae.Tests.Export;

internal static class CvExportTestFixtures
{
    public static CvExportSectionLabels CreateEnglishLabels(AppLocalizer localizer)
    {
        return new CvExportSectionLabels(
            Summary: localizer.Get(TranslationKeys.Summary),
            Contact: localizer.Get(TranslationKeys.Contact),
            Profile: localizer.Get(TranslationKeys.Profile),
            Objective: localizer.Get(TranslationKeys.Objective),
            PreviewWorkExperience: localizer.Get(TranslationKeys.PreviewWorkExperience),
            PreviewAchievements: localizer.Get(TranslationKeys.PreviewAchievements),
            PreviewTechnologies: localizer.Get(TranslationKeys.PreviewTechnologies),
            WorkExperienceCompanyUrl: localizer.Get(TranslationKeys.WorkExperienceCompanyUrl),
            PreviewEducation: localizer.Get(TranslationKeys.PreviewEducation),
            PreviewFieldOfStudy: localizer.Get(TranslationKeys.PreviewFieldOfStudy),
            PreviewGrade: localizer.Get(TranslationKeys.PreviewGrade),
            EducationInstitutionUrl: localizer.Get(TranslationKeys.EducationInstitutionUrl),
            PreviewSkills: localizer.Get(TranslationKeys.PreviewSkills),
            PreviewYearsSuffix: localizer.Get(TranslationKeys.PreviewYearsSuffix),
            PreviewLanguages: localizer.Get(TranslationKeys.PreviewLanguages),
            PreviewCertificates: localizer.Get(TranslationKeys.PreviewCertificates),
            PreviewProjects: localizer.Get(TranslationKeys.PreviewProjects),
            PreviewCustomLinks: localizer.Get(TranslationKeys.PreviewCustomLinks),
            PreviewAdditionalInformation: localizer.Get(TranslationKeys.PreviewAdditionalInformation),
            ContactLinks: localizer.Get(TranslationKeys.ContactLinks),
            Digital: localizer.Get(TranslationKeys.Digital),
            Links: localizer.Get(TranslationKeys.Links),
            Online: localizer.Get(TranslationKeys.Online),
            Email: localizer.Get(TranslationKeys.Email),
            Phone: localizer.Get(TranslationKeys.Phone),
            Location: localizer.Get(TranslationKeys.Location),
            ProfessionalTitle: localizer.Get(TranslationKeys.ProfessionalTitle),
            LinkedInUrl: localizer.Get(TranslationKeys.LinkedInUrl),
            PortfolioUrl: localizer.Get(TranslationKeys.PortfolioUrl),
            GitHubUrl: localizer.Get(TranslationKeys.GitHubUrl));
    }

    public static CvExportDocument CreateRepresentativeDocument(
        CvExportTemplateId templateId = CvExportTemplateId.CleanTopHeader,
        AppLocalizer? localizer = null)
    {
        localizer ??= new AppLocalizer("en");
        var labels = CreateEnglishLabels(localizer);

        return new CvExportDocument(
            templateId,
            labels,
            FirstName: "Ladislav",
            LastName: "Kostolný",
            ProfessionalTitle: "Software Engineer",
            Email: "ladislav@example.com",
            Phone: "+421 900 000 000",
            Location: "Košice",
            LinkedInUrl: "https://linkedin.com/in/ladislav",
            PortfolioUrl: "https://example.com",
            GitHubUrl: "https://github.com/ladislav",
            ShortSummary: "Experienced engineer building desktop products.",
            PhotoPath: null,
            WorkExperienceEntries:
            [
                new ExportWorkExperienceEntry(
                    "Senior Developer",
                    "Acme Corp",
                    "Košice",
                    "Full-time",
                    "2020 – súčasnosť",
                    "Built cross-platform desktop apps.",
                    "Shipped two major releases.",
                    "C#, Avalonia, .NET",
                    "https://acme.example")
            ],
            EducationEntries:
            [
                new ExportEducationEntry(
                    "BSc Computer Science",
                    "Technical University",
                    "Software Engineering",
                    "Košice",
                    "Bachelor's",
                    "2016 – 2020",
                    "1.0",
                    "Focused on systems programming.",
                    "https://school.example")
            ],
            SkillsGroups:
            [
                new ExportSkillsGroup(
                    "Programming",
                    [new ExportSkillItem("C#", "Advanced", 8)])
            ],
            LanguageEntries:
            [
                new ExportLanguageEntry("Slovak — Native", ["Reading: C2", "Writing: C2"])
            ],
            CertificateEntries:
            [
                new ExportCertificateEntry(
                    "AWS Solutions Architect",
                    ["Issued 2023", "Credential ID ABC-123"])
            ],
            ProjectEntries:
            [
                new ExportProjectEntry(
                    "ReVitae",
                    ["Technologies: .NET, Avalonia", "Highlights: Local-first CV builder"])
            ],
            CustomLinkLines: ["Blog — https://blog.example"],
            AdditionalInformationContent: "Open to remote roles across Europe.");
    }

    public static CvExportDocument CreatePersonalInfoOnlyDocument()
    {
        var localizer = new AppLocalizer("en");
        return new CvExportDocument(
            CvExportTemplateId.CleanTopHeader,
            CreateEnglishLabels(localizer),
            FirstName: "Jane",
            LastName: "Doe",
            ProfessionalTitle: "Designer",
            Email: "jane@example.com",
            Phone: "+1 555 0100",
            Location: "Berlin",
            LinkedInUrl: string.Empty,
            PortfolioUrl: string.Empty,
            GitHubUrl: string.Empty,
            ShortSummary: null,
            PhotoPath: null,
            WorkExperienceEntries: [],
            EducationEntries: [],
            SkillsGroups: [],
            LanguageEntries: [],
            CertificateEntries: [],
            ProjectEntries: [],
            CustomLinkLines: [],
            AdditionalInformationContent: null);
    }

    public static CvExportDocument CreateLongContentDocument()
    {
        var document = CreateRepresentativeDocument();
        var longParagraph = string.Join(
            Environment.NewLine,
            Enumerable.Repeat(
                "This paragraph repeats to force pagination across multiple A4 pages in the exported PDF.",
                80));

        return document with { AdditionalInformationContent = longParagraph };
    }

    public static CvExportSourceData CreateRepresentativeSourceData()
    {
        var personal = new PersonalInformationImport
        {
            FirstName = "Ladislav",
            LastName = "Kostolný",
            ProfessionalTitle = "Software Engineer",
            Email = "ladislav@example.com",
            Phone = "+421 900 000 000",
            Location = "Košice",
            LinkedInUrl = "https://linkedin.com/in/ladislav",
            PortfolioUrl = "https://example.com",
            GitHubUrl = "https://github.com/ladislav",
            ShortSummary = "Experienced engineer building desktop products."
        };

        var work = new ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry
        {
            JobTitle = "Senior Developer",
            Company = "Acme Corp",
            Location = "Košice",
            StartMonth = 1,
            StartYear = 2020,
            IsCurrentlyWorking = true,
            Description = "Built cross-platform desktop apps.",
            Achievements = "Shipped two major releases.",
            Technologies = "C#, Avalonia, .NET",
            CompanyUrl = "https://acme.example"
        };

        var education = new ReVitae.Core.Cv.Education.EducationEntry
        {
            Degree = "BSc Computer Science",
            Institution = "Technical University",
            FieldOfStudy = "Software Engineering",
            Location = "Košice",
            StartMonth = 9,
            StartYear = 2016,
            EndMonth = 6,
            EndYear = 2020,
            Grade = "1.0",
            Description = "Focused on systems programming.",
            InstitutionUrl = "https://school.example"
        };

        var skills = new SkillsGroupEntry { Category = "Programming" };
        skills.Skills.Add(new ReVitae.Core.Cv.Skills.SkillItem { Name = "C#", YearsOfExperience = 8 });

        var language = new ReVitae.Core.Cv.Languages.LanguageEntry { Language = "Slovak", Proficiency = LanguageProficiency.Native };
        var certificate = new ReVitae.Core.Cv.Certificates.CertificateEntry { Name = "AWS Solutions Architect", Issuer = "Amazon", IssueYear = 2023 };
        var project = new ReVitae.Core.Cv.Projects.ProjectEntry
        {
            Name = "ReVitae",
            Description = "Local-first CV builder",
            Highlights = "Technologies: .NET, Avalonia"
        };
        var link = new LinkEntry { Label = "Blog", Url = "https://blog.example" };

        return new CvExportSourceData(
            personal,
            [work],
            [education],
            [skills],
            [language],
            [certificate],
            [project],
            [link],
            "Open to remote roles across Europe.");
    }

    public static CvExportSourceData CreatePersonalOnlySourceData()
    {
        return new CvExportSourceData(
            new PersonalInformationImport
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@example.com"
            },
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            null);
    }

    public static IEnumerable<CvExportFormat> AllShippedFormats =>
        Enum.GetValues<CvExportFormat>();
}
