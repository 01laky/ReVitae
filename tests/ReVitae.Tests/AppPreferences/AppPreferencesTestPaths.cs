using ReVitae.Core.AppPreferences;

namespace ReVitae.Tests.AppPreferences;

internal sealed class AppPreferencesTestPaths : IDisposable
{
	public AppPreferencesTestPaths()
	{
		Root = Path.Combine(Path.GetTempPath(), "revitae-app-preferences-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Root);
		AppSettingsPath = Path.Combine(Root, "app-settings.json");
		AiSettingsPath = Path.Combine(Root, "ai-settings.json");
		AiDownloadJobPath = Path.Combine(Root, "ai-download-job.json");
	}

	public string Root { get; }

	public string AppSettingsPath { get; }

	public string AiSettingsPath { get; }

	public string AiDownloadJobPath { get; }

	public AppPreferencesRepository CreateRepository() =>
		new(AppSettingsPath, AiSettingsPath, AiDownloadJobPath);

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(Root))
			{
				Directory.Delete(Root, recursive: true);
			}
		}
		catch
		{
			// Best-effort temp cleanup.
		}
	}
}
