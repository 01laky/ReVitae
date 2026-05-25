using System.Reflection;
using System.Text.RegularExpressions;
using ReVitae.Core;

namespace ReVitae.Tests;

public sealed class AppVersionTests
{
	public AppVersionTests()
	{
		AppVersion.Initialize(typeof(AppVersionTests).Assembly);
	}

	[Fact]
	public void Current_ReturnsNonEmptySemVerLikeValue()
	{
		var current = AppVersion.Current;

		Assert.Matches(@"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$", current);
	}

	[Fact]
	public void Informational_ReturnsNonEmptyValue()
	{
		Assert.False(string.IsNullOrWhiteSpace(AppVersion.Informational));
	}

	[Fact]
	public void IsPreRelease_IsTrueForZeroMajorVersion()
	{
		if (!AppVersion.Current.StartsWith("0.", StringComparison.Ordinal))
		{
			return;
		}

		Assert.True(AppVersion.IsPreRelease);
	}

	[Fact]
	public void AuthorMetadata_MatchesRepositoryBaseline()
	{
		AppVersion.Initialize(Assembly.GetExecutingAssembly());

		Assert.Equal("Ladislav Kostolny", AppVersion.Author);
		Assert.Equal("01laky@gmail.com", AppVersion.AuthorEmail);
	}

	[Fact]
	public void Current_MatchesRepositoryBaselineVersion()
	{
		AppVersion.Initialize(Assembly.GetExecutingAssembly());

		Assert.Equal("0.2.11", AppVersion.Current);
		Assert.StartsWith("0.2.11", AppVersion.Informational, StringComparison.Ordinal);
		Assert.True(AppVersion.IsPreRelease);
	}
}

public sealed class VersionConsistencyTests
{
	[Fact]
	public void RepositoryMetadata_MatchesVersionProps()
	{
		var repoRoot = FindRepoRoot();
		var version = ReadXmlProperty(Path.Combine(repoRoot, "Version.props"), "VersionPrefix");
		var assemblyVersion = ReadXmlProperty(Path.Combine(repoRoot, "Version.props"), "AssemblyVersion");
		var readme = File.ReadAllText(Path.Combine(repoRoot, "README.md"));
		var packageJson = File.ReadAllText(Path.Combine(repoRoot, "package.json"));
		var manifest = File.ReadAllText(Path.Combine(repoRoot, "src", "ReVitae", "app.manifest"));

		Assert.Matches(@"^\d+\.\d+\.\d+$", version);
		Assert.Contains($"badge/app-{version}-blue", readme, StringComparison.Ordinal);
		Assert.Contains($"\"version\": \"{version}\"", packageJson, StringComparison.Ordinal);
		Assert.Contains("\"author\": \"Ladislav Kostolny <01laky@gmail.com>\"", packageJson, StringComparison.Ordinal);
		Assert.Contains($"assemblyIdentity version=\"{assemblyVersion}\"", manifest, StringComparison.Ordinal);
		Assert.Contains("<Authors>Ladislav Kostolny</Authors>", File.ReadAllText(Path.Combine(repoRoot, "Version.props")), StringComparison.Ordinal);
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

	private static string ReadXmlProperty(string filePath, string propertyName)
	{
		var content = File.ReadAllText(filePath);
		var match = Regex.Match(
			content,
			$@"<{Regex.Escape(propertyName)}>(?<value>[^<]+)</{Regex.Escape(propertyName)}>",
			RegexOptions.CultureInvariant);

		Assert.True(match.Success, $"Property '{propertyName}' was not found in {filePath}.");
		return match.Groups["value"].Value.Trim();
	}
}
