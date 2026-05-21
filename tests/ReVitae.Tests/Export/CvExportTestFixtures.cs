using ReVitae.Core.Export;
using ReVitae.Core.Localization;

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
                new WorkExperienceEntry(
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
                new EducationEntry(
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
                new SkillsGroup(
                    "Programming",
                    [new SkillItem("C#", "Advanced", 8)])
            ],
            LanguageEntries:
            [
                new LanguageEntry("Slovak — Native", ["Reading: C2", "Writing: C2"])
            ],
            CertificateEntries:
            [
                new CertificateEntry(
                    "AWS Solutions Architect",
                    ["Issued 2023", "Credential ID ABC-123"])
            ],
            ProjectEntries:
            [
                new ProjectEntry(
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
}
