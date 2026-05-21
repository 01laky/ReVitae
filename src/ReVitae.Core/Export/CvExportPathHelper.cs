namespace ReVitae.Core.Export;

public static class CvExportPathHelper
{
    public static bool IsExistingFile(string? path) =>
        !string.IsNullOrWhiteSpace(path) && File.Exists(path);
}
