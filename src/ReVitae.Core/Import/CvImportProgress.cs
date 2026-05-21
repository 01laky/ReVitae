namespace ReVitae.Core.Import;

/// <summary>Reports coarse import stages to the UI (translation keys).</summary>
public static class CvImportProgress
{
    public static event Action<string>? StatusChanged;

    internal static void Report(string translationKey)
    {
        CvImportDiagnosticsLogger.LogStep("progress", translationKey);
        StatusChanged?.Invoke(translationKey);
    }
}
