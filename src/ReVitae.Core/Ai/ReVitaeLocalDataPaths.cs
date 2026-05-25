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

	public static string GetAiSecretsFilePath()
	{
		return Path.Combine(GetReVitaeRootDirectory(), "ai-secrets.enc");
	}

	public static string GetAiSecretsKeyFilePath()
	{
		return Path.Combine(GetReVitaeRootDirectory(), "ai-secrets.key");
	}

	public static string GetProjectAutosaveRecoveryPath()
	{
		return Path.Combine(GetReVitaeRootDirectory(), "autosave.recovery.revitae.json");
	}

	public static string GetAppSettingsFilePath()
	{
		return Path.Combine(GetReVitaeRootDirectory(), "app-settings.json");
	}

	public static string GetTessdataDirectory() =>
		Path.Combine(GetReVitaeRootDirectory(), "tessdata");

	public static string GetProfilePhotosDirectory() =>
		Path.Combine(GetReVitaeRootDirectory(), "profile-photos");

	public static string GetImportDebugLogPath() =>
		Path.Combine(GetReVitaeRootDirectory(), "import-debug.log");

	public static string GetRecentProjectsPath() =>
		Path.Combine(GetReVitaeRootDirectory(), "recent-projects.json");
}
