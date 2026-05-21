using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Import;
using CvWorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;
using CvEducationEntry = ReVitae.Core.Cv.Education.EducationEntry;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ReVitae.Core.Export;

internal static class CvStructuredExportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static void WriteRevitaeJson(CvExportSourceData source, Stream output)
    {
        var dto = BuildRevitaeDto(source);
        using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true });
        JsonSerializer.Serialize(writer, dto, JsonOptions);
        writer.Flush();
    }

    public static void WriteJsonResume(CvExportSourceData source, Stream output)
    {
        var dto = BuildJsonResumeDto(source);
        using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true });
        JsonSerializer.Serialize(writer, dto, JsonOptions);
        writer.Flush();
    }

    public static void WriteYaml(CvExportSourceData source, Stream output)
    {
        var dto = BuildRevitaeDto(source);
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var plainRoot = JsonElementToPlainObject(document.RootElement);

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithDefaultScalarStyle(ScalarStyle.DoubleQuoted)
            .Build();
        using var textWriter = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        textWriter.Write(serializer.Serialize(plainRoot));
        textWriter.Flush();
    }

    public static void WriteEuropassXml(CvExportSourceData source, Stream output)
    {
        var doc = BuildEuropassDocument(source);
        var settings = SecureXmlWriterSettings();
        using var writer = XmlWriter.Create(output, settings);
        doc.Save(writer);
    }

    public static void WriteHrXml(CvExportSourceData source, Stream output)
    {
        var doc = BuildHrXmlDocument(source);
        var settings = SecureXmlWriterSettings();
        using var writer = XmlWriter.Create(output, settings);
        doc.Save(writer);
    }

    public static void WriteCsv(CvExportSourceData source, Stream output, char delimiter)
    {
        using var writer = new StreamWriter(output, new UTF8Encoding(true), leaveOpen: true);
        var headers = new[] { "firstName", "lastName", "email", "phone", "location", "professionalTitle", "summary" };
        writer.WriteLine(string.Join(delimiter, headers));
        var p = source.Personal;
        var row = new[]
        {
            EscapeCsv(p.FirstName, delimiter),
            EscapeCsv(p.LastName, delimiter),
            EscapeCsv(p.Email, delimiter),
            EscapeCsv(p.Phone, delimiter),
            EscapeCsv(p.Location, delimiter),
            EscapeCsv(p.ProfessionalTitle, delimiter),
            EscapeCsv(p.ShortSummary, delimiter)
        };
        writer.WriteLine(string.Join(delimiter, row));
        writer.Flush();
    }

    private static object BuildRevitaeDto(CvExportSourceData source)
    {
        var photoBytes = ProfilePhotoBytes.TryRead(source.Personal.ProfilePhotoPath);
        var hasPhoto = photoBytes is { Length: > 0 };
        var root = new Dictionary<string, object?> { ["revitaeVersion"] = hasPhoto ? 2 : 1 };

        if (HasPersonal(source.Personal) || hasPhoto)
        {
            root["personalInformation"] = BuildPersonalInformationDto(source.Personal, photoBytes);
        }

        if (source.WorkExperience.Count > 0)
        {
            root["workExperience"] = source.WorkExperience;
        }

        if (source.Education.Count > 0)
        {
            root["education"] = source.Education;
        }

        if (source.Skills.Count > 0)
        {
            root["skills"] = source.Skills.Select(g => new
            {
                category = g.Category,
                skills = g.Skills.Select(s => new { name = s.Name }).ToArray()
            }).ToArray();
        }

        if (source.Languages.Count > 0)
        {
            root["languages"] = source.Languages;
        }

        if (source.Certificates.Count > 0)
        {
            root["certificates"] = source.Certificates;
        }

        if (source.Projects.Count > 0)
        {
            root["projects"] = source.Projects;
        }

        if (source.Links.Count > 0)
        {
            root["links"] = source.Links;
        }

        if (!string.IsNullOrWhiteSpace(source.AdditionalInformation))
        {
            root["additionalInformation"] = new { content = source.AdditionalInformation };
        }

        return root;
    }

    private static object BuildJsonResumeDto(CvExportSourceData source)
    {
        var p = source.Personal;
        var profiles = new List<object>();
        if (!string.IsNullOrWhiteSpace(p.LinkedInUrl))
        {
            profiles.Add(new { network = "LinkedIn", url = p.LinkedInUrl });
        }

        if (!string.IsNullOrWhiteSpace(p.GitHubUrl))
        {
            profiles.Add(new { network = "GitHub", url = p.GitHubUrl });
        }

        return new
        {
            schema = "https://jsonresume.org/schema/1.0.0",
            basics = new
            {
                name = $"{p.FirstName} {p.LastName}".Trim(),
                label = p.ProfessionalTitle,
                email = p.Email,
                phone = p.Phone,
                url = p.PortfolioUrl,
                summary = p.ShortSummary,
                location = string.IsNullOrWhiteSpace(p.Location) ? null : new { city = p.Location },
                profiles = profiles.Count == 0 ? null : profiles
            },
            work = source.WorkExperience.Select(MapJsonResumeWork).ToArray(),
            education = source.Education.Select(MapJsonResumeEducation).ToArray(),
            skills = source.Skills.Select(g => new
            {
                name = g.Category,
                keywords = g.Skills.Select(s => s.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToArray()
            }).ToArray(),
            languages = source.Languages.Select(l => new { language = l.Language, fluency = l.Proficiency.ToString() }).ToArray(),
            certificates = source.Certificates.Select(c => new { name = c.Name, issuer = c.Issuer }).ToArray(),
            projects = source.Projects.Select(pj => new
            {
                name = pj.Name,
                description = pj.Description,
                url = pj.ProjectUrl,
                highlights = string.IsNullOrWhiteSpace(pj.Highlights) ? null : pj.Highlights.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            }).ToArray()
        };
    }

    private static object MapJsonResumeWork(CvWorkExperienceEntry entry) => new
    {
        position = entry.JobTitle,
        name = entry.Company,
        location = entry.Location,
        startDate = FormatIsoDate(entry.StartYear, entry.StartMonth),
        endDate = entry.IsCurrentlyWorking ? null : FormatIsoDate(entry.EndYear, entry.EndMonth),
        summary = entry.Description,
        highlights = string.IsNullOrWhiteSpace(entry.Achievements)
            ? null
            : entry.Achievements.Split('\n', StringSplitOptions.RemoveEmptyEntries)
    };

    private static object MapJsonResumeEducation(CvEducationEntry entry) => new
    {
        institution = entry.Institution,
        area = entry.FieldOfStudy,
        studyType = entry.Degree,
        startDate = FormatIsoDate(entry.StartYear, entry.StartMonth),
        endDate = FormatIsoDate(entry.EndYear, entry.EndMonth)
    };

    private static XDocument BuildEuropassDocument(CvExportSourceData source)
    {
        XNamespace esp = "http://europass.cedefop.europa.eu/Europass";
        var root = new XElement(esp + "SkillsPassport",
            new XElement(esp + "Identification",
                new XElement(esp + "PersonName",
                    new XElement(esp + "FirstName", source.Personal.FirstName),
                    new XElement(esp + "Surname", source.Personal.LastName)),
                new XElement(esp + "ContactInfo",
                    new XElement(esp + "Email", source.Personal.Email),
                    new XElement(esp + "Telephone", source.Personal.Phone)),
                new XElement(esp + "Headline", source.Personal.ProfessionalTitle)));

        foreach (var job in source.WorkExperience)
        {
            root.Add(new XElement(esp + "WorkExperience",
                new XElement(esp + "Position", job.JobTitle),
                new XElement(esp + "Employer", job.Company),
                new XElement(esp + "Period", job.BuildDateRangeLabel("Present"))));
        }

        foreach (var edu in source.Education)
        {
            root.Add(new XElement(esp + "Education",
                new XElement(esp + "Title", edu.Degree),
                new XElement(esp + "Organisation", edu.Institution)));
        }

        if (!string.IsNullOrWhiteSpace(source.Personal.ShortSummary))
        {
            root.Add(new XElement(esp + "OtherInfo", source.Personal.ShortSummary));
        }

        if (!string.IsNullOrWhiteSpace(source.AdditionalInformation))
        {
            root.Add(new XElement(esp + "AdditionalInformation", source.AdditionalInformation));
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
    }

    private static XDocument BuildHrXmlDocument(CvExportSourceData source)
    {
        XNamespace ns = "http://www.hr-xml.org/3";
        var root = new XElement(ns + "Resume",
            new XElement(ns + "Candidate",
                new XElement(ns + "PersonName",
                    new XElement(ns + "GivenName", source.Personal.FirstName),
                    new XElement(ns + "FamilyName", source.Personal.LastName)),
                new XElement(ns + "Communication",
                    new XElement(ns + "Email", source.Personal.Email),
                    new XElement(ns + "Telephone", source.Personal.Phone)),
                new XElement(ns + "ExecutiveSummary", source.Personal.ShortSummary)));

        foreach (var job in source.WorkExperience)
        {
            root.Add(new XElement(ns + "EmploymentHistory",
                new XElement(ns + "EmployerOrgName", job.Company),
                new XElement(ns + "PositionTitle", job.JobTitle)));
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
    }

    private static XmlWriterSettings SecureXmlWriterSettings() => new()
    {
        OmitXmlDeclaration = false,
        Indent = true,
        Encoding = new UTF8Encoding(false),
        ConformanceLevel = ConformanceLevel.Document
    };

    private static string? FormatIsoDate(int? year, int? month)
    {
        if (year is null)
        {
            return null;
        }

        if (month is null)
        {
            return year.Value.ToString(CultureInfo.InvariantCulture);
        }

        return $"{year.Value:0000}-{month.Value:00}";
    }

    private static Dictionary<string, object?> BuildPersonalInformationDto(
        PersonalInformationImport personal,
        byte[]? photoBytes)
    {
        var dto = new Dictionary<string, object?>
        {
            ["firstName"] = personal.FirstName,
            ["lastName"] = personal.LastName,
            ["professionalTitle"] = personal.ProfessionalTitle,
            ["email"] = personal.Email,
            ["phone"] = personal.Phone,
            ["location"] = personal.Location,
            ["linkedInUrl"] = personal.LinkedInUrl,
            ["portfolioUrl"] = personal.PortfolioUrl,
            ["gitHubUrl"] = personal.GitHubUrl,
            ["shortSummary"] = personal.ShortSummary
        };

        if (photoBytes is { Length: > 0 })
        {
            var contentType = ProfilePhotoFormats.GetContentTypeForExtension(
                Path.GetExtension(personal.ProfilePhotoPath));
            dto["profilePhotoBase64"] = Convert.ToBase64String(photoBytes);
            dto["profilePhotoContentType"] = contentType;
        }

        return dto;
    }

    private static bool HasPersonal(PersonalInformationImport personal) =>
        !string.IsNullOrWhiteSpace(personal.FirstName)
        || !string.IsNullOrWhiteSpace(personal.LastName)
        || !string.IsNullOrWhiteSpace(personal.Email)
        || !string.IsNullOrWhiteSpace(personal.ProfessionalTitle)
        || !string.IsNullOrWhiteSpace(personal.Phone)
        || !string.IsNullOrWhiteSpace(personal.Location)
        || !string.IsNullOrWhiteSpace(personal.LinkedInUrl)
        || !string.IsNullOrWhiteSpace(personal.PortfolioUrl)
        || !string.IsNullOrWhiteSpace(personal.GitHubUrl)
        || !string.IsNullOrWhiteSpace(personal.ShortSummary);

    private static object? JsonElementToPlainObject(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject()
            .ToDictionary(property => property.Name, property => JsonElementToPlainObject(property.Value)),
        JsonValueKind.Array => element.EnumerateArray()
            .Select(JsonElementToPlainObject)
            .ToArray(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => element.ToString()
    };

    private static string EscapeCsv(string? value, char delimiter)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains('\n') || text.Contains(delimiter))
        {
            return "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return text;
    }
}
