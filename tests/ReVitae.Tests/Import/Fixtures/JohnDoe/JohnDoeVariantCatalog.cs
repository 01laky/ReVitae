using ReVitae.Core.Export;

namespace ReVitae.Tests.Import.Fixtures.JohnDoe;

public static class JohnDoeVariantCatalog
{
    public static IReadOnlyList<JohnDoeVariantSpec> All { get; } = BuildAll();

    private static IReadOnlyList<JohnDoeVariantSpec> BuildAll()
    {
        var specs = new List<JohnDoeVariantSpec>
        {
            Pdf("01", "pdf-modern-sidebar-stress", CvExportTemplateId.ModernSidebar),
            Pdf("02", "pdf-classic-sidebar", CvExportTemplateId.ClassicSidebar),
            Pdf("03", "pdf-clean-top-header", CvExportTemplateId.CleanTopHeader),
            Pdf("04", "pdf-royal-blue-sidebar", CvExportTemplateId.RoyalBlueSidebar),
            Pdf("05", "pdf-navy-profile-split", CvExportTemplateId.NavyProfileSplit),
            Pdf("06", "pdf-photo-left-band", CvExportTemplateId.PhotoLeftBand),
            Pdf("07", "pdf-dark-sidebar-accent", CvExportTemplateId.DarkSidebarAccent),
            Pdf("08", "pdf-executive-blue-sidebar", CvExportTemplateId.ExecutiveBlueSidebar),
            Pdf("09", "pdf-centered-minimal-single-column", CvExportTemplateId.CenteredMinimal),
            Pdf("10", "pdf-orange-timeline", CvExportTemplateId.OrangeTimeline),

            Text("11", "txt-deferred-contact-at-end", JohnDoeTextFormattingProfile.DeferredContactAtEnd, JohnDoeExpectationMode.Full),

            Text("12", "txt-split-linkedin-url-lines", JohnDoeTextFormattingProfile.SplitLinkedInUrl, JohnDoeExpectationMode.Full),
            Text("13", "txt-split-professional-title-lines", JohnDoeTextFormattingProfile.SplitProfessionalTitle, JohnDoeExpectationMode.Full),
            Text("14", "txt-digital-block-end", JohnDoeTextFormattingProfile.DigitalBlockEnd, JohnDoeExpectationMode.StandardEntryCounts),
            Text("15", "txt-work-meta-split-dates", JohnDoeTextFormattingProfile.WorkMetaSplitDates, JohnDoeExpectationMode.StandardEntryCounts),
            Text("16", "txt-education-meta-split-dates", JohnDoeTextFormattingProfile.EducationMetaSplitDates, JohnDoeExpectationMode.Full),
            Text("17", "txt-skills-split-proficiency", JohnDoeTextFormattingProfile.SkillsSplitProficiency, JohnDoeExpectationMode.PdfFull),
            Text("18", "txt-certificates-single-newline", JohnDoeTextFormattingProfile.SingleNewlineCertificatesProjects, JohnDoeExpectationMode.Full),
            Text("19", "txt-languages-with-sublines", JohnDoeTextFormattingProfile.DefaultReVitae, JohnDoeExpectationMode.Full),
            Text("20", "txt-unicode-nbsp-location", JohnDoeTextFormattingProfile.UnicodeNbspLocation, JohnDoeExpectationMode.Full),

            Text("21", "txt-work-at-company", JohnDoeTextFormattingProfile.WorkAtCompany, JohnDoeExpectationMode.StandardEntryCounts),
            Text("22", "txt-work-dash-company", JohnDoeTextFormattingProfile.WorkDashCompany, JohnDoeExpectationMode.StandardEntryCounts),
            Text("23", "txt-skills-colon-categories", JohnDoeTextFormattingProfile.SkillsColonCategories, JohnDoeExpectationMode.Full),
            Text("24", "txt-skills-bullet-list", JohnDoeTextFormattingProfile.SkillsBulletList, JohnDoeExpectationMode.StandardEntryCounts),
            Text("25", "txt-education-degree-first-blankline", JohnDoeTextFormattingProfile.EducationDegreeFirstBlankLine, JohnDoeExpectationMode.Full),
            Text("26", "txt-education-date-first", JohnDoeTextFormattingProfile.EducationDateFirst, JohnDoeExpectationMode.Full),

            new("27", "md-github-style-headings", JohnDoeVariantKind.MarkdownExport, null, JohnDoeTextFormattingProfile.MarkdownHeadings, JohnDoeExpectationMode.StandardEntryCounts, ".md"),
            new("28", "docx-open-xml-export", JohnDoeVariantKind.DocxExport, null, null, JohnDoeExpectationMode.StandardEntryCounts, ".docx"),
            new("29", "html-export-paragraphs", JohnDoeVariantKind.HtmlExport, null, null, JohnDoeExpectationMode.StandardEntryCounts, ".html"),
            Text("30", "txt-crlf-and-emdash-dates", JohnDoeTextFormattingProfile.CrlfEmDashSlovakLabels, JohnDoeExpectationMode.StandardEntryCounts),

            Text("31", "txt-certificate-issued-split-line", JohnDoeTextFormattingProfile.CertificateIssuedSplitLine, JohnDoeExpectationMode.Full),
            Text("32", "txt-certificate-valid-through-label", JohnDoeTextFormattingProfile.CertificateValidThroughLabel, JohnDoeExpectationMode.Full),
            Text("33", "txt-certificate-inline-mainline", JohnDoeTextFormattingProfile.CertificateInlineMainLine, JohnDoeExpectationMode.StandardEntryCounts),
            Text("34", "txt-certificate-mmm-yyyy-dates", JohnDoeTextFormattingProfile.CertificateMmmYyyyDates, JohnDoeExpectationMode.Full),
            Text("35", "txt-work-present-on-own-line", JohnDoeTextFormattingProfile.WorkPresentOnOwnLine, JohnDoeExpectationMode.Full),
            Text("36", "txt-summary-at-end", JohnDoeTextFormattingProfile.SummaryAtEnd, JohnDoeExpectationMode.Full),
            Text("37", "txt-certificates-before-education", JohnDoeTextFormattingProfile.CertificatesBeforeEducation, JohnDoeExpectationMode.Full),
            Text("38", "txt-uppercase-section-headers", JohnDoeTextFormattingProfile.UppercaseSectionHeaders, JohnDoeExpectationMode.StandardEntryCounts),
            Text("39", "txt-tab-indented-content", JohnDoeTextFormattingProfile.TabIndentedContent, JohnDoeExpectationMode.StandardEntryCounts),
            Text("40", "txt-single-newline-work-entries", JohnDoeTextFormattingProfile.SingleNewlineWorkEntries, JohnDoeExpectationMode.StandardEntryCounts),
            Text("41", "txt-project-labeled-fields", JohnDoeTextFormattingProfile.ProjectLabeledFields, JohnDoeExpectationMode.StandardEntryCounts),
            Text("42", "txt-education-institution-first", JohnDoeTextFormattingProfile.EducationInstitutionFirst, JohnDoeExpectationMode.StandardEntryCounts),
            Text("43", "txt-skills-pipe-separated", JohnDoeTextFormattingProfile.SkillsPipeSeparated, JohnDoeExpectationMode.StandardEntryCounts),
            Text("44", "txt-links-with-bullets", JohnDoeTextFormattingProfile.LinksWithBullets, JohnDoeExpectationMode.StandardEntryCounts),
            Text("45", "txt-phone-parentheses-format", JohnDoeTextFormattingProfile.PhoneParenthesesFormat, JohnDoeExpectationMode.Full),
            Text("46", "txt-certificate-credential-url-line", JohnDoeTextFormattingProfile.CertificateCredentialUrlLine, JohnDoeExpectationMode.Full),
            Text("47", "txt-work-contract-employment-type", JohnDoeTextFormattingProfile.WorkContractEmploymentType, JohnDoeExpectationMode.StandardEntryCounts),
            Text("48", "txt-mixed-colon-headers", JohnDoeTextFormattingProfile.MixedColonHeaders, JohnDoeExpectationMode.StandardEntryCounts),
            Pdf("49", "pdf-forest-green-sidebar", CvExportTemplateId.ForestGreenSidebar),
            Pdf("50", "pdf-blue-accent-summary", CvExportTemplateId.BlueAccentSummary),
            new("51", "pdf-deferred-sidebar-contact", JohnDoeVariantKind.DeferredSidebarPdf, null, null, JohnDoeExpectationMode.DeferredSidebarPdf, ".pdf")
        };

        if (specs.Count != 51)
        {
            throw new InvalidOperationException($"Expected 51 John Doe variants, found {specs.Count}.");
        }

        var duplicateIds = specs.GroupBy(spec => spec.Id).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        if (duplicateIds.Length > 0)
        {
            throw new InvalidOperationException($"Duplicate John Doe variant ids: {string.Join(", ", duplicateIds)}");
        }

        return specs;
    }

    private static JohnDoeVariantSpec Pdf(string id, string name, CvExportTemplateId template)
    {
        var mode = id switch
        {
            "01" => JohnDoeExpectationMode.PdfFull,
            "02" or "07" => JohnDoeExpectationMode.PdfSidebarCounts,
            "49" => JohnDoeExpectationMode.PdfTemplateLayout,
            _ => JohnDoeExpectationMode.PdfTemplateLayout
        };
        return new(id, name, JohnDoeVariantKind.PdfTemplate, template, null, mode, ".pdf");
    }

    private static JohnDoeVariantSpec Text(
        string id,
        string name,
        JohnDoeTextFormattingProfile profile,
        JohnDoeExpectationMode mode) =>
        new(id, name, JohnDoeVariantKind.PlainTextProfile, null, profile, mode, ".txt");
}
