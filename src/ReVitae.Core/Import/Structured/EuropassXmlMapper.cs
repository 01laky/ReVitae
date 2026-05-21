using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ReVitae.Core.Cv;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Structured;

/// <summary>Best-effort mapping from Europass / Skills Passport XML exports.</summary>
public static class EuropassXmlMapper
{
    private static readonly Regex YearPattern =
        new(@"\b(19|20)\d{2}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] MiscellaneousLocals =
        ["OtherInfo", "AdditionalInformation", "CoverLetter", "Reference"];

    private static readonly string[] EuropaIndicators =
    [
        "europass",
        "skills passport",
        "skills-passport",
        "skills/passport/",
        "<skillspassport",
        "/esp/"
    ];

    private static readonly HashSet<string> SkillHeadings =
        new(["Skills", "Competencies", "Competences"], StringComparer.OrdinalIgnoreCase);

    public static CvImportResult Map(XDocument doc)
    {
        if (!LooksLikeEuropass(doc))
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnsupportedStructuredFormat);
        }

        var personal = MapPersonal(doc);
        var work = MapWorkExperience(doc);
        var educationEntries = MapEducation(doc);
        var langs = MapLanguages(doc);
        var certs = MapCertificates(doc);
        var skillGroups = MapSkills(doc);
        var additionalRaw = ConcatenateMisc(doc);

        var additionalNormalized = CvTextNormalizer.Normalize(additionalRaw).Trim();

