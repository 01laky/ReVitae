using System.Text;
using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;

namespace ReVitae.Tests.Import.Fixtures.JohnDoe;

internal static class JohnDoeTextVariantRenderer
{
    public static string Render(CvExportDocument document, JohnDoeTextFormattingProfile profile)
    {
        var text = profile switch
        {
            JohnDoeTextFormattingProfile.DefaultReVitae => RenderDefaultReVitae(document, contactFirst: true),
            JohnDoeTextFormattingProfile.DeferredContactAtEnd => RenderDefaultReVitae(document, contactFirst: false),
            JohnDoeTextFormattingProfile.SplitLinkedInUrl => ApplySplitLinkedIn(RenderDefaultReVitae(document, contactFirst: false)),
            JohnDoeTextFormattingProfile.SplitProfessionalTitle => ApplySplitProfessionalTitle(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.DigitalBlockEnd => RenderDigitalBlockEnd(document),
            JohnDoeTextFormattingProfile.WorkMetaSplitDates => ApplySplitWorkMetaDates(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.EducationMetaSplitDates => ApplySplitEducationMetaDates(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.SkillsSplitProficiency => ApplySplitSkillProficiency(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.SingleNewlineCertificatesProjects => RenderSingleNewlineCertificatesProjects(document),
            JohnDoeTextFormattingProfile.UnicodeNbspLocation => ApplyNbspLocation(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.WorkAtCompany => RenderWorkAtCompany(document),
            JohnDoeTextFormattingProfile.WorkDashCompany => RenderWorkDashCompany(document),
            JohnDoeTextFormattingProfile.SkillsColonCategories => RenderSkillsColonCategories(document),
            JohnDoeTextFormattingProfile.SkillsBulletList => RenderSkillsBulletList(document),
            JohnDoeTextFormattingProfile.EducationDegreeFirstBlankLine => RenderEducationDegreeFirstBlankLine(document),
            JohnDoeTextFormattingProfile.EducationDateFirst => RenderEducationDateFirst(document),
            JohnDoeTextFormattingProfile.MarkdownHeadings => RenderMarkdownHeadings(document),
            JohnDoeTextFormattingProfile.CrlfEmDashSlovakLabels => ApplyCrlfEmDashSlovak(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.CertificateIssuedSplitLine => ApplySplitCertificateIssuedLines(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.CertificateValidThroughLabel => ApplyValidThroughLabel(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.CertificateInlineMainLine => RenderCertificateInlineMainLine(document),
            JohnDoeTextFormattingProfile.CertificateMmmYyyyDates => ApplyCertificateMmmYyyyDates(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.WorkPresentOnOwnLine => ApplyWorkPresentOnOwnLine(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.SummaryAtEnd => RenderSummaryAtEnd(document),
            JohnDoeTextFormattingProfile.CertificatesBeforeEducation => RenderCertificatesBeforeEducation(document),
            JohnDoeTextFormattingProfile.UppercaseSectionHeaders => ApplyUppercaseSectionHeaders(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.TabIndentedContent => ApplyTabIndentedContent(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.SingleNewlineWorkEntries => ApplySingleNewlineWorkEntries(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.ProjectLabeledFields => RenderProjectLabeledFields(document),
            JohnDoeTextFormattingProfile.EducationInstitutionFirst => RenderEducationInstitutionFirst(document),
            JohnDoeTextFormattingProfile.SkillsPipeSeparated => RenderSkillsPipeSeparated(document),
            JohnDoeTextFormattingProfile.LinksWithBullets => RenderLinksWithBullets(document),
            JohnDoeTextFormattingProfile.PhoneParenthesesFormat => ApplyPhoneParenthesesFormat(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.CertificateCredentialUrlLine => ApplyCertificateCredentialUrlLines(RenderDefaultReVitae(document, contactFirst: true)),
            JohnDoeTextFormattingProfile.WorkContractEmploymentType => RenderWorkContractEmploymentType(document),
            JohnDoeTextFormattingProfile.MixedColonHeaders => ApplyMixedColonHeaders(RenderDefaultReVitae(document, contactFirst: true)),
            _ => RenderDefaultReVitae(document, contactFirst: true)
        };

        return text.TrimEnd();
    }

    private static string RenderDefaultReVitae(CvExportDocument document, bool contactFirst)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();

        var contact = BuildContactSection(document);
        var digital = BuildDigitalSection(document);

        if (contactFirst)
        {
            AppendSection(sb, document.Labels.Contact, contact);
            AppendSection(sb, document.Labels.Digital, digital);
        }

        AppendSection(sb, document.Labels.Profile, CvExportPreviewContentBuilder.BuildSummary(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AppendSection(sb, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewProjects, CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewCustomLinks, CvExportPreviewContentBuilder.BuildCustomLinksPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewAdditionalInformation, document.AdditionalInformationContent ?? string.Empty);

        if (!contactFirst)
        {
            AppendSection(sb, document.Labels.Digital, digital);
            AppendSection(sb, document.Labels.Contact, contact);
        }

        return sb.ToString();
    }

    private static string RenderDigitalBlockEnd(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();
        AppendSection(sb, document.Labels.Profile, CvExportPreviewContentBuilder.BuildSummary(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AppendSection(sb, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewProjects, CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewCustomLinks, CvExportPreviewContentBuilder.BuildCustomLinksPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewAdditionalInformation, document.AdditionalInformationContent ?? string.Empty);
        AppendSection(sb, document.Labels.Digital, BuildDigitalSection(document));
        AppendSection(sb, document.Labels.Contact, BuildContactSection(document));
        return sb.ToString();
    }

    private static string RenderSingleNewlineCertificatesProjects(CvExportDocument document)
    {
        var text = RenderDefaultReVitae(document, contactFirst: true);
        var certContent = CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document);
        var projectContent = CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document);
        if (!string.IsNullOrWhiteSpace(certContent))
        {
            text = text.Replace(
                certContent,
                string.Join('\n', certContent.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
        }

        if (!string.IsNullOrWhiteSpace(projectContent))
        {
            text = text.Replace(
                projectContent,
                string.Join('\n', projectContent.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
        }

        return text;
    }

    private static string RenderWorkAtCompany(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();
        AppendSection(sb, document.Labels.Contact, BuildContactSection(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, BuildWorkSection(document, entry => $"{entry.JobTitle} at {entry.Company}"));
        AppendRemainingSections(sb, document, includeWork: false);
        return sb.ToString();
    }

    private static string RenderWorkDashCompany(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();
        AppendSection(sb, document.Labels.Contact, BuildContactSection(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, BuildWorkSection(document, entry => $"{entry.Company} - {entry.JobTitle}"));
        AppendRemainingSections(sb, document, includeWork: false);
        return sb.ToString();
    }

    private static string RenderSkillsColonCategories(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();
        AppendSection(sb, document.Labels.Contact, BuildContactSection(document));
        AppendSection(sb, document.Labels.Digital, BuildDigitalSection(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AppendSection(sb, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));

        var skills = new StringBuilder();
        foreach (var group in document.SkillsGroups)
        {
            skills.AppendLine($"{group.Category}: {string.Join(", ", group.Skills.Select(skill => skill.Name))}");
        }

        AppendSection(sb, document.Labels.PreviewSkills, skills.ToString().TrimEnd());
        AppendRemainingSections(sb, document, includeWork: false, includeEducation: false, includeSkills: false);
        return sb.ToString();
    }

    private static string RenderSkillsBulletList(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();
        AppendSection(sb, document.Labels.Contact, BuildContactSection(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AppendSection(sb, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));

        var skills = new StringBuilder();
        skills.AppendLine("General");
        foreach (var skill in document.SkillsGroups.SelectMany(group => group.Skills))
        {
            skills.AppendLine($"- {skill.Name}");
        }

        AppendSection(sb, document.Labels.PreviewSkills, skills.ToString().TrimEnd());
        AppendRemainingSections(sb, document, includeWork: false, includeEducation: false, includeSkills: false);
        return sb.ToString();
    }

    private static string RenderEducationDegreeFirstBlankLine(CvExportDocument document) =>
        RenderDefaultReVitae(document, contactFirst: true);

    private static string RenderEducationDateFirst(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();
        AppendSection(sb, document.Labels.Contact, BuildContactSection(document));
        AppendSection(sb, document.Labels.Digital, BuildDigitalSection(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));

        var education = new StringBuilder();
        foreach (var entry in document.EducationEntries)
        {
            education.AppendLine(entry.DateRange);
            if (!string.IsNullOrWhiteSpace(entry.Location))
            {
                education.AppendLine(entry.Location);
            }

            education.AppendLine(entry.Degree);
            education.AppendLine(entry.Institution);
            education.AppendLine();
        }

        AppendSection(sb, document.Labels.PreviewEducation, education.ToString().TrimEnd());
        AppendRemainingSections(sb, document, includeWork: false, includeEducation: false);
        return sb.ToString();
    }

    private static string RenderMarkdownHeadings(CvExportDocument document)
    {
        var plain = RenderDefaultReVitae(document, contactFirst: true);
        var labels = document.Labels;
        return plain
            .Replace($"{labels.Contact}{Environment.NewLine}", $"## {labels.Contact}{Environment.NewLine}", StringComparison.Ordinal)
            .Replace($"{labels.PreviewWorkExperience}{Environment.NewLine}", $"## {labels.PreviewWorkExperience}{Environment.NewLine}", StringComparison.Ordinal)
            .Replace($"{labels.PreviewEducation}{Environment.NewLine}", $"## {labels.PreviewEducation}{Environment.NewLine}", StringComparison.Ordinal)
            .Replace($"{labels.PreviewSkills}{Environment.NewLine}", $"## {labels.PreviewSkills}{Environment.NewLine}", StringComparison.Ordinal)
            .Replace($"{labels.PreviewLanguages}{Environment.NewLine}", $"## {labels.PreviewLanguages}{Environment.NewLine}", StringComparison.Ordinal)
            .Replace($"{labels.PreviewCertificates}{Environment.NewLine}", $"## {labels.PreviewCertificates}{Environment.NewLine}", StringComparison.Ordinal)
            .Replace($"{labels.PreviewProjects}{Environment.NewLine}", $"## {labels.PreviewProjects}{Environment.NewLine}", StringComparison.Ordinal)
            .Replace($"{labels.PreviewCustomLinks}{Environment.NewLine}", $"## {labels.PreviewCustomLinks}{Environment.NewLine}", StringComparison.Ordinal)
            .Replace($"{labels.PreviewAdditionalInformation}{Environment.NewLine}", $"## {labels.PreviewAdditionalInformation}{Environment.NewLine}", StringComparison.Ordinal);
    }

    private static string BuildWorkSection(CvExportDocument document, Func<WorkExperienceEntry, string> titleLineFactory)
    {
        var entries = new List<string>();
        foreach (var entry in document.WorkExperienceEntries)
        {
            var block = new List<string>
            {
                titleLineFactory(entry),
                $"{entry.Location} · {entry.EmploymentTypeLabel} · {entry.DateRange}"
            };

            if (!string.IsNullOrWhiteSpace(entry.Description))
            {
                block.Add(entry.Description);
            }

            entries.Add(string.Join(Environment.NewLine, block));
        }

        return string.Join($"{Environment.NewLine}{Environment.NewLine}", entries);
    }

    private static void AppendRemainingSections(
        StringBuilder sb,
        CvExportDocument document,
        bool includeWork = true,
        bool includeEducation = true,
        bool includeSkills = true,
        bool includeCertificates = true,
        bool includeProjects = true,
        bool includeLinks = true)
    {
        if (includeWork)
        {
            AppendSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        }

        if (includeEducation)
        {
            AppendSection(sb, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));
        }

        if (includeSkills)
        {
            AppendSection(sb, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
        }

        AppendSection(sb, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document));
        if (includeCertificates)
        {
            AppendSection(sb, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document));
        }

        if (includeProjects)
        {
            AppendSection(sb, document.Labels.PreviewProjects, CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document));
        }

        if (includeLinks)
        {
            AppendSection(sb, document.Labels.PreviewCustomLinks, CvExportPreviewContentBuilder.BuildCustomLinksPreviewContent(document));
        }

        AppendSection(sb, document.Labels.PreviewAdditionalInformation, document.AdditionalInformationContent ?? string.Empty);
    }

    private static string BuildContactSection(CvExportDocument document) =>
        CvExportPreviewContentBuilder.BuildLines(
            document.Labels.ProfessionalTitle, document.ProfessionalTitle,
            document.Labels.Phone, document.Phone,
            document.Labels.Email, document.Email,
            document.Labels.Location, document.Location,
            document.Labels.LinkedInUrl, document.LinkedInUrl);

    private static string BuildDigitalSection(CvExportDocument document) =>
        CvExportPreviewContentBuilder.BuildDigitalLines(document);

    private static void AppendSection(StringBuilder sb, string title, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        sb.AppendLine(title);
        sb.AppendLine(body.TrimEnd());
        sb.AppendLine();
    }

    private static string ApplySplitLinkedIn(string text)
    {
        const string linkedIn = "https://www.linkedin.com/in/john-doe-architect";
        return text.Replace(
            $"{JohnDoeStressCvDataset.CreateDocument().Labels.LinkedInUrl}: {linkedIn}",
            string.Join(
                Environment.NewLine,
                [
                    $"{JohnDoeStressCvDataset.CreateDocument().Labels.LinkedInUrl}: https://",
                    "www.linkedin.com/in/john-doe-",
                    "architect"
                ]),
            StringComparison.Ordinal);
    }

    private static string ApplySplitProfessionalTitle(string text)
    {
        const string title = "Senior Full Stack Software Architect & Engineering Leader";
        return text.Replace(
            $"{JohnDoeStressCvDataset.CreateDocument().Labels.ProfessionalTitle}: {title}",
            $"{JohnDoeStressCvDataset.CreateDocument().Labels.ProfessionalTitle}: Senior Full Stack Software Architect &{Environment.NewLine}Engineering Leader",
            StringComparison.Ordinal);
    }

    private static string ApplySplitWorkMetaDates(string text)
    {
        return text.Replace(
            " · 03 / 2025 – Present",
            $" · 03 / 2025 –{Environment.NewLine}Present",
            StringComparison.Ordinal);
    }

    private static string ApplySplitEducationMetaDates(string text)
    {
        return text.Replace(
            "Massachusetts Institute of Technology · Cambridge, MA, USA · Master's · 09 / 2000 – 06 / 2002",
            "Massachusetts Institute of Technology · Cambridge, MA, USA · Master's ·" + Environment.NewLine + "09 / 2000 - 06 / 2002",
            StringComparison.Ordinal);
    }

    private static string ApplySplitSkillProficiency(string text) =>
        text.Replace("C# · Expert · 18 yrs", $"C#{Environment.NewLine}Expert · 18 yrs", StringComparison.Ordinal);

    private static string ApplyNbspLocation(string text) =>
        text.Replace("San Francisco, CA 94107, United States", "San Francisco,\u00A0CA\u00A094107,\u00A0United\u00A0States", StringComparison.Ordinal);

    private static string ApplyCrlfEmDashSlovak(string text)
    {
        var localized = text
            .Replace("Contact", "Kontakt", StringComparison.Ordinal)
            .Replace("Work Experience", "Pracovné skúsenosti", StringComparison.Ordinal)
            .Replace("Education", "Vzdelanie", StringComparison.Ordinal)
            .Replace("Skills", "Zručnosti", StringComparison.Ordinal)
            .Replace("Languages", "Jazyky", StringComparison.Ordinal)
            .Replace("Certificates", "Certifikáty", StringComparison.Ordinal)
            .Replace("Projects", "Projekty", StringComparison.Ordinal)
            .Replace(" – ", " – ", StringComparison.Ordinal);

        return localized.Replace("\n", "\r\n", StringComparison.Ordinal);
    }

    private static string ApplySplitCertificateIssuedLines(string text) =>
        text.Replace("Issued: 02 / 202", $"Issued:{Environment.NewLine}02 / 202", StringComparison.Ordinal);

    private static string ApplyValidThroughLabel(string text) =>
        text.Replace("Valid through:", "Valid through:", StringComparison.Ordinal);

    private static string RenderCertificateInlineMainLine(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();
        AppendSection(sb, document.Labels.Contact, BuildContactSection(document));
        AppendSection(sb, document.Labels.Digital, BuildDigitalSection(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AppendSection(sb, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));

        var certificates = new StringBuilder();
        foreach (var entry in document.CertificateEntries)
        {
            var issuer = entry.DetailLines.FirstOrDefault(line =>
                line.StartsWith("Issuing Organization:", StringComparison.OrdinalIgnoreCase))?
                .Replace("Issuing Organization:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim() ?? "Global Tech Certification Board";
            certificates.AppendLine($"{entry.MainLine} · {issuer} · Feb 2021 · Valid until Feb 2024");
            foreach (var line in entry.DetailLines.Skip(1))
            {
                certificates.AppendLine(line);
            }

            certificates.AppendLine();
        }

        AppendSection(sb, document.Labels.PreviewCertificates, certificates.ToString().TrimEnd());
        AppendRemainingSections(sb, document, includeWork: false, includeEducation: false, includeCertificates: false);
        return sb.ToString();
    }

    private static string ApplyCertificateMmmYyyyDates(string text) =>
        text.Replace("Issued: 02 / 2021", "Issued: Feb 2021", StringComparison.Ordinal)
            .Replace("Valid through: 02 / 2024", "Valid through: Feb 2024", StringComparison.Ordinal);

    private static string ApplyWorkPresentOnOwnLine(string text) =>
        text.Replace(" · 03 / 2025 – Present", $" · 03 / 2025 –{Environment.NewLine}Present", StringComparison.Ordinal);

    private static string RenderSummaryAtEnd(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();
        AppendSection(sb, document.Labels.Contact, BuildContactSection(document));
        AppendSection(sb, document.Labels.Digital, BuildDigitalSection(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AppendSection(sb, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewProjects, CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewCustomLinks, CvExportPreviewContentBuilder.BuildCustomLinksPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewAdditionalInformation, document.AdditionalInformationContent ?? string.Empty);
        AppendSection(sb, document.Labels.Profile, CvExportPreviewContentBuilder.BuildSummary(document));
        return sb.ToString();
    }

    private static string RenderCertificatesBeforeEducation(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();
        AppendSection(sb, document.Labels.Contact, BuildContactSection(document));
        AppendSection(sb, document.Labels.Digital, BuildDigitalSection(document));
        AppendSection(sb, document.Labels.Profile, CvExportPreviewContentBuilder.BuildSummary(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AppendSection(sb, document.Labels.PreviewCertificates, CvExportPreviewContentBuilder.BuildCertificatesPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewSkills, CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewLanguages, CvExportPreviewContentBuilder.BuildLanguagesPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewProjects, CvExportPreviewContentBuilder.BuildProjectsPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewCustomLinks, CvExportPreviewContentBuilder.BuildCustomLinksPreviewContent(document));
        AppendSection(sb, document.Labels.PreviewAdditionalInformation, document.AdditionalInformationContent ?? string.Empty);
        return sb.ToString();
    }

    private static string ApplyUppercaseSectionHeaders(string text) =>
        text.Replace("Work Experience", "WORK EXPERIENCE", StringComparison.Ordinal)
            .Replace("Education", "EDUCATION", StringComparison.Ordinal)
            .Replace("Skills", "SKILLS", StringComparison.Ordinal)
            .Replace("Languages", "LANGUAGES", StringComparison.Ordinal)
            .Replace("Certificates", "CERTIFICATES", StringComparison.Ordinal)
            .Replace("Projects", "PROJECTS", StringComparison.Ordinal);

    private static string ApplyTabIndentedContent(string text) =>
        text.Replace($"{Environment.NewLine}Principal Software Engineer", $"{Environment.NewLine}\tPrincipal Software Engineer", StringComparison.Ordinal);

    private static string ApplySingleNewlineWorkEntries(string text)
    {
        var work = CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(JohnDoeStressCvDataset.CreateDocument());
        return text.Replace(
            work,
            string.Join('\n', work.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
            StringComparison.Ordinal);
    }

    private static string RenderProjectLabeledFields(CvExportDocument document) =>
        RenderDefaultReVitae(document, contactFirst: true);

    private static string RenderEducationInstitutionFirst(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();
        AppendSection(sb, document.Labels.Contact, BuildContactSection(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));

        var education = new StringBuilder();
        foreach (var entry in document.EducationEntries)
        {
            education.AppendLine(entry.Institution);
            education.AppendLine(entry.Degree);
            education.AppendLine($"{entry.Location} · {entry.DegreeTypeLabel} · {entry.DateRange}");
            education.AppendLine();
        }

        AppendSection(sb, document.Labels.PreviewEducation, education.ToString().TrimEnd());
        AppendRemainingSections(sb, document, includeWork: false, includeEducation: false);
        return sb.ToString();
    }

    private static string RenderSkillsPipeSeparated(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();
        AppendSection(sb, document.Labels.Contact, BuildContactSection(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document));
        AppendSection(sb, document.Labels.PreviewEducation, CvExportPreviewContentBuilder.BuildEducationPreviewContent(document));

        var skills = new StringBuilder();
        foreach (var group in document.SkillsGroups)
        {
            skills.AppendLine($"{group.Category} | {string.Join(", ", group.Skills.Select(skill => skill.Name))}");
        }

        AppendSection(sb, document.Labels.PreviewSkills, skills.ToString().TrimEnd());
        AppendRemainingSections(sb, document, includeWork: false, includeEducation: false, includeSkills: false);
        return sb.ToString();
    }

    private static string RenderLinksWithBullets(CvExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();
        AppendSection(sb, document.Labels.Contact, BuildContactSection(document));
        AppendRemainingSections(sb, document, includeLinks: false);
        var links = string.Join(
            Environment.NewLine,
            document.CustomLinkLines.Select(line => $"- {line}"));
        AppendSection(sb, document.Labels.PreviewCustomLinks, links);
        return sb.ToString();
    }

    private static string ApplyPhoneParenthesesFormat(string text) =>
        text.Replace("+1 (555) 010-2030", "(555) 010-2030", StringComparison.Ordinal);

    private static string ApplyCertificateCredentialUrlLines(string text) =>
        text.Replace(
            "Credential ID:",
            "Credential URL: https://certs.example.com/john-doe\nCredential ID:",
            StringComparison.Ordinal);

    private static string RenderWorkContractEmploymentType(CvExportDocument document)
    {
        var work = CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document)
            .Replace(" · Full-time · ", " · Contract · ", StringComparison.Ordinal);
        var sb = new StringBuilder();
        sb.AppendLine(document.FullName);
        sb.AppendLine();
        AppendSection(sb, document.Labels.Contact, BuildContactSection(document));
        AppendSection(sb, document.Labels.PreviewWorkExperience, work);
        AppendRemainingSections(sb, document, includeWork: false);
        return sb.ToString();
    }

    private static string ApplyMixedColonHeaders(string text) =>
        text.Replace($"{Environment.NewLine}Contact{Environment.NewLine}", $"{Environment.NewLine}Contact:{Environment.NewLine}", StringComparison.Ordinal)
            .Replace($"{Environment.NewLine}Profile{Environment.NewLine}", $"{Environment.NewLine}Profile:{Environment.NewLine}", StringComparison.Ordinal);
}
