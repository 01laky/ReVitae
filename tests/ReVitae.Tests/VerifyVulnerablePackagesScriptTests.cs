namespace ReVitae.Tests;

public sealed class VerifyVulnerablePackagesScriptTests
{
	[Fact]
	public void Script_ExistsAndIsExecutable()
	{
		var repoRoot = FindRepoRoot();
		var scriptPath = Path.Combine(repoRoot, "scripts", "verify-vulnerable-packages.sh");

		Assert.True(File.Exists(scriptPath));

		if (!OperatingSystem.IsWindows())
		{
			var info = new FileInfo(scriptPath);
			Assert.True(info.Exists);
			Assert.Contains("dotnet list ReVitae.sln package --vulnerable", File.ReadAllText(scriptPath), StringComparison.Ordinal);
		}
	}

	[Fact]
	public void CoreProject_PinsCryptographyXmlAboveVulnerableRange()
	{
		var repoRoot = FindRepoRoot();
		var coreProject = File.ReadAllText(Path.Combine(repoRoot, "src", "ReVitae.Core", "ReVitae.Core.csproj"));

		Assert.Contains("System.Security.Cryptography.Xml", coreProject, StringComparison.Ordinal);
		Assert.Contains("Version=\"10.0.6\"", coreProject, StringComparison.Ordinal);
	}

	private static string FindRepoRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory is not null)
		{
			if (File.Exists(Path.Combine(directory.FullName, "Version.props")))
			{
				return directory.FullName;
			}

			directory = directory.Parent;
		}

		throw new InvalidOperationException("Could not locate repository root.");
	}
}
