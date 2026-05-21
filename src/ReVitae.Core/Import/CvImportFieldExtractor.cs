using System.Text.RegularExpressions;
using ReVitae.Core.Cv;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import.Patterns;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import;

internal sealed class CvImportBuildContext
{
    public List<CvImportWarning> Warnings { get; } = [];

    public List<ImportedFieldConfidence> FieldConfidences { get; } = [];

    public void AddConfidence(string fieldKey, CvImportConfidence confidence)
    {
        FieldConfidences.Add(new ImportedFieldConfidence(fieldKey, confidence));
    }
}

public static class CvImportFieldExtractor
{
    public static CvImportResult Extract(CvSegmentationResult segmentation)
    {
        var context = new CvImportBuildContext();
        var personal = ExtractPersonalInformation(segmentation, context);
        var workExperience = ExtractWorkExperience(GetBody(segmentation, CvImportSectionId.WorkExperience));
        var education = ExtractEducation(GetBody(segmentation, CvImportSectionId.Education));
        var skills = ExtractSkills(GetBody(segmentation, CvImportSectionId.Skills), context);
        var languages = ExtractLanguages(GetBody(segmentation, CvImportSectionId.Languages), context);
        var certificates = ExtractCertificates(GetBody(segmentation, CvImportSectionId.Certificates));
        var projects = ExtractProjects(GetBody(segmentation, CvImportSectionId.Projects));
        var links = ExtractLinks(GetBody(segmentation, CvImportSectionId.Links), personal, context);
        var additional = BuildAdditionalInformation(segmentation, context);

        AddEntryConfidences(context, workExperience, education, languages, certificates, projects, links);

        var sectionHasData = BuildSectionHasData(
            personal,
            workExperience,
            education,
            skills,
            languages,
            certificates,
            projects,
            links,
            additional);

        return new CvImportResult
        {
            Success = HasStructuredData(personal, workExperience, education, skills, languages, certificates, projects, links, additional),
            ErrorMessageKey = null,
            Personal = personal,
            WorkExperienceEntries = workExperience,
            EducationEntries = education,
            SkillsGroups = skills,
            LanguageEntries = languages,
            CertificateEntries = certificates,
            ProjectEntries = projects,
            LinkEntries = links,
            AdditionalInformationContent = additional,
            SectionHasData = sectionHasData,
            Warnings = context.Warnings,
            FieldConfidences = context.FieldConfidences
        };
    }

