using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Ollama;

public sealed class OllamaInstaller : IOllamaInstaller
{
	private const string MacDownloadUrl = "https://ollama.com/download/Ollama-darwin.zip";
	private const string WindowsDownloadUrl = "https://ollama.com/download/OllamaSetup.exe";
	private const string LinuxInstallScriptUrl = "https://ollama.com/install.sh";

	private readonly HttpClient _httpClient;

	public OllamaInstaller()
		: this(new HttpClient { Timeout = Timeout.InfiniteTimeSpan })
	{
	}

	public OllamaInstaller(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<OllamaInstallResult> EnsureInstalledAsync(
		IProgress<OllamaInstallProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		if (OllamaPaths.IsManagedInstallPresent())
		{
			return new OllamaInstallResult(true, null);
		}

		try
		{
			Directory.CreateDirectory(OllamaPaths.GetManagedInstallDirectory());

			if (OperatingSystem.IsMacOS())
			{
				return await InstallMacAsync(progress, cancellationToken).ConfigureAwait(false);
			}

			if (OperatingSystem.IsWindows())
			{
				return await InstallWindowsAsync(progress, cancellationToken).ConfigureAwait(false);
			}

			if (OperatingSystem.IsLinux())
			{
				return await InstallLinuxAsync(progress, cancellationToken).ConfigureAwait(false);
			}

			return new OllamaInstallResult(false, TranslationKeys.AiSetupOllamaInstallFailed);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch
		{
			return new OllamaInstallResult(false, TranslationKeys.AiSetupOllamaInstallFailed);
		}
	}

	private async Task<OllamaInstallResult> InstallMacAsync(
		IProgress<OllamaInstallProgress>? progress,
		CancellationToken cancellationToken)
	{
		var installDirectory = OllamaPaths.GetManagedInstallDirectory();
		var zipPath = Path.Combine(installDirectory, "Ollama-darwin.download.zip");
		var extractDirectory = Path.Combine(installDirectory, "extract");

		try
		{
			if (File.Exists(zipPath) &&
				await TryInstallMacFromZipAsync(zipPath, installDirectory, extractDirectory, progress, cancellationToken)
					.ConfigureAwait(false))
			{
				return new OllamaInstallResult(true, null);
			}

			if (File.Exists(zipPath) && !IsCompleteMacZip(zipPath))
			{
				TryDelete(zipPath);
			}

			await DownloadFileAsync(
					MacDownloadUrl,
					zipPath,
					OllamaInstallPhase.DownloadingEngine,
					progress,
					cancellationToken)
				.ConfigureAwait(false);

			if (!await TryInstallMacFromZipAsync(zipPath, installDirectory, extractDirectory, progress, cancellationToken)
					.ConfigureAwait(false))
			{
				TryDelete(zipPath);
				return new OllamaInstallResult(false, TranslationKeys.AiSetupOllamaInstallFailed);
			}

			return new OllamaInstallResult(true, null);
		}
		finally
		{
			TryDeleteDirectory(extractDirectory);
		}
	}

	private static async Task<bool> TryInstallMacFromZipAsync(
		string zipPath,
		string installDirectory,
		string extractDirectory,
		IProgress<OllamaInstallProgress>? progress,
		CancellationToken cancellationToken)
	{
		if (!IsCompleteMacZip(zipPath))
		{
			return false;
		}

		Report(progress, OllamaInstallPhase.InstallingEngine, null, null);

		if (Directory.Exists(extractDirectory))
		{
			Directory.Delete(extractDirectory, recursive: true);
		}

		Directory.CreateDirectory(extractDirectory);
		await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, extractDirectory), cancellationToken)
			.ConfigureAwait(false);

		var extractedApp = Path.Combine(extractDirectory, "Ollama.app");
		if (!Directory.Exists(extractedApp))
		{
			return false;
		}

		var targetApp = OllamaPaths.GetManagedMacAppBundlePath();
		if (Directory.Exists(targetApp))
		{
			Directory.Delete(targetApp, recursive: true);
		}

		Directory.Move(extractedApp, targetApp);
		TryDelete(zipPath);
		return true;
	}

	private static bool IsCompleteMacZip(string zipPath)
	{
		try
		{
			using var archive = ZipFile.OpenRead(zipPath);
			return archive.Entries.Any(entry =>
				entry.FullName.StartsWith("Ollama.app/", StringComparison.Ordinal) &&
				!string.IsNullOrEmpty(entry.Name));
		}
		catch
		{
			return false;
		}
	}

	private async Task<OllamaInstallResult> InstallWindowsAsync(
		IProgress<OllamaInstallProgress>? progress,
		CancellationToken cancellationToken)
	{
		var installDirectory = OllamaPaths.GetManagedInstallDirectory();
		var installerPath = Path.Combine(installDirectory, "OllamaSetup.download.exe");

		try
		{
			await DownloadFileAsync(
					WindowsDownloadUrl,
					installerPath,
					OllamaInstallPhase.DownloadingEngine,
					progress,
					cancellationToken)
				.ConfigureAwait(false);

			Report(progress, OllamaInstallPhase.InstallingEngine, null, null);

			var arguments =
				$"/VERYSILENT /NORESTART /SUPPRESSMSGBOXES /DIR=\"{installDirectory}\"";
			using var process = Process.Start(new ProcessStartInfo
			{
				FileName = installerPath,
				Arguments = arguments,
				UseShellExecute = false,
				CreateNoWindow = true,
			});

			if (process is null)
			{
				return new OllamaInstallResult(false, TranslationKeys.AiSetupOllamaInstallFailed);
			}

			await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
			if (process.ExitCode != 0 || !File.Exists(OllamaPaths.GetManagedWindowsExePath()))
			{
				return new OllamaInstallResult(false, TranslationKeys.AiSetupOllamaInstallFailed);
			}

			return new OllamaInstallResult(true, null);
		}
		finally
		{
			TryDelete(installerPath);
		}
	}

