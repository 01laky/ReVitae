using System.Diagnostics;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Ollama;

public static class OllamaStartupHelper
{
	public static bool IsLikelyInstalled()
	{
		if (OllamaPaths.IsManagedInstallPresent())
		{
			return true;
		}

		if (OperatingSystem.IsMacOS() && Directory.Exists("/Applications/Ollama.app"))
		{
			return true;
		}

		if (OperatingSystem.IsWindows())
		{
			var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			if (File.Exists(Path.Combine(localAppData, "Programs", "Ollama", "Ollama.exe")))
			{
				return true;
			}
		}

		return IsOllamaCliAvailable();
	}

	public static async Task<OllamaReachabilityResult> EnsureReachableAsync(
		IOllamaRuntimeProbe probe,
		IProgress<OllamaInstallProgress>? installProgress = null,
		IOllamaInstaller? installer = null,
		OllamaReachabilityOptions? options = null,
		CancellationToken cancellationToken = default,
		bool allowInstall = true)
	{
		installer ??= new OllamaInstaller();
		options ??= OllamaReachabilityOptions.Default;

		var initial = await ProbeWithRetriesAsync(
				probe,
				options.InitialProbeAttempts,
				options.InitialProbeDelay,
				cancellationToken)
			.ConfigureAwait(false);
		if (initial.IsReachable)
		{
			return new OllamaReachabilityResult(initial, null);
		}

		if (allowInstall && !OllamaPaths.IsManagedInstallPresent())
		{
			installProgress?.Report(new OllamaInstallProgress(OllamaInstallPhase.StartingEngine, null, null));
			var installResult = await installer
				.EnsureInstalledAsync(installProgress, cancellationToken)
				.ConfigureAwait(false);

			if (!installResult.Succeeded)
			{
				var finalAfterInstall = await probe.ProbeAsync(cancellationToken).ConfigureAwait(false);
				if (finalAfterInstall.IsReachable)
				{
					return new OllamaReachabilityResult(finalAfterInstall, null);
				}

				return new OllamaReachabilityResult(
					finalAfterInstall,
					TranslationKeys.AiSetupOllamaInstallFailed);
			}
		}

		if (TryLaunchOllama())
		{
			installProgress?.Report(new OllamaInstallProgress(OllamaInstallPhase.StartingEngine, null, null));

			var launched = await ProbeWithRetriesAsync(
					probe,
					options.LaunchProbeAttempts,
					options.LaunchProbeDelay,
					cancellationToken)
				.ConfigureAwait(false);

			if (launched.IsReachable)
			{
				return new OllamaReachabilityResult(launched, null);
			}
		}

		var final = await probe.ProbeAsync(cancellationToken).ConfigureAwait(false);
		if (final.IsReachable)
		{
			return new OllamaReachabilityResult(final, null);
		}

		var errorKey = IsLikelyInstalled() || !allowInstall
			? TranslationKeys.AiSetupOllamaNotRunning
			: TranslationKeys.AiSetupOllamaInstallFailed;

		return new OllamaReachabilityResult(final, errorKey);
	}

	internal static async Task<OllamaRuntimeStatus> ProbeWithRetriesAsync(
		IOllamaRuntimeProbe probe,
		int attempts,
		TimeSpan delay,
		CancellationToken cancellationToken)
	{
		for (var attempt = 0; attempt < attempts; attempt++)
		{
			var status = await probe.ProbeAsync(cancellationToken).ConfigureAwait(false);
			if (status.IsReachable)
			{
				return status;
			}

			if (attempt < attempts - 1)
			{
				await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
			}
		}

		return await probe.ProbeAsync(cancellationToken).ConfigureAwait(false);
	}

	private static bool TryLaunchOllama()
	{
		try
		{
			if (OperatingSystem.IsMacOS())
			{
				if (TryLaunchManagedMacOllama())
				{
					return true;
				}

				if (Directory.Exists("/Applications/Ollama.app"))
				{
					return StartProcess(MacOpenExecutable, "-a", "Ollama", "--args", "hidden");
				}
			}

			if (OperatingSystem.IsWindows())
			{
				if (File.Exists(OllamaPaths.GetManagedWindowsExePath()))
				{
					return StartProcess(OllamaPaths.GetManagedWindowsExePath());
				}

				var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				var exePath = Path.Combine(localAppData, "Programs", "Ollama", "Ollama.exe");
				if (File.Exists(exePath))
				{
					return StartProcess(exePath);
				}
			}

			if (OperatingSystem.IsLinux())
			{
				if (OllamaServeSupervisor.TryStartManagedServe())
				{
					return true;
				}
			}

			if (IsOllamaCliAvailable())
			{
				return StartManagedServe("ollama");
			}
		}
		catch
		{
		}

		return false;
	}

	private static bool TryLaunchManagedMacOllama()
	{
		if (OllamaServeSupervisor.TryStartManagedServe())
		{
			return true;
		}

		var appPath = OllamaPaths.GetManagedMacAppBundlePath();
		if (!Directory.Exists(appPath))
		{
			return false;
		}

		if (StartProcess(MacOpenExecutable, "-a", appPath, "--args", "hidden"))
		{
			return true;
		}

		var appExecutable = OllamaPaths.GetManagedMacAppExecutablePath();
		return File.Exists(appExecutable) && StartProcess(appExecutable, "hidden");
	}

	private const string MacOpenExecutable = "/usr/bin/open";

	private static bool StartManagedServe(string binaryPath)
	{
		if (string.Equals(binaryPath, OllamaPaths.GetManagedMacBinaryPath(), StringComparison.Ordinal) ||
			string.Equals(binaryPath, OllamaPaths.GetManagedLinuxBinaryPath(), StringComparison.Ordinal))
		{
			return OllamaServeSupervisor.TryStartManagedServe();
		}

		using var process = Process.Start(new ProcessStartInfo
		{
			FileName = binaryPath,
			ArgumentList = { "serve" },
			UseShellExecute = false,
			CreateNoWindow = true,
		});

		return process is not null;
	}

	private static bool IsOllamaCliAvailable()
	{
		var pathEnv = Environment.GetEnvironmentVariable("PATH");
		if (string.IsNullOrWhiteSpace(pathEnv))
		{
			return false;
		}

		foreach (var directory in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
		{
			try
			{
				var candidate = Path.Combine(directory.Trim(), OperatingSystem.IsWindows() ? "ollama.exe" : "ollama");
				if (File.Exists(candidate))
				{
					return true;
				}
			}
			catch
			{
			}
		}

		return false;
	}

	private static bool StartProcess(string fileName, params string[] arguments)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = fileName,
			UseShellExecute = false,
		};

		foreach (var argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		using var process = Process.Start(startInfo);
		return process is not null;
	}
}

public sealed record OllamaReachabilityResult(OllamaRuntimeStatus Status, string? ErrorMessageKey);