    private static PersonalInformationImport ExtractPersonalInformation(
        CvSegmentationResult segmentation,
        CvImportBuildContext context)
    {
        var contactBody = GetBody(segmentation, CvImportSectionId.Contact);
        var personalSource = string.Join(
            "\n",
            new[] { segmentation.HeaderBlock, contactBody }.Where(part => !string.IsNullOrWhiteSpace(part)));
        var headerLines = personalSource.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var personal = new PersonalInformationImport();
        var assignedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var combinedHeader = personalSource;
        var emailMatch = CvImportPatterns.Email.Match(combinedHeader);
        if (emailMatch.Success)
        {
            personal.Email = emailMatch.Value;
            context.AddConfidence(MainPersonalInformationFieldKeys.Email, CvImportConfidence.High);
        }

        var phoneMatch = CvImportPatterns.Phone.Match(combinedHeader);
        if (phoneMatch.Success)
        {
            personal.Phone = phoneMatch.Value.Trim();
            context.AddConfidence(MainPersonalInformationFieldKeys.Phone, CvImportConfidence.High);
        }

        foreach (Match urlMatch in CvImportPatterns.Url.Matches(combinedHeader))
        {
            var url = urlMatch.Value.Trim();
            if (url.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(personal.LinkedInUrl))
            {
                personal.LinkedInUrl = url;
                assignedUrls.Add(url);
                context.AddConfidence(MainPersonalInformationFieldKeys.LinkedInUrl, CvImportConfidence.High);
            }
            else if (url.Contains("github.com", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(personal.GitHubUrl))
            {
                personal.GitHubUrl = url;
                assignedUrls.Add(url);
                context.AddConfidence(MainPersonalInformationFieldKeys.GitHubUrl, CvImportConfidence.High);
            }
            else if (string.IsNullOrWhiteSpace(personal.PortfolioUrl) && !assignedUrls.Contains(url))
            {
                personal.PortfolioUrl = url;
                assignedUrls.Add(url);
                context.AddConfidence(MainPersonalInformationFieldKeys.PortfolioUrl, CvImportConfidence.Medium);
            }
        }

        foreach (var line in headerLines)
        {
            var labeled = CvImportPatterns.LabeledValue.Match(line);
            if (!labeled.Success)
            {
                continue;
            }

            var label = labeled.Groups["label"].Value.Trim();
            var value = labeled.Groups["value"].Value.Trim();
            if (label.Equals("location", StringComparison.OrdinalIgnoreCase) || label.Equals("lokalita", StringComparison.OrdinalIgnoreCase))
            {
                personal.Location = value;
                context.AddConfidence(MainPersonalInformationFieldKeys.Location, CvImportConfidence.Medium);
            }
        }

        if (headerLines.Length > 0 && string.IsNullOrWhiteSpace(personal.FirstName))
        {
            var nameLine = headerLines[0];
            if (!CvImportPatterns.Email.IsMatch(nameLine) && !CvImportPatterns.Url.IsMatch(nameLine))
            {
                var parts = nameLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 1
                    && headerLines.Length > 1
                    && IsLikelyNameToken(headerLines[1]))
                {
                    personal.FirstName = headerLines[0];
                    personal.LastName = headerLines[1];
                    context.AddConfidence(MainPersonalInformationFieldKeys.FirstName, CvImportConfidence.Medium);
                    context.AddConfidence(MainPersonalInformationFieldKeys.LastName, CvImportConfidence.Medium);
                }
                else if (parts.Length >= 2)
                {
                    personal.FirstName = parts[0];
                    personal.LastName = string.Join(' ', parts.Skip(1));
                    context.AddConfidence(MainPersonalInformationFieldKeys.FirstName, CvImportConfidence.Low);
                    context.AddConfidence(MainPersonalInformationFieldKeys.LastName, CvImportConfidence.Low);
                }
                else
                {
                    personal.FirstName = nameLine;
                    context.AddConfidence(MainPersonalInformationFieldKeys.FirstName, CvImportConfidence.Low);
                    context.Warnings.Add(new CvImportWarning(TranslationKeys.ImportWarningNameUncertain));
                }
            }
        }

        if (headerLines.Length > 1 && string.IsNullOrWhiteSpace(personal.ProfessionalTitle))
        {
            var titleLineIndex = !string.IsNullOrWhiteSpace(personal.LastName)
                && headerLines.Length > 2
                && headerLines[1].Equals(personal.LastName, StringComparison.Ordinal)
                    ? 2
                    : 1;
            var titleLine = headerLines[titleLineIndex];
            if (!CvImportPatterns.Email.IsMatch(titleLine) && !CvImportPatterns.Url.IsMatch(titleLine))
            {
                personal.ProfessionalTitle = titleLine;
                context.AddConfidence(MainPersonalInformationFieldKeys.ProfessionalTitle, CvImportConfidence.Medium);
            }
        }

        if (segmentation.SectionBodies.TryGetValue(CvImportSectionId.Summary, out var summaryBody)
            && !string.IsNullOrWhiteSpace(summaryBody))
        {
            personal.ShortSummary = summaryBody.Trim();
            context.AddConfidence(MainPersonalInformationFieldKeys.ShortSummary, CvImportConfidence.Medium);
        }

        return personal;
    }

