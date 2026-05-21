using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ReVitae.Core.Cv;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Structured;

/// <summary>Maps CSV/TSV header + first data row into <see cref="PersonalInformationImport"/>.</summary>
public static class TabularCvMapper
{
    public static CvImportResult Map(string rawText, bool tabDelimited)
    {
        char delimiter = tabDelimited ? '\t' : ',';
        var lines = NonEmptyLines(rawText).ToArray();
        if (lines.Length < 2)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorNoStructuredData);
        }

        var header = SplitRow(lines[0], delimiter);
        if (header.Count == 0 || header.All(string.IsNullOrWhiteSpace))
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorNoStructuredData);
        }

        var cells = Pad(SplitRow(lines[1], delimiter), header.Count);
        var personal = new PersonalInformationImport();
        var notes = new StringBuilder();

        for (var i = 0; i < header.Count; i++)
        {
            MapField(CleanHeader(header[i]), cells[i], personal, notes);
        }

        var appendix = notes.Length == 0 ? string.Empty : CvTextNormalizer.Normalize(notes.ToString()).Trim();
        if (!CvStructuredImportMapper.HasImportableCvData(personal, [], [], [], [], [], [], [], appendix))
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorNoStructuredData);
        }

        var warnings = lines.Length > 2
            ? new List<CvImportWarning>
                { CvStructuredImportMapper.Warning(TranslationKeys.ImportWarningTabularMultipleRowsIgnored) }
            : [];

        return new CvImportResult
        {
            Success = true,
            Personal = personal,
            AdditionalInformationContent = appendix,
            SectionHasData = CvStructuredImportMapper.SectionHasData(personal, [], [], [], [], [], [], [], appendix),
            FieldConfidences = BuildConfidence(personal),
            Warnings = warnings
        };
    }

    private static IEnumerable<string> NonEmptyLines(string corpus)
    {
        foreach (var line in (corpus ?? string.Empty).Replace('\r', '\n').Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0)
            {
                yield return trimmed;
            }
        }
    }

    private static List<string> SplitRow(string row, char delimiter)
    {
        var pieces = row.Split(delimiter, StringSplitOptions.TrimEntries);
        return pieces.Select(StripQuotes).ToList();
    }

    private static string StripQuotes(string token)
    {
        token = token.Trim();
        if ((token.StartsWith('"') && token.EndsWith('"')) ||
            (token.StartsWith('\'') && token.EndsWith('\'')))
        {
            return token[1..^1].Trim();
        }

        return token;
    }

    private static List<string> Pad(IReadOnlyList<string> source, int columns)
    {
        var buffer = source.ToList();
        while (buffer.Count < columns)
        {
            buffer.Add(string.Empty);
        }

        while (buffer.Count > columns)
        {
            buffer.RemoveAt(buffer.Count - 1);
        }

        return buffer;
    }

    private static string CleanHeader(string header)
    {
        header = header.ToLowerInvariant().Trim();
        header = Regex.Replace(header, @"[^a-z0-9]+", " ");
        return Regex.Replace(header, @"\s+", " ").Trim();
    }

    private static void MapField(string header, string rawValue, PersonalInformationImport personal, StringBuilder appendix)
    {
        if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        var text = Regex.Replace(rawValue.Trim(), @"\s+", " ");

        switch (header)
        {
            case "firstname":
            case "given name":
                personal.FirstName = MergeIfAbsent(personal.FirstName, text);
                break;
            case "lastname":
            case "surname":
            case "family name":
                personal.LastName = MergeIfAbsent(personal.LastName, text);
                break;
            case "name":
            case "full name":
            case "candidate name":
                ApplyFullName(personal, text);
                break;
            case "email":
            case "e mail":
                personal.Email = MergeIfAbsent(personal.Email, text);
                break;
            case "phone":
            case "telephone":
            case "mobile":
            case "tel":
                personal.Phone = MergeIfAbsent(personal.Phone, text);
                break;
            case "location":
            case "city":
            case "address":
                personal.Location = MergeIfAbsent(personal.Location, text);
                break;
            case "title":
            case "headline":
            case "professional title":
                personal.ProfessionalTitle = MergeIfAbsent(personal.ProfessionalTitle, text);
                break;
            case "summary":
            case "objective":
            case "bio":
                personal.ShortSummary = MergeIfAbsent(personal.ShortSummary, text);
                break;
            default:
                if (appendix.Length > 0)
                {
                    appendix.Append(Environment.NewLine);
                }

                appendix.Append(CultureInfo.InvariantCulture.TextInfo.ToTitleCase(header)).Append(": ").Append(text);
                break;
        }
    }

    private static string MergeIfAbsent(string current, string value)
        => string.IsNullOrWhiteSpace(current) && !string.IsNullOrWhiteSpace(value) ? value : current;

    private static void ApplyFullName(PersonalInformationImport personal, string incoming)
    {
        var parts = incoming.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return;
        }

        personal.FirstName = MergeIfAbsent(personal.FirstName, parts[0]);

        switch (parts.Length)
        {


            case > 1:


                personal.LastName = MergeIfAbsent(personal.LastName, string.Join(' ', parts.Skip(1)));



                break;


        }


    }

    private static List<ImportedFieldConfidence> BuildConfidence(PersonalInformationImport personal)
    {
        var bucket = new List<ImportedFieldConfidence>();
        Tag(bucket, MainPersonalInformationFieldKeys.FirstName, personal.FirstName);
        Tag(bucket, MainPersonalInformationFieldKeys.LastName, personal.LastName);
        Tag(bucket, MainPersonalInformationFieldKeys.Email, personal.Email);
        Tag(bucket, MainPersonalInformationFieldKeys.Phone, personal.Phone);
        Tag(bucket, MainPersonalInformationFieldKeys.Location, personal.Location);
        Tag(bucket, MainPersonalInformationFieldKeys.ProfessionalTitle, personal.ProfessionalTitle);
        Tag(bucket, MainPersonalInformationFieldKeys.ShortSummary, personal.ShortSummary);
        return bucket;
    }

    private static void Tag(List<ImportedFieldConfidence> sink, string descriptor, string candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            sink.Add(CvStructuredImportMapper.Field(descriptor, CvImportConfidence.Medium));
        }
    }
}