	private async Task<OllamaInstallResult> InstallLinuxAsync(
		IProgress<OllamaInstallProgress>? progress,
		CancellationToken cancellationToken)
	{
		Report(progress, OllamaInstallPhase.DownloadingEngine, null, null);

		var installDirectory = OllamaPaths.GetManagedInstallDirectory();
		var startInfo = new ProcessStartInfo
		{
			FileName = "/bin/bash",
			Arguments = $"-c \"curl -fsSL {LinuxInstallScriptUrl} | sh\"",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};

		startInfo.Environment["OLLAMA_INSTALL_DIR"] = installDirectory;
		startInfo.Environment["OLLAMA_NO_START"] = "1";

		using var process = Process.Start(startInfo);
		if (process is null)
		{
			return new OllamaInstallResult(false, TranslationKeys.AiSetupOllamaInstallFailed);
		}

		Report(progress, OllamaInstallPhase.InstallingEngine, null, null);
		await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

		if (process.ExitCode != 0 || !File.Exists(OllamaPaths.GetManagedLinuxBinaryPath()))
		{
			return new OllamaInstallResult(false, TranslationKeys.AiSetupOllamaInstallFailed);
		}

		return new OllamaInstallResult(true, null);
	}

	private async Task DownloadFileAsync(
		string url,
		string destinationPath,
		OllamaInstallPhase phase,
		IProgress<OllamaInstallProgress>? progress,
		CancellationToken cancellationToken)
	{
		var existingBytes = File.Exists(destinationPath) ? new FileInfo(destinationPath).Length : 0L;
		if (existingBytes > 0)
		{
			try
			{
				await DownloadFileCoreAsync(
						url,
						destinationPath,
						phase,
						progress,
						existingBytes,
						cancellationToken)
					.ConfigureAwait(false);
				return;
			}
			catch
			{
				TryDelete(destinationPath);
			}
		}

		await DownloadFileCoreAsync(
				url,
				destinationPath,
				phase,
				progress,
				resumeFromBytes: 0,
				cancellationToken)
			.ConfigureAwait(false);
	}

	private async Task DownloadFileCoreAsync(
		string url,
		string destinationPath,
		OllamaInstallPhase phase,
		IProgress<OllamaInstallProgress>? progress,
		long resumeFromBytes,
		CancellationToken cancellationToken)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		if (resumeFromBytes > 0)
		{
			request.Headers.Range = new RangeHeaderValue(resumeFromBytes, null);
		}

		using var response = await _httpClient
			.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
			.ConfigureAwait(false);

		if (resumeFromBytes > 0 && response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
		{
			throw new IOException("Download resume range was rejected.");
		}

		if (!response.IsSuccessStatusCode)
		{
			response.EnsureSuccessStatusCode();
		}

		var totalBytes = response.Content.Headers.ContentLength;
		if (response.StatusCode == HttpStatusCode.PartialContent && resumeFromBytes > 0)
		{
			totalBytes = resumeFromBytes + (totalBytes ?? 0);
		}
		else if (totalBytes is long contentLength)
		{
			totalBytes = contentLength;
		}
		else if (resumeFromBytes > 0)
		{
			totalBytes = null;
		}

		await using var contentStream = await response.Content
			.ReadAsStreamAsync(cancellationToken)
			.ConfigureAwait(false);

		await using var fileStream = new FileStream(
			destinationPath,
			resumeFromBytes > 0 && response.StatusCode == HttpStatusCode.PartialContent
				? FileMode.Append
				: FileMode.Create,
			FileAccess.Write,
			FileShare.None);

		var buffer = new byte[81920];
		var completedBytes = resumeFromBytes > 0 && response.StatusCode == HttpStatusCode.PartialContent
			? resumeFromBytes
			: 0L;
		Report(progress, phase, completedBytes, totalBytes);

		while (true)
		{
			using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			readTimeout.CancelAfter(TimeSpan.FromSeconds(45));

			int read;
			try
			{
				read = await contentStream.ReadAsync(buffer, readTimeout.Token).ConfigureAwait(false);
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				throw new TimeoutException("Engine download stalled.");
			}

			if (read == 0)
			{
				break;
			}

			await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
			completedBytes += read;
			Report(progress, phase, completedBytes, totalBytes);
		}
	}

	private static void Report(
		IProgress<OllamaInstallProgress>? progress,
		OllamaInstallPhase phase,
		long? completedBytes,
		long? totalBytes) =>
		progress?.Report(new OllamaInstallProgress(phase, completedBytes, totalBytes));

	private static void TryDelete(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	private static void TryDeleteDirectory(string path)
	{
		try
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
			}
		}
		catch
		{
		}
	}
}
