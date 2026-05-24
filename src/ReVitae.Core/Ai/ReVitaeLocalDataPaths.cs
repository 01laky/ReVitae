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
}
