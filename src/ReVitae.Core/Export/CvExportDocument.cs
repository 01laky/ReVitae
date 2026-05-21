namespace ReVitae.Core.Export;

public sealed record WorkExperienceEntry(
    string JobTitle,
    string Company,
    string Location,
    string EmploymentTypeLabel,
    string DateRange,
    string? Description,
    string? Achievements,
    string? Technologies,
    string? CompanyUrl);

public sealed record EducationEntry(
    string Degree,
    string Institution,
    string FieldOfStudy,
    string Location,
    string DegreeTypeLabel,
    string DateRange,
    string? Grade,
    string? Description,
    string? InstitutionUrl);

public sealed record SkillItem(
    string Name,
    string ProficiencyLabel,
    int? YearsOfExperience);

public sealed record SkillsGroup(
    string Category,
    IReadOnlyList<SkillItem> Skills);

public sealed record LanguageEntry(
    string MainLine,
    IReadOnlyList<string> SubSkillLines);

public sealed record CertificateEntry(
    string MainLine,
    IReadOnlyList<string> DetailLines);

public sealed record ProjectEntry(
    string MainLine,
    IReadOnlyList<string> DetailLines);

public sealed record CvExportDocument(
    CvExportTemplateId TemplateId,
    CvExportSectionLabels Labels,
    string FirstName,
    string LastName,
    string ProfessionalTitle,
    string Email,
    string Phone,
    string Location,
    string LinkedInUrl,
    string PortfolioUrl,
    string GitHubUrl,
    string? ShortSummary,
    string? PhotoPath,
    IReadOnlyList<WorkExperienceEntry> WorkExperienceEntries,
    IReadOnlyList<EducationEntry> EducationEntries,
    IReadOnlyList<SkillsGroup> SkillsGroups,
    IReadOnlyList<LanguageEntry> LanguageEntries,
    IReadOnlyList<CertificateEntry> CertificateEntries,
    IReadOnlyList<ProjectEntry> ProjectEntries,
    IReadOnlyList<string> CustomLinkLines,
    string? AdditionalInformationContent)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}
