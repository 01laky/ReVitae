namespace ReVitae.Core.Import.Ocr;

internal static class TessdataLocator
{
    public static string? FindTessdataDirectory()
    {
        var candidates = BuildCandidatePaths();
        CvImportDiagnosticsLogger.LogStep("tessdata", $"Searching {candidates.Count} path(s) for eng.traineddata");

        foreach (var candidate in candidates)
        {
            var exists = Directory.Exists(candidate);
            var hasEng = exists && HasLanguageFile(candidate, "eng");
            CvImportDiagnosticsLogger.LogStep(
                "tessdata",
                $"  {(hasEng ? "FOUND" : "miss")}: {candidate}" +
                (exists && !hasEng ? " (dir exists, no eng.traineddata)" : string.Empty));

            if (hasEng)
            {
                return candidate;
            }
        }

        CvImportDiagnosticsLogger.LogStep("tessdata", "No tessdata directory found");
        return null;
    }

    public static bool HasLanguageFile(string tessdataDirectory, string languageCode)
    {
        var trainedDataPath = Path.Combine(tessdataDirectory, $"{languageCode}.traineddata");
        return File.Exists(trainedDataPath);
    }

    internal static IReadOnlyList<string> BuildCandidatePaths()
    {
        var candidates = new List<string>();

        var baseDirectory = AppContext.BaseDirectory;
        if (!string.IsNullOrWhiteSpace(baseDirectory))
        {
            candidates.Add(Path.Combine(baseDirectory, "tessdata"));
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            candidates.Add(Path.Combine(localAppData, "ReVitae", "tessdata"));
        }

        var tessdataPrefix = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
        if (!string.IsNullOrWhiteSpace(tessdataPrefix))
        {
            candidates.Add(tessdataPrefix.Trim());
        }

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
