using System.Diagnostics;
using System.Text;
using ReVitae.Core.Import.Extraction;

namespace ReVitae.Core.Import;

/// <summary>Writes a human-readable import parse trace to a local log file for debugging.</summary>
internal static class CvImportDiagnosticsLogger
{
    private static readonly object Gate = new();
    private static Stopwatch? _sessionStopwatch;

    public static string LogFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ReVitae",
        "import-debug.log");

    public static bool IsEnabled =>
        !string.Equals(Environment.GetEnvironmentVariable("REVITAE_IMPORT_DEBUG"), "0", StringComparison.Ordinal);

    public static void BeginSession(string filePath, CvImportFormat format, long fileSizeBytes)
    {
        if (!IsEnabled)
        {
            return;
        }

        _sessionStopwatch = Stopwatch.StartNew();
        var header = new StringBuilder()
            .AppendLine(new string('=', 80))
            .AppendLine($"ReVitae import debug — {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}")
            .AppendLine($"File: {filePath}")
            .AppendLine($"Format: {format}")
            .AppendLine($"Size: {fileSizeBytes} bytes")
            .AppendLine($"Log: {LogFilePath}")
            .AppendLine(new string('=', 80))
            .ToString();

        Write(header);
        Trace.WriteLine($"[ReVitae import debug] logging to {LogFilePath}");
        Console.Error.WriteLine($"[ReVitae import debug] logging to {LogFilePath}");
    }

    public static void LogExtraction(CvTextExtractionResult extraction)
    {
        if (!IsEnabled)
        {
            return;
        }

        var builder = new StringBuilder()
            .AppendLine()
            .AppendLine("--- 1. Text extraction ---")
            .AppendLine($"Success: {extraction.Success}");

        if (extraction.Strategy is { } strategy)
        {
            builder.AppendLine($"Strategy: {strategy}");
        }

        if (!string.IsNullOrWhiteSpace(extraction.OcrEngineName))
        {
            builder.AppendLine($"OcrEngine: {extraction.OcrEngineName} (debug only)");
        }

        if (!string.IsNullOrWhiteSpace(extraction.OcrLanguages))
        {
            builder.AppendLine($"OcrLanguages: {extraction.OcrLanguages}");
        }

        if (extraction.PageCount is { } pageCount)
        {
            builder.AppendLine($"Pages: {pageCount}");
        }

        if (extraction.ErrorMessageKey is { } errorKey)
        {
            builder.AppendLine($"Error key: {errorKey}");
        }

        if (extraction.HyperlinkUrls is { Count: > 0 } links)
        {
            builder.AppendLine($"Hyperlinks ({links.Count}):");
            foreach (var link in links.Take(20))
            {
                builder.AppendLine($"  - {link}");
            }

            if (links.Count > 20)
            {
                builder.AppendLine($"  ... and {links.Count - 20} more");
            }
        }

        if (extraction.Warnings is { Count: > 0 } warnings)
        {
            builder.AppendLine($"Extraction warnings ({warnings.Count}):");
            foreach (var warning in warnings)
            {
                builder.AppendLine($"  - {warning.MessageKey}");
            }
        }

        builder.AppendLine($"Text length: {extraction.Text.Length} chars");
        AppendTextPreview(builder, "Extracted text preview", extraction.Text, maxChars: 2_000);
        AppendFullText(builder, "Full extracted text", extraction.Text);

        Write(builder.ToString());
    }

    public static void LogTextPipeline(string rawText, CvSegmentationResult segmentation, CvImportResult result)
    {
        if (!IsEnabled)
        {
            return;
        }

        var normalized = CvTextNormalizer.Normalize(rawText);
        var builder = new StringBuilder()
            .AppendLine()
            .AppendLine("--- 2. Normalization ---")
            .AppendLine($"Raw length: {rawText.Length} chars")
            .AppendLine($"Normalized length: {normalized.Length} chars");

        AppendTextPreview(builder, "Normalized text preview", normalized, maxChars: 2_000);

        builder.AppendLine()
            .AppendLine("--- 3. Section segmentation ---")
            .AppendLine($"Header block: {segmentation.HeaderBlock.Length} chars");

        if (!string.IsNullOrWhiteSpace(segmentation.HeaderBlock))
        {
            AppendIndentedBlock(builder, segmentation.HeaderBlock, maxChars: 1_500);
        }
        else
        {
            builder.AppendLine("  (empty)");
        }

        if (segmentation.Warnings is { Count: > 0 } segmentationWarnings)
        {
            builder.AppendLine($"Segmentation warnings ({segmentationWarnings.Count}):");
            foreach (var warning in segmentationWarnings)
            {
                builder.AppendLine($"  - {warning.MessageKey}");
            }
        }

        builder.AppendLine($"Detected sections: {segmentation.SectionBodies.Count}");
        foreach (var (sectionId, body) in segmentation.SectionBodies.OrderBy(pair => pair.Key.ToString()))
        {
            var lineCount = body.Split('\n', StringSplitOptions.None).Length;
            builder.AppendLine()
                .AppendLine($"[{sectionId}] {body.Length} chars, {lineCount} lines");
            AppendIndentedBlock(builder, body, maxChars: 1_000);
        }

        AppendParsedResult(builder, result);
        Write(builder.ToString());
    }

    public static void LogStructuredResult(CvImportResult result)
    {
        if (!IsEnabled)
        {
            return;
        }

        var builder = new StringBuilder()
            .AppendLine()
            .AppendLine("--- Structured import (no text pipeline) ---");

        AppendParsedResult(builder, result);
        Write(builder.ToString());
    }

    public static void LogFailure(string stage, string? detail = null)
    {
        if (!IsEnabled)
        {
            return;
        }

        var builder = new StringBuilder()
            .AppendLine()
            .AppendLine($"--- FAILED at {stage} ---");

        if (!string.IsNullOrWhiteSpace(detail))
        {
            builder.AppendLine(detail);
        }

        Write(builder.ToString());
        EchoToConsole($"FAILED at {stage}" + (detail is null ? string.Empty : $": {detail}"));
    }

    /// <summary>Append a single trace line to the debug log (and stderr when enabled).</summary>
    public static void LogStep(string section, string message)
    {
        if (!IsEnabled)
        {
            return;
        }

        var timestamp = DateTimeOffset.Now.ToString("HH:mm:ss.fff");
        var line = $"[{timestamp}] [{section}] {message}";
        Write(line + Environment.NewLine);
        EchoToConsole(line);
    }

    public static void EndSession(bool success)
    {
        if (!IsEnabled)
        {
            return;
        }

        var elapsed = _sessionStopwatch?.ElapsedMilliseconds ?? 0;
        _sessionStopwatch = null;

        var footer = new StringBuilder()
            .AppendLine(new string('=', 80))
            .AppendLine($"Import finished in {elapsed} ms — {(success ? "SUCCESS" : "FAILED")}")
            .AppendLine(new string('=', 80))
            .AppendLine()
            .ToString();

        Write(footer);
    }

    private static void AppendParsedResult(StringBuilder builder, CvImportResult result)
    {
        builder.AppendLine()
            .AppendLine("--- 4. Parsed result ---")
            .AppendLine($"Success: {result.Success}");

        if (result.ErrorMessageKey is { } errorKey)
        {
            builder.AppendLine($"Error key: {errorKey}");
        }

        builder.AppendLine("Personal:")
            .AppendLine($"  First name: {ValueOrDash(result.Personal.FirstName)}")
            .AppendLine($"  Last name: {ValueOrDash(result.Personal.LastName)}")
            .AppendLine($"  Title: {ValueOrDash(result.Personal.ProfessionalTitle)}")
            .AppendLine($"  Email: {ValueOrDash(result.Personal.Email)}")
            .AppendLine($"  Phone: {ValueOrDash(result.Personal.Phone)}")
            .AppendLine($"  Location: {ValueOrDash(result.Personal.Location)}")
            .AppendLine($"  LinkedIn: {ValueOrDash(result.Personal.LinkedInUrl)}")
            .AppendLine($"  GitHub: {ValueOrDash(result.Personal.GitHubUrl)}")
            .AppendLine($"  Summary length: {result.Personal.ShortSummary?.Length ?? 0} chars");

        builder.AppendLine($"Work experience ({result.WorkExperienceEntries.Count} entries):");
        foreach (var (entry, index) in result.WorkExperienceEntries.Select((entry, index) => (entry, index + 1)))
        {
            builder.AppendLine(
                $"  [{index}] {ValueOrDash(entry.JobTitle)} @ {ValueOrDash(entry.Company)} " +
                $"({FormatMonthYear(entry.StartMonth, entry.StartYear)} – {FormatEnd(entry)})");
        }

        builder.AppendLine($"Education ({result.EducationEntries.Count} entries):");
        foreach (var (entry, index) in result.EducationEntries.Select((entry, index) => (entry, index + 1)))
        {
            builder.AppendLine(
                $"  [{index}] {ValueOrDash(entry.Degree)} @ {ValueOrDash(entry.Institution)}");
        }

        var skillCount = result.SkillsGroups.Sum(group => group.Skills.Count);
        builder.AppendLine($"Skills ({result.SkillsGroups.Count} groups, {skillCount} skills):");
        foreach (var group in result.SkillsGroups.Take(12))
        {
            var names = string.Join(", ", group.Skills.Take(8).Select(skill => skill.Name));
            if (group.Skills.Count > 8)
            {
                names += $", ... +{group.Skills.Count - 8}";
            }

            builder.AppendLine($"  {ValueOrDash(group.Category)}: {names}");
        }

        if (result.SkillsGroups.Count > 12)
        {
            builder.AppendLine($"  ... and {result.SkillsGroups.Count - 12} more groups");
        }

        builder.AppendLine($"Languages ({result.LanguageEntries.Count}):");
        foreach (var language in result.LanguageEntries.Take(15))
        {
            builder.AppendLine($"  - {ValueOrDash(language.Language)} ({language.Proficiency})");
        }

        if (result.LanguageEntries.Count > 15)
        {
            builder.AppendLine($"  ... and {result.LanguageEntries.Count - 15} more");
        }

        builder.AppendLine($"Certificates: {result.CertificateEntries.Count}");
        builder.AppendLine($"Projects: {result.ProjectEntries.Count}");
        builder.AppendLine($"Links: {result.LinkEntries.Count}");
        builder.AppendLine($"Additional info length: {result.AdditionalInformationContent?.Length ?? 0} chars");

        if (result.SectionHasData is { Count: > 0 } sectionFlags)
        {
            builder.AppendLine("Section flags:");
            foreach (var (sectionId, hasData) in sectionFlags.OrderBy(pair => pair.Key.ToString()))
            {
                builder.AppendLine($"  {sectionId}: {hasData}");
            }
        }

        if (result.Warnings is { Count: > 0 } warnings)
        {
            builder.AppendLine($"Warnings ({warnings.Count}):");
            foreach (var warning in warnings)
            {
                builder.AppendLine($"  - {warning.MessageKey}");
            }
        }

        if (result.FieldConfidences is { Count: > 0 } confidences)
        {
            var lowOrMedium = confidences
                .Where(item => item.Confidence != CvImportConfidence.High)
                .Take(25)
                .ToArray();
            if (lowOrMedium.Length > 0)
            {
                builder.AppendLine($"Uncertain fields ({lowOrMedium.Length} shown):");
                foreach (var item in lowOrMedium)
                {
                    builder.AppendLine($"  - {item.FieldKey}: {item.Confidence}");
                }
            }
        }
    }

    private static void AppendTextPreview(StringBuilder builder, string title, string text, int maxChars)
    {
        builder.AppendLine($"{title} (first {Math.Min(maxChars, text.Length)} chars):");
        AppendIndentedBlock(builder, text, maxChars);
    }

    private static void AppendFullText(StringBuilder builder, string title, string text)
    {
        builder.AppendLine().AppendLine($"--- {title} ---");
        builder.AppendLine(text);
    }

    private static void AppendIndentedBlock(StringBuilder builder, string text, int maxChars)
    {
        var slice = text.Length <= maxChars ? text : text[..maxChars] + $"\n... [{text.Length - maxChars} more chars truncated]";
        foreach (var line in slice.Split('\n'))
        {
            builder.Append("  ").AppendLine(line);
        }
    }

    private static string ValueOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    private static string FormatMonthYear(int? month, int? year) =>
        month is { } m && year is { } y ? $"{m:D2}/{y}" : year?.ToString() ?? "—";

    private static string FormatEnd(Cv.WorkExperience.WorkExperienceEntry entry) =>
        entry.IsCurrentlyWorking ? "Present" : FormatMonthYear(entry.EndMonth, entry.EndYear);

    private static void Write(string content)
    {
        lock (Gate)
        {
            var directory = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(LogFilePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    private static void EchoToConsole(string line)
    {
        Trace.WriteLine($"[ReVitae import debug] {line}");
        Console.Error.WriteLine($"[ReVitae import debug] {line}");
    }
}