        if (!CvStructuredImportMapper.HasImportableCvData(
                personal,
                work,
                educationEntries,
                skillGroups,
                langs,
                certs,
                [],
                [],
                additionalNormalized))
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorNoStructuredData);
        }

        return new CvImportResult
        {
            Success = true,
            Personal = personal,
            WorkExperienceEntries = work,
            EducationEntries = educationEntries,
            SkillsGroups = skillGroups,
            LanguageEntries = langs,
            CertificateEntries = certs,
            ProjectEntries = [],
            LinkEntries = [],
            AdditionalInformationContent = additionalNormalized,
            SectionHasData = CvStructuredImportMapper.SectionHasData(personal, work, educationEntries,
                skillGroups,
                langs,
                certs, [], [], additionalNormalized),
            Warnings = [],
            FieldConfidences = TagPersonal(personal)
        };
    }

    private static bool LooksLikeEuropass(XDocument doc)
    {
        if (doc.Root is null)
        {
            return false;
        }

        var blob = $"{doc.Root}".ToLowerInvariant();
        if (EuropaIndicators.Any(hit => blob.Contains(hit, StringComparison.Ordinal)))
        {
            return true;
        }

        return doc.Descendants().Any(e => LocalEquals(e.Name.LocalName, "SkillsPassport"));
    }

    private static PersonalInformationImport MapPersonal(XDocument doc)
    {
        var person = new PersonalInformationImport();
        foreach (var block in ElementsNamed(doc.Descendants(), "Identification"))
        {
            var personNameScope = ElementsNamed(block.Descendants(), "PersonName").FirstOrDefault();

            if (personNameScope is not null)
            {
                person.FirstName = Pick(person.FirstName, LocateText(personNameScope, "FirstName", "GivenName"));
                person.LastName = Pick(person.LastName, LocateText(personNameScope, "Surname", "FamilyName"));
            }

            person.Email = Pick(person.Email, LocateText(block, "Email"));
            person.Phone = Pick(person.Phone, LocateText(block, "Telephone", "Mobile"));

            person.Location = Pick(person.Location, LocateText(block, "Municipality", "City"));

            person.ProfessionalTitle = Pick(person.ProfessionalTitle, LocateText(block, "Headline"));

            var goals = ElementsNamed(doc.Descendants(), "LearnerGoals", "Goals").FirstOrDefault();

            person.ShortSummary =
                Pick(person.ShortSummary, goals is null ? null : Compress(goals!.Value)!);
        }

        return person;
    }

    private static List<WorkExperienceEntry> MapWorkExperience(XDocument doc)
    {
        var outcomes = new List<WorkExperienceEntry>();
        foreach (var node in ElementsNamed(doc.Descendants(), "WorkExperience", "WorkHistory"))
        {
            var entry = new WorkExperienceEntry();

            entry.JobTitle = Pick(entry.JobTitle, LocateText(node, "Position", "Profession", "OccupationProfile", "Title"));

            entry.Company = Pick(entry.Company,
                NestedLocate(node, ["Employer", "EmployerContactInfo"], ["Name", "Organisation"])
                ?? LocateText(node, "EmployerName"));

            entry.Location = Pick(entry.Location, LocateText(node, "Town", "Municipality", "City"));
            entry.Description = Pick(entry.Description,
                LocateText(node, "ActivitiesAndResponsibilities", "Activities", "Mission"));

            entry.Achievements = Pick(entry.Achievements, LocateText(node, "Achievement"));
            entry.Technologies = Pick(entry.Technologies, LocateText(node, "Sector"));

            var timeframe = ElementsNamed(node.Descendants(), "Period", "TimeInterval").FirstOrDefault();

            AnnotateYears(entry,
                timeframe is null ? string.Empty : Compress(timeframe!.ToString()));

            if (entry.HasUserInput())
            {
                outcomes.Add(entry);
            }
        }

        return outcomes;
    }

    private static void AnnotateYears(WorkExperienceEntry entry, string span)
    {
        if (span.Length == 0)
        {
            return;
        }

        entry.IsCurrentlyWorking |= span.Contains("present", StringComparison.OrdinalIgnoreCase);

        var window = SortedYears(span);
        AssignSpanYears(entry, window.ToArray());

        static void AssignSpanYears(WorkExperienceEntry work, int[] chronological)
        {
            if (chronological.Length == 0)
            {
                return;
            }

            if (chronological.Length == 1)
            {
                work.StartYear = chronological[0];

                return;
            }

            work.StartYear = chronological[0];

            work.EndYear = chronological[^1];
        }
    }

    private static List<EducationEntry> MapEducation(XDocument doc)
    {
        var rows = new List<EducationEntry>();
        foreach (var edu in ElementsNamed(doc.Descendants(), "Education"))
        {
            var entry = new EducationEntry();

            entry.Institution =
                Pick(entry.Institution,
                    NestedLocate(edu, ["Organisation", "School"], ["Name"]) ?? string.Empty);

            entry.Degree = Pick(entry.Degree, LocateText(edu, "EducationLevelEnum", "Level", "Title"));

            entry.FieldOfStudy = Pick(entry.FieldOfStudy, LocateText(edu, "Field", "Subjects"));

            entry.Location = Pick(entry.Location, LocateText(edu, "Country", "Town"));

            entry.Description = Pick(entry.Description,
                LocateText(edu, "ActivitiesAndResponsibilities", "Activities"));

            var timeframe = ElementsNamed(edu.Descendants(), "Period").FirstOrDefault();
            if (timeframe != null)
            {
                var span = Compress(timeframe.ToString());
                entry.IsCurrentlyStudying |= span.Contains("present", StringComparison.OrdinalIgnoreCase);

                AssignEducationYears(entry, SortedYears(span).ToArray());
            }

            if (entry.HasUserInput())
            {
                rows.Add(entry);
            }
        }

        return rows;
    }

    private static void AssignEducationYears(EducationEntry entry, int[] chronological)
    {
        if (chronological.Length == 0)
        {
            return;
        }

        if (chronological.Length == 1)
        {
            entry.StartYear = chronological[0];

            return;
        }

        entry.StartYear = chronological[0];

        entry.EndYear = chronological[^1];
    }

    private static List<LanguageEntry> MapLanguages(XDocument doc)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<LanguageEntry>();

        foreach (var node in ElementsNamed(doc.Descendants(), "ForeignLanguage", "MotherTongue"))
        {
            var languageChild = ElementsNamed(node.Descendants(), "Language").FirstOrDefault();
            string? moniker =
                LocateTextNullable(node, "Description", "Name")
                ?? (languageChild is null ? null : LocateTextNullable(languageChild!, "Description", "Name"));

            moniker ??= FirstToken(node.Value);

            moniker = moniker is null ? null : Compress(moniker);

            if (string.IsNullOrWhiteSpace(moniker) || !seen.Add(moniker.Trim()))
            {
                continue;
            }

            list.Add(new LanguageEntry { Language = moniker.Trim() });
        }

        return list;
    }

    private static List<CertificateEntry> MapCertificates(XDocument doc)
    {
        var entries = new List<CertificateEntry>();
        foreach (var node in ElementsNamed(doc.Descendants(), "Achievement", "Certificate", "Diploma"))
        {
            var title =
                LocateTextNullable(node, "Title", "AwardingBody", "Name");

            title ??= FirstToken(node.Value);

            title = title is null ? null : Compress(title);

            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var entry = new CertificateEntry
            {
                Name = title.Trim(),
                Issuer = LocateText(node, "OrganisationName", "Organisation", "Issuer"),

                Description = LocateText(node, "Activities", "Location")
            };

            var yearsWindow = SortedYears($"{node}");

            foreach (var year in yearsWindow)
            {
                entry.IssueYear = year;
                break;
            }

            if (entry.HasUserInput())
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private static List<SkillsGroupEntry> MapSkills(XDocument doc)
    {
        var buckets = new Dictionary<string, SkillsGroupEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in ElementsNamed(doc.Descendants(), "Skill"))
        {
            var tag =
                LocateTextNullable(skill, "Title", "Description", "Name");

            tag ??= FirstToken(skill.Value);

            tag = tag is null ? null : Compress(tag);

            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            var heading = LocateSkillHeading(skill);

            heading = heading.Length > 0 ? heading : "General";

            buckets.TryAdd(heading, new SkillsGroupEntry { Category = heading });
            buckets[heading].Skills.Add(new SkillItem { Name = tag.Trim() });
        }

        return buckets.Values.ToList();
    }

    private static string LocateSkillHeading(XElement leaf)
    {
        var cursor = leaf.Parent;
        while (cursor is not null)
        {
            if (!cursor.Name.LocalName.Equals("Skill", StringComparison.OrdinalIgnoreCase)
                && SkillHeadings.Contains(cursor.Name.LocalName))
            {
                return cursor.Name.LocalName;
            }

            cursor = cursor.Parent;
        }

        return string.Empty;
    }

    private static string ConcatenateMisc(XDocument doc)
    {
        var lines = new List<string>();

        foreach (var marker in MiscellaneousLocals)
        {
            foreach (var section in ElementsNamed(doc.Descendants(), marker))
            {
                lines.Add($"{marker}: {Compress(section!.ToString())}");
            }
        }

        return lines.Count == 0 ? string.Empty : string.Join($"{Environment.NewLine}{Environment.NewLine}", lines);
    }

    private static List<ImportedFieldConfidence> TagPersonal(PersonalInformationImport person)
    {
        var sink = new List<ImportedFieldConfidence>();

        Confidence(sink, MainPersonalInformationFieldKeys.FirstName, person.FirstName);

        Confidence(sink, MainPersonalInformationFieldKeys.LastName, person.LastName);

        Confidence(sink, MainPersonalInformationFieldKeys.Email, person.Email);

        Confidence(sink, MainPersonalInformationFieldKeys.Phone, person.Phone);

        Confidence(sink, MainPersonalInformationFieldKeys.Location, person.Location);

        Confidence(sink, MainPersonalInformationFieldKeys.ProfessionalTitle, person.ProfessionalTitle);

        Confidence(sink, MainPersonalInformationFieldKeys.ShortSummary, person.ShortSummary);

        return sink;
    }

    private static void Confidence(List<ImportedFieldConfidence> sink, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sink.Add(CvStructuredImportMapper.Field(key, CvImportConfidence.Medium));
        }
    }

    private static IEnumerable<XElement> ElementsNamed(IEnumerable<XElement> sequence, params string[] locals)
        => sequence.Where(e => locals.Contains(e.Name.LocalName, StringComparer.OrdinalIgnoreCase));

    private static bool LocalEquals(string actual, string expected)
        => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<int> SortedYears(string blob)
        => YearPattern.Matches(blob)
            .Cast<Match>()
            .Select(m => int.Parse(m.Value.AsSpan(), NumberStyles.Integer, CultureInfo.InvariantCulture))
            .Where(y => y is > 1950 and < 2150)
            .Distinct()
            .OrderBy(y => y);

    private static string Pick(string incumbent, string? candidate)
        => string.IsNullOrWhiteSpace(candidate)
            ? incumbent
            : string.IsNullOrWhiteSpace(incumbent)
                ? candidate.Trim()
                : incumbent;

    private static string LocateText(XElement anchor, params string[] locals)
        => LocateTextNullable(anchor, locals) ?? string.Empty;

    private static string? LocateTextNullable(XElement anchor, params string[] locals)
    {
        IEnumerable<XElement> Walk()
        {
            yield return anchor;
            foreach (var hit in anchor.Descendants())
            {
                yield return hit;
            }
        }

        foreach (var name in locals)
        {
            var element = Walk().FirstOrDefault(e =>
                LocalEquals(e!.Name.LocalName, name)
                && !string.IsNullOrWhiteSpace(e.Value));

            if (element is not null)
            {
                return Compress(element!.Value);
            }
        }

        return null;
    }

    private static string? NestedLocate(XElement envelope, IEnumerable<string> containers, IEnumerable<string> leaves)
    {
        foreach (var container in containers)
        {
            var shell = ElementsNamed(envelope.Descendants(), container).FirstOrDefault();
            shell ??= ElementsNamed(envelope.Elements(), container).FirstOrDefault();

            if (shell is null)
            {
                continue;
            }

            foreach (var leaf in leaves)
            {
                foreach (var hit in shell.Descendants().Prepend(shell))
                {
                    if (LocalEquals(hit.Name.LocalName, leaf!) && !string.IsNullOrWhiteSpace(hit.Value))
                    {
                        return Compress(hit.Value);
                    }
                }
            }
        }

        foreach (var fallback in leaves)
        {
            var textual = LocateTextNullable(envelope, fallback);

            if (!string.IsNullOrWhiteSpace(textual))
            {
                return textual;
            }
        }

        return null;
    }

    private static string Compress(string corpus)
        => Regex.Replace(corpus, @"\s+", " ").Trim();

    private static string? FirstToken(string input)
        => string.IsNullOrWhiteSpace(input) ? null :
            Compress(input.Split('|', '/', StringSplitOptions.RemoveEmptyEntries).First()).Trim();

}
