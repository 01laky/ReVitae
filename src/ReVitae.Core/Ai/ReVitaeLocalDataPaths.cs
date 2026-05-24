namespace ReVitae.Core.Ai;

public static class ReVitaeLocalDataPaths
{
    public static string GetReVitaeRootDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReVitae");
    }

    public static string GetAiSettingsFilePath()
    {
        return Path.Combine(GetReVitaeRootDirectory(), "ai-settings.json");
    }

    public static string GetAiDownloadJobFilePath()
    {
        return Path.Combine(GetReVitaeRootDirectory(), "ai-download-job.json");
    }
}
