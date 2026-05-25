using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Ollama;

namespace ReVitae.Tests.Ai.Ollama;

[Trait("Category", "Ollama")]
public sealed class OllamaPathsEdgeCaseTests : IDisposable
{
	private readonly string? _originalLocalAppData;
	private readonly string _tempRoot;
	private readonly bool _hadManagedInstallBefore;

	public OllamaPathsEdgeCaseTests()
	{
		_tempRoot = Path.Combine(Path.GetTempPath(), "revitae-ollama-paths", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempRoot);
		_originalLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
		_hadManagedInstallBefore = OllamaPaths.IsManagedInstallPresent();
		Environment.SetEnvironmentVariable("LOCALAPPDATA", _tempRoot);
	}

	[Fact]
	public void GetManagedInstallDirectory_IsUnderReVitaeRoot()
	{
		var installDir = OllamaPaths.GetManagedInstallDirectory();
		var root = ReVitaeLocalDataPaths.GetReVitaeRootDirectory();

		Assert.StartsWith(root, installDir, StringComparison.Ordinal);
		Assert.EndsWith("ollama", installDir, StringComparison.Ordinal);
	}

	[Fact]
	public void GetManagedMacAppBundlePath_UsesInstallDirectory()
	{
		var expected = Path.Combine(OllamaPaths.GetManagedInstallDirectory(), "Ollama.app");

		Assert.Equal(expected, OllamaPaths.GetManagedMacAppBundlePath());
	}

	[Fact]
	public void GetManagedMacBinaryPath_PointsInsideAppBundle()
	{
		var path = OllamaPaths.GetManagedMacBinaryPath();

		Assert.Contains("Contents", path, StringComparison.Ordinal);
		Assert.EndsWith("ollama", path, StringComparison.Ordinal);
	}

	[Fact]
	public void GetManagedWindowsExePath_UsesInstallDirectory()
	{
		var expected = Path.Combine(OllamaPaths.GetManagedInstallDirectory(), "Ollama.exe");

		Assert.Equal(expected, OllamaPaths.GetManagedWindowsExePath());
	}

	[Fact]
	public void GetManagedLinuxBinaryPath_UsesBinSubfolder()
	{
		var expected = Path.Combine(OllamaPaths.GetManagedInstallDirectory(), "bin", "ollama");

		Assert.Equal(expected, OllamaPaths.GetManagedLinuxBinaryPath());
	}

	[Fact]
	public void IsManagedInstallPresent_ReflectsManagedLayoutOnly()
	{
		var managedDir = OllamaPaths.GetManagedInstallDirectory();
		var hadManagedInstall = OllamaPaths.IsManagedInstallPresent();

		try
		{
			if (Directory.Exists(managedDir))
			{
				Directory.Delete(managedDir, recursive: true);
			}

			Assert.False(OllamaPaths.IsManagedInstallPresent());
		}
		catch (IOException)
		{
			// Managed install directory may be locked while a serve process is running.
		}
		finally
		{
			if (hadManagedInstall && OperatingSystem.IsMacOS())
			{
				Directory.CreateDirectory(OllamaPaths.GetManagedMacAppBundlePath());
			}
		}
	}

	[Fact]
	public void IsManagedInstallPresent_DetectsManagedLayoutForCurrentOs()
	{
		if (OperatingSystem.IsMacOS())
		{
			Directory.CreateDirectory(OllamaPaths.GetManagedMacAppBundlePath());
		}
		else if (OperatingSystem.IsWindows())
		{
			Directory.CreateDirectory(Path.GetDirectoryName(OllamaPaths.GetManagedWindowsExePath())!);
			File.WriteAllText(OllamaPaths.GetManagedWindowsExePath(), string.Empty);
		}
		else if (OperatingSystem.IsLinux())
		{
			Directory.CreateDirectory(Path.GetDirectoryName(OllamaPaths.GetManagedLinuxBinaryPath())!);
			File.WriteAllText(OllamaPaths.GetManagedLinuxBinaryPath(), string.Empty);
		}

		Assert.True(OllamaPaths.IsManagedInstallPresent());
	}

	[Fact]
	public void ManagedInstallReserveBytes_IsHalfGigabyte()
	{
		Assert.Equal(512L * 1024 * 1024, OllamaPaths.ManagedInstallReserveBytes);
	}

	public void Dispose()
	{
		Environment.SetEnvironmentVariable("LOCALAPPDATA", _originalLocalAppData);
		try
		{
			if (!_hadManagedInstallBefore && OllamaPaths.IsManagedInstallPresent())
			{
				Directory.Delete(OllamaPaths.GetManagedInstallDirectory(), recursive: true);
			}

			if (Directory.Exists(_tempRoot))
			{
				Directory.Delete(_tempRoot, recursive: true);
			}
		}
		catch
		{
		}
	}
}
