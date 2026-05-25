using System.Text.RegularExpressions;

namespace ReVitae.Tests;

public sealed class VerifyTestCountScriptTests
{
	[Fact]
	public void VerifyTestCountScript_ExistsAndReferencesBaseline()
	{
		var repoRoot = FindRepoRoot();
		var scriptPath = Path.Combine(repoRoot, "scripts", "verify-test-count.sh");
		Assert.True(File.Exists(scriptPath));

		var content = File.ReadAllText(scriptPath);
		Assert.Contains("MinimumTestCount", content, StringComparison.Ordinal);
		Assert.Contains("README.md", content, StringComparison.Ordinal);
		Assert.Contains("dotnet test", content, StringComparison.Ordinal);
		Assert.Contains("%20", content, StringComparison.Ordinal);
	}

	[Fact]
	public void ReadmeBadge_MatchesTestCountBaseline()
	{
		var repoRoot = FindRepoRoot();
		var readme = File.ReadAllText(Path.Combine(repoRoot, "README.md"));
		var match = Regex.Match(readme, @"tests-(\d+)(?:%20| )passing");
		Assert.True(match.Success);

		Assert.Equal(TestCountBaselineTests.MinimumTestCount.ToString(), match.Groups[1].Value);
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