    private static IReadOnlyList<WorkExperienceEntry> ExtractWorkExperience(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        var blocks = SplitWorkExperienceBlocks(body);
        var entries = new List<WorkExperienceEntry>();

        foreach (var block in blocks)
        {
            var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length == 0)
            {
                continue;
            }

            var entry = new WorkExperienceEntry();
            var titleLine = lines[0];
            ParsedDateRange? dateRange = null;
            var lineIndex = 1;

            if (DateRangeParser.TryParse(titleLine, out var titleDates))
            {
                dateRange = titleDates;
                titleLine = lines.Length > 1 ? lines[1] : string.Empty;
                lineIndex = 2;
            }
            else
            {
                TryParseLeadingWorkExperienceMetadata(lines, ref lineIndex, entry, out dateRange);
            }

            if (!string.IsNullOrWhiteSpace(titleLine))
            {
                SplitTitleCompany(titleLine, out var jobTitle, out var company);
                entry.JobTitle = jobTitle;
                entry.Company = company;
            }

            if (dateRange is not null)
            {
                entry.StartMonth = dateRange.StartMonth;
                entry.StartYear = dateRange.StartYear;
                entry.EndMonth = dateRange.EndMonth;
                entry.EndYear = dateRange.EndYear;
                entry.IsCurrentlyWorking = dateRange.IsPresent;
            }

            var descriptionLines = new List<string>();
            var achievementLines = new List<string>();
            for (; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                if (line.StartsWith("- ", StringComparison.Ordinal))
                {
                    achievementLines.Add(line[2..]);
                    continue;
                }

                if (line.StartsWith("Technologies:", StringComparison.OrdinalIgnoreCase))
                {
                    entry.Technologies = MergeTechnologies(entry.Technologies, line["Technologies:".Length..].Trim());
                    continue;
                }

                if (LooksLikeTechnologyList(line))
                {
                    entry.Technologies = MergeTechnologies(entry.Technologies, line);
                    continue;
                }

                if (LooksLikeSidebarSkillToken(line) || IsRepeatedJobTitleLine(line, entry.JobTitle))
                {
                    continue;
                }

                descriptionLines.Add(line);
            }

            entry.Description = string.Join('\n', descriptionLines).Trim();
            entry.Achievements = string.Join('\n', achievementLines).Trim();

            if (entry.HasUserInput())
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private static IReadOnlyList<EducationEntry> ExtractEducation(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        var entries = new List<EducationEntry>();
        foreach (var block in SplitBlocks(body))
        {
            var lines = block.Split('\n', StringSplitOptions.TrimEntries);
            if (lines.Length == 0)
            {
                continue;
            }

            var entry = new EducationEntry { Degree = lines[0] };
            var startIndex = 1;
            if (lines.Length > 1 && DateRangeParser.TryParse(lines[1], out var dates))
            {
                entry.StartMonth = dates.StartMonth;
                entry.StartYear = dates.StartYear;
                entry.EndMonth = dates.EndMonth;
                entry.EndYear = dates.EndYear;
                startIndex = 2;
            }
            else if (lines.Length > 1)
            {
                entry.Institution = lines[1];
                startIndex = 2;
            }

            if (startIndex < lines.Length)
            {
                entry.Description = string.Join('\n', lines.Skip(startIndex)).Trim();
            }

            if (entry.HasUserInput())
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private static IReadOnlyList<SkillsGroupEntry> ExtractSkills(string body, CvImportBuildContext context)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        var groups = new List<SkillsGroupEntry>();
        foreach (var line in body.Split('\n', StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.Contains(':', StringComparison.Ordinal))
            {
                var parts = line.Split(':', 2);
                var group = new SkillsGroupEntry { Category = parts[0].Trim() };
                foreach (var skillName in SplitCommaList(parts[1]))
                {
                    group.Skills.Add(new SkillItem { Name = skillName });
                }

                if (group.HasUserInput())
                {
                    groups.Add(group);
                }

                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                EnsureDefaultSkillsGroup(groups).Skills.Add(new SkillItem { Name = line[2..].Trim() });
                continue;
            }

            foreach (var skillName in SplitCommaList(line))
            {
                EnsureDefaultSkillsGroup(groups).Skills.Add(new SkillItem { Name = skillName });
            }
        }

        if (groups.Count > 0)
        {
            context.AddConfidence("skills.import.defaultCategory", CvImportConfidence.Medium);
        }

        return groups;
    }

    private static IReadOnlyList<LanguageEntry> ExtractLanguages(string body, CvImportBuildContext context)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        var entries = new List<LanguageEntry>();
        foreach (var line in body.Split('\n', StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var entry = new LanguageEntry();
            var separatorIndex = line.IndexOf('-');
            if (separatorIndex < 0)
            {
                separatorIndex = line.IndexOf('—');
            }

            if (separatorIndex > 0)
            {
                entry.Language = line[..separatorIndex].Trim();
                MapLanguageProficiency(line[(separatorIndex + 1)..].Trim(), entry, context);
            }
            else
            {
                entry.Language = line.Trim();
                context.AddConfidence($"languages.{entry.Id}.language", CvImportConfidence.Medium);
            }

            if (entry.HasUserInput())
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private static IReadOnlyList<CertificateEntry> ExtractCertificates(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        var entries = new List<CertificateEntry>();
        foreach (var block in SplitBlocks(body))
        {
            var lines = block.Split('\n', StringSplitOptions.TrimEntries);
            if (lines.Length == 0)
            {
                continue;
            }

            var entry = new CertificateEntry
            {
                Name = lines[0],
                Issuer = lines.Length > 1 ? lines[1] : string.Empty
            };

            if (lines.Length > 2)
            {
                entry.Description = string.Join('\n', lines.Skip(2)).Trim();
            }

            if (entry.HasUserInput())
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private static IReadOnlyList<ProjectEntry> ExtractProjects(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        var entries = new List<ProjectEntry>();
        foreach (var block in SplitBlocks(body))
        {
            var lines = block.Split('\n', StringSplitOptions.TrimEntries);
            if (lines.Length == 0)
            {
                continue;
            }

            var entry = new ProjectEntry { Name = lines[0] };
            var startIndex = 1;
            if (lines.Length > 1 && DateRangeParser.TryParse(lines[1], out var dates))
            {
                entry.StartMonth = dates.StartMonth;
                entry.StartYear = dates.StartYear;
                entry.EndMonth = dates.EndMonth;
                entry.EndYear = dates.EndYear;
                startIndex = 2;
            }

            var descriptionLines = new List<string>();
            for (; startIndex < lines.Length; startIndex++)
            {
                var line = lines[startIndex];
                var urlMatch = CvImportPatterns.Url.Match(line);
                if (urlMatch.Success && string.IsNullOrWhiteSpace(entry.ProjectUrl))
                {
                    entry.ProjectUrl = urlMatch.Value;
                    continue;
                }

                if (line.StartsWith("Tech:", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("Stack:", StringComparison.OrdinalIgnoreCase))
                {
                    var techPart = line[(line.IndexOf(':') + 1)..];
                    foreach (var tech in SplitCommaList(techPart))
                    {
                        entry.Technologies.Add(new ProjectTechnologyItem { Name = tech });
                    }

                    continue;
                }

                descriptionLines.Add(line);
            }

            entry.Description = string.Join('\n', descriptionLines).Trim();
            if (entry.HasUserInput())
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private static IReadOnlyList<LinkEntry> ExtractLinks(
        string body,
        PersonalInformationImport personal,
        CvImportBuildContext context)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            personal.LinkedInUrl,
            personal.GitHubUrl,
            personal.PortfolioUrl
        };

        var entries = new List<LinkEntry>();
        var source = body;
        if (string.IsNullOrWhiteSpace(source))
        {
            return entries;
        }

        foreach (Match match in CvImportPatterns.Url.Matches(source))
        {
            var url = match.Value.Trim();
            if (excluded.Contains(url))
            {
                context.Warnings.Add(new CvImportWarning(TranslationKeys.ImportWarningPersonalLinksDuplicatedSkipped));
                continue;
            }

            var entry = new LinkEntry
            {
                Url = url,
                Label = InferLabelFromUrl(url)
            };
            entries.Add(entry);
            context.AddConfidence(Cv.Links.LinksFieldKeys.Build(entry.Id, Cv.Links.LinksFieldKeys.Url), CvImportConfidence.Medium);
            context.AddConfidence(Cv.Links.LinksFieldKeys.Build(entry.Id, Cv.Links.LinksFieldKeys.Label), CvImportConfidence.Low);
        }

        return entries;
    }

    private static string BuildAdditionalInformation(CvSegmentationResult segmentation, CvImportBuildContext context)
    {
        if (segmentation.SectionBodies.TryGetValue(CvImportSectionId.AdditionalInformation, out var body))
        {
            if (!string.IsNullOrWhiteSpace(body))
            {
                context.AddConfidence(Cv.AdditionalInformation.AdditionalInformationFieldKeys.Content, CvImportConfidence.Medium);
            }

            return body.Trim();
        }

        return string.Empty;
    }

    private static void AddEntryConfidences(
        CvImportBuildContext context,
        IReadOnlyList<WorkExperienceEntry> workExperience,
        IReadOnlyList<EducationEntry> education,
        IReadOnlyList<LanguageEntry> languages,
        IReadOnlyList<CertificateEntry> certificates,
        IReadOnlyList<ProjectEntry> projects,
        IReadOnlyList<LinkEntry> links)
    {
        foreach (var entry in workExperience)
        {
            if (!string.IsNullOrWhiteSpace(entry.JobTitle))
            {
                context.AddConfidence(WorkExperienceFieldKeys.Build(entry.Id, WorkExperienceFieldKeys.JobTitle), CvImportConfidence.Medium);
            }

            if (!string.IsNullOrWhiteSpace(entry.Company))
            {
                context.AddConfidence(WorkExperienceFieldKeys.Build(entry.Id, WorkExperienceFieldKeys.Company), CvImportConfidence.Medium);
            }
        }

        foreach (var entry in education)
        {
            if (!string.IsNullOrWhiteSpace(entry.Degree))
            {
                context.AddConfidence(EducationFieldKeys.Build(entry.Id, EducationFieldKeys.Degree), CvImportConfidence.Medium);
            }
        }

        foreach (var entry in languages)
        {
            if (!string.IsNullOrWhiteSpace(entry.Language))
            {
                context.AddConfidence(LanguagesFieldKeys.Build(entry.Id, LanguagesFieldKeys.Language), CvImportConfidence.Medium);
            }
        }

        foreach (var entry in certificates)
        {
            if (!string.IsNullOrWhiteSpace(entry.Name))
            {
                context.AddConfidence(CertificatesFieldKeys.Build(entry.Id, CertificatesFieldKeys.Name), CvImportConfidence.Medium);
            }
        }

        foreach (var entry in projects)
        {
            if (!string.IsNullOrWhiteSpace(entry.Name))
            {
                context.AddConfidence(ProjectsFieldKeys.Build(entry.Id, ProjectsFieldKeys.Name), CvImportConfidence.Medium);
            }
        }

        _ = links;
    }

    private static IReadOnlyDictionary<CvImportSectionId, bool> BuildSectionHasData(
        PersonalInformationImport personal,
        IReadOnlyList<WorkExperienceEntry> workExperience,
        IReadOnlyList<EducationEntry> education,
        IReadOnlyList<SkillsGroupEntry> skills,
        IReadOnlyList<LanguageEntry> languages,
        IReadOnlyList<CertificateEntry> certificates,
        IReadOnlyList<ProjectEntry> projects,
        IReadOnlyList<LinkEntry> links,
        string additional)
    {
        return new Dictionary<CvImportSectionId, bool>
        {
            [CvImportSectionId.PersonalInformation] = personal.HasAnyData(),
            [CvImportSectionId.WorkExperience] = workExperience.Count > 0,
            [CvImportSectionId.Education] = education.Count > 0,
            [CvImportSectionId.Skills] = skills.Count > 0,
            [CvImportSectionId.Languages] = languages.Count > 0,
            [CvImportSectionId.Certificates] = certificates.Count > 0,
            [CvImportSectionId.Projects] = projects.Count > 0,
            [CvImportSectionId.Links] = links.Count > 0,
            [CvImportSectionId.AdditionalInformation] = !string.IsNullOrWhiteSpace(additional)
        };
    }

    private static bool HasStructuredData(
        PersonalInformationImport personal,
        IReadOnlyList<WorkExperienceEntry> workExperience,
        IReadOnlyList<EducationEntry> education,
        IReadOnlyList<SkillsGroupEntry> skills,
        IReadOnlyList<LanguageEntry> languages,
        IReadOnlyList<CertificateEntry> certificates,
        IReadOnlyList<ProjectEntry> projects,
        IReadOnlyList<LinkEntry> links,
        string additional)
    {
        return personal.HasAnyData()
            || workExperience.Count > 0
            || education.Count > 0
            || skills.Count > 0
            || languages.Count > 0
            || certificates.Count > 0
            || projects.Count > 0
            || links.Count > 0
            || !string.IsNullOrWhiteSpace(additional);
    }

    private static string GetBody(CvSegmentationResult segmentation, CvImportSectionId sectionId)
    {
        return segmentation.SectionBodies.TryGetValue(sectionId, out var body) ? body : string.Empty;
    }

    private static void TryParseLeadingWorkExperienceMetadata(
        string[] lines,
        ref int lineIndex,
        WorkExperienceEntry entry,
        out ParsedDateRange? dateRange)
    {
        dateRange = null;
        if (lineIndex >= lines.Length)
        {
            return;
        }

        if (DateRangeParser.TryParse(lines[lineIndex], out var directDate))
        {
            dateRange = directDate;
            lineIndex++;
            return;
        }

        if (lineIndex + 1 < lines.Length
            && LooksLikeLocationLine(lines[lineIndex])
            && DateRangeParser.TryParse(lines[lineIndex + 1], out var dateAfterLocation))
        {
            entry.Location = lines[lineIndex];
            dateRange = dateAfterLocation;
            lineIndex += 2;
            return;
        }

        for (var index = lineIndex; index < Math.Min(lines.Length, lineIndex + 3); index++)
        {
            if (!DateRangeParser.TryParse(lines[index], out var candidateDates))
            {
                continue;
            }

            if (index > lineIndex && LooksLikeLocationLine(lines[lineIndex]))
            {
                entry.Location = lines[lineIndex];
            }

            dateRange = candidateDates;
            lineIndex = index + 1;
            return;
        }
    }

    private static bool LooksLikeLocationLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || LooksLikeWorkEntryHeader(line))
        {
            return false;
        }

        if (DateRangeParser.TryParse(line, out _))
        {
            return false;
        }

        if (line.StartsWith("- ", StringComparison.Ordinal)
            || line.StartsWith("Technologies:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!line.Contains(','))
        {
            return false;
        }

        var parts = line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 2 && LooksLikeCityCountryPair(parts[0], parts[1]);
    }

    private static bool LooksLikeTechnologyList(string line)
    {
        if (!line.Contains(','))
        {
            return false;
        }

        var parts = SplitCommaList(line).ToArray();
        if (parts.Length < 2 || parts.Any(part => part.Length is < 1 or > 40))
        {
            return false;
        }

        if (parts.Length == 2 && LooksLikeCityCountryPair(parts[0], parts[1]))
        {
            return false;
        }

        return true;
    }

    private static bool LooksLikeCityCountryPair(string city, string country)
    {
        return city.Length >= 2
            && country.Length >= 3
            && char.IsLetter(city[0])
            && char.IsLetter(country[0])
            && !city.Any(char.IsDigit)
            && !country.Any(char.IsDigit)
            && !city.Contains('.')
            && !country.Contains('.')
            && !country.Contains('#')
            && !city.Contains('#');
    }

    private static bool LooksLikeSidebarSkillToken(string line)
    {
        if (string.IsNullOrWhiteSpace(line)
            || line.Contains(' ')
            || line.Contains('.')
            || line.Contains(',')
            || line.Contains(':'))
        {
            return false;
        }

        return line.Length <= 30;
    }

    private static bool IsRepeatedJobTitleLine(string line, string jobTitle)
    {
        return !string.IsNullOrWhiteSpace(jobTitle)
            && line.Equals(jobTitle, StringComparison.OrdinalIgnoreCase);
    }

    private static string MergeTechnologies(string existing, string additional)
    {
        if (string.IsNullOrWhiteSpace(existing))
        {
            return additional.Trim();
        }

        if (string.IsNullOrWhiteSpace(additional))
        {
            return existing.Trim();
        }

        return $"{existing.Trim()}, {additional.Trim()}";
    }

    private static IEnumerable<string> SplitBlocks(string body)
    {
        return body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static IEnumerable<string> SplitWorkExperienceBlocks(string body)
    {
        var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var blocks = new List<List<string>>();
        List<string>? current = null;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var startsEntry = LooksLikeWorkEntryHeader(line)
                && lines.Skip(index + 1).Take(3).Any(candidate => DateRangeParser.TryParse(candidate, out _));

            if (startsEntry)
            {
                current = [line];
                blocks.Add(current);
                continue;
            }

            current ??= [];
            current.Add(line);
        }

        return blocks
            .Where(block => block.Count > 0)
            .Select(block => string.Join('\n', block));
    }

    private static bool LooksLikeWorkEntryHeader(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        return line.Contains(" - ", StringComparison.Ordinal)
            || line.Contains(" at ", StringComparison.OrdinalIgnoreCase)
            || line.Contains(" s.r.o.", StringComparison.OrdinalIgnoreCase)
            || line.Contains(" a.s.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLikelyNameToken(string line)
    {
        if (string.IsNullOrWhiteSpace(line)
            || CvImportPatterns.Email.IsMatch(line)
            || CvImportPatterns.Url.IsMatch(line)
            || CvImportPatterns.Phone.IsMatch(line))
        {
            return false;
        }

        return line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length == 1;
    }

    private static IEnumerable<string> SplitCommaList(string value)
    {
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static void SplitTitleCompany(string line, out string jobTitle, out string company)
    {
        var atIndex = line.IndexOf(" at ", StringComparison.OrdinalIgnoreCase);
        if (atIndex > 0)
        {
            jobTitle = line[..atIndex].Trim();
            company = line[(atIndex + 4)..].Trim();
            return;
        }

        var dashIndex = line.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIndex > 0)
        {
            company = line[..dashIndex].Trim();
            jobTitle = line[(dashIndex + 3)..].Trim();
            return;
        }

        var pipeIndex = line.IndexOf('|');
        if (pipeIndex > 0)
        {
            jobTitle = line[..pipeIndex].Trim();
            company = line[(pipeIndex + 1)..].Trim();
            return;
        }

        jobTitle = line.Trim();
        company = string.Empty;
    }

    private static SkillsGroupEntry EnsureDefaultSkillsGroup(List<SkillsGroupEntry> groups)
    {
        const string defaultCategory = "General";
        var existing = groups.FirstOrDefault(group => group.Category.Equals(defaultCategory, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var created = new SkillsGroupEntry { Category = defaultCategory };
        groups.Add(created);
        return created;
    }

    private static void MapLanguageProficiency(string token, LanguageEntry entry, CvImportBuildContext context)
    {
        token = token.Trim().Trim('(', ')');
        if (Enum.TryParse<CefrLevel>(token, ignoreCase: true, out var cefr))
        {
            entry.CefrLevel = cefr;
            context.AddConfidence(LanguagesFieldKeys.Build(entry.Id, LanguagesFieldKeys.CefrLevel), CvImportConfidence.High);
            return;
        }

        entry.Proficiency = token.ToLowerInvariant() switch
        {
            "native" or "mother tongue" => LanguageProficiency.Native,
            "fluent" or "full professional" => LanguageProficiency.Fluent,
            "advanced" or "upper intermediate" => LanguageProficiency.Advanced,
            "intermediate" or "working proficiency" => LanguageProficiency.Intermediate,
            "elementary" or "basic" or "beginner" => LanguageProficiency.Elementary,
            _ => LanguageProficiency.Intermediate
        };
        context.AddConfidence(LanguagesFieldKeys.Build(entry.Id, LanguagesFieldKeys.Proficiency), CvImportConfidence.Medium);
    }

    private static string InferLabelFromUrl(string url)
    {
        if (url.Contains("behance.net", StringComparison.OrdinalIgnoreCase))
        {
            return "Behance";
        }

        if (url.Contains("orcid.org", StringComparison.OrdinalIgnoreCase))
        {
            return "ORCID";
        }

        if (url.Contains("stackoverflow.com", StringComparison.OrdinalIgnoreCase))
        {
            return "Stack Overflow";
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var host = uri.Host.Replace("www.", string.Empty, StringComparison.OrdinalIgnoreCase);
            var label = host.Split('.')[0];
            return char.ToUpperInvariant(label[0]) + label[1..];
        }

        return "Link";
    }
}
