namespace ReVitae.Core.Ai.Ollama;

public static class OllamaPaths
{
	public const long ManagedInstallReserveBytes = 512L * 1024 * 1024;

	public static string GetManagedInstallDirectory() =>
		Path.Combine(ReVitaeLocalDataPaths.GetReVitaeRootDirectory(), "ollama");

	public static string GetManagedMacAppBundlePath() =>
		Path.Combine(GetManagedInstallDirectory(), "Ollama.app");

	public static string GetManagedMacBinaryPath() =>
		Path.Combine(GetManagedMacAppBundlePath(), "Contents", "Resources", "ollama");

	public static string GetManagedMacAppExecutablePath() =>
		Path.Combine(GetManagedMacAppBundlePath(), "Contents", "MacOS", "Ollama");

	public static string GetManagedWindowsExePath() =>
		Path.Combine(GetManagedInstallDirectory(), "Ollama.exe");

	public static string GetManagedLinuxBinaryPath() =>
		Path.Combine(GetManagedInstallDirectory(), "bin", "ollama");

	public static bool IsManagedInstallPresent()
	{
		if (OperatingSystem.IsMacOS())
		{
			return Directory.Exists(GetManagedMacAppBundlePath());
		}

		if (OperatingSystem.IsWindows())
		{
			return File.Exists(GetManagedWindowsExePath());
		}

		if (OperatingSystem.IsLinux())
		{
			return File.Exists(GetManagedLinuxBinaryPath());
		}

		return false;
	}
}
