using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Ollama;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Ollama;

public sealed class OllamaStartupHelperEdgeCaseTests
{
	[Fact]
	public void IsLikelyInstalled_DetectsManagedInstallStub()
	{
		using var env = new OllamaEnvironmentScope(clearPath: true, hideManagedInstall: false);
		env.CreateManagedInstallStub();

		Assert.True(OllamaStartupHelper.IsLikelyInstalled());
	}

	[Fact]
	public void IsLikelyInstalled_DetectsManagedInstall()
	{
		using var env = new OllamaEnvironmentScope(clearPath: true, hideManagedInstall: false);
		env.CreateManagedInstallStub();

		Assert.True(OllamaStartupHelper.IsLikelyInstalled());
	}

	[Fact]
	public async Task EnsureReachableAsync_ReturnsImmediatelyWhenProbeSucceeds()
	{
		var probe = new ScriptedOllamaProbe(reachableOnAttempt: 1);
		var installer = new RecordingOllamaInstaller();

		var result = await OllamaStartupHelper.EnsureReachableAsync(
			probe,
			installer: installer,
			options: OllamaReachabilityOptions.FastForTests,
			allowInstall: false);

		Assert.True(result.Status.IsReachable);
		Assert.Null(result.ErrorMessageKey);
		Assert.Equal(0, installer.InstallCalls);
	}

	[Fact]
	public async Task EnsureReachableAsync_SkipsInstallWhenAllowInstallFalse()
	{
		var probe = new AlwaysUnreachableProbe();
		var installer = new RecordingOllamaInstaller();

		var result = await OllamaStartupHelper.EnsureReachableAsync(
			probe,
			installer: installer,
			options: OllamaReachabilityOptions.FastForTests,
			allowInstall: false);

		Assert.False(result.Status.IsReachable);
		Assert.Equal(0, installer.InstallCalls);
	}

	[Fact]
	public async Task EnsureReachableAsync_AttemptsInstallWhenUnreachableAndNoManagedInstall()
	{
		using var env = new OllamaEnvironmentScope(clearPath: true, hideManagedInstall: true);
		env.RemoveManagedInstallDirectory();
		var probe = new AlwaysUnreachableProbe();
		var installer = new RecordingOllamaInstaller(shouldSucceed: false);

		var result = await OllamaStartupHelper.EnsureReachableAsync(
			probe,
			installer: installer,
			options: OllamaReachabilityOptions.FastForTests,
			allowInstall: true);

		Assert.False(result.Status.IsReachable);
		if (!OllamaStartupHelper.IsLikelyInstalled())
		{
			Assert.Equal(1, installer.InstallCalls);
			Assert.Equal(TranslationKeys.AiSetupOllamaInstallFailed, result.ErrorMessageKey);
		}
	}

	[Fact]
	public async Task EnsureReachableAsync_ReturnsNotRunningWhenInstalledButUnreachable()
	{
		using var env = new OllamaEnvironmentScope(clearPath: true, hideManagedInstall: false);
		env.CreateManagedInstallStub();
		var probe = new AlwaysUnreachableProbe();
		var installer = new RecordingOllamaInstaller(shouldSucceed: false);

		var result = await OllamaStartupHelper.EnsureReachableAsync(
			probe,
			installer: installer,
			options: OllamaReachabilityOptions.FastForTests,
			allowInstall: true);

		Assert.Equal(TranslationKeys.AiSetupOllamaNotRunning, result.ErrorMessageKey);
	}

	[Fact]
	public async Task ProbeWithRetriesAsync_StopsOnFirstReachableAttempt()
	{
		var probe = new ScriptedOllamaProbe(reachableOnAttempt: 2);

		var status = await OllamaStartupHelper.ProbeWithRetriesAsync(
			probe,
			attempts: 3,
			delay: TimeSpan.Zero,
			CancellationToken.None);

		Assert.True(status.IsReachable);
		Assert.Equal(2, probe.Attempts);
	}

	[Fact]
	public async Task EnsureReachableAsync_UsesInstallerWhenManagedInstallMissing()
	{
		using var env = new OllamaEnvironmentScope(clearPath: true, hideManagedInstall: true);
		env.RemoveManagedInstallDirectory();
		var probe = new ScriptedOllamaProbe(reachableOnAttempt: 2);
		var installer = new RecordingOllamaInstaller(shouldSucceed: true);

		var result = await OllamaStartupHelper.EnsureReachableAsync(
			probe,
			installer: installer,
			options: OllamaReachabilityOptions.FastForTests,
			allowInstall: true);

		Assert.True(result.Status.IsReachable);
		Assert.True(probe.Attempts >= 1);
	}

	private sealed class AlwaysUnreachableProbe : IOllamaRuntimeProbe
	{
		public Task<OllamaRuntimeStatus> ProbeAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult(new OllamaRuntimeStatus(false, []));
	}

	private sealed class ScriptedOllamaProbe(int reachableOnAttempt) : IOllamaRuntimeProbe
	{
		public int Attempts { get; private set; }

		public Task<OllamaRuntimeStatus> ProbeAsync(CancellationToken cancellationToken = default)
		{
			Attempts++;
			return Task.FromResult(new OllamaRuntimeStatus(Attempts >= reachableOnAttempt, []));
		}
	}

	private sealed class RecordingOllamaInstaller(bool shouldSucceed = true) : IOllamaInstaller
	{
		public int InstallCalls { get; private set; }

		public Task<OllamaInstallResult> EnsureInstalledAsync(
			IProgress<OllamaInstallProgress>? progress = null,
			CancellationToken cancellationToken = default)
		{
			InstallCalls++;
			return Task.FromResult(new OllamaInstallResult(
				shouldSucceed,
				shouldSucceed ? null : TranslationKeys.AiSetupOllamaInstallFailed));
		}
	}

	private sealed class OllamaEnvironmentScope : IDisposable
	{
		private readonly string? _originalLocalAppData;
		private readonly string? _originalPath;
		private readonly string _tempRoot;

		public OllamaEnvironmentScope(bool clearPath, bool hideManagedInstall)
		{
			_tempRoot = Path.Combine(Path.GetTempPath(), "revitae-ollama-startup", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_tempRoot);
			_originalLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
			_originalPath = Environment.GetEnvironmentVariable("PATH");
			Environment.SetEnvironmentVariable("LOCALAPPDATA", _tempRoot);
			if (clearPath)
			{
				Environment.SetEnvironmentVariable("PATH", string.Empty);
			}

			if (hideManagedInstall)
			{
				// Leave managed install absent.
			}
		}

		public void RemoveManagedInstallDirectory()
		{
			var managedDir = OllamaPaths.GetManagedInstallDirectory();
			if (Directory.Exists(managedDir))
			{
				Directory.Delete(managedDir, recursive: true);
			}
		}

		public void CreateManagedInstallStub()
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
		}

		public void Dispose()
		{
			Environment.SetEnvironmentVariable("LOCALAPPDATA", _originalLocalAppData);
			Environment.SetEnvironmentVariable("PATH", _originalPath);
			try
			{
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
}
