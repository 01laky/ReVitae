using System.Diagnostics;

namespace ReVitae.Core.Ai.Ollama;

/// <summary>
/// Keeps a managed <c>ollama serve</c> subprocess alive across download pause/resume.
/// Prefer this over repeatedly launching the macOS GUI app, which can conflict with an existing instance.
/// </summary>
internal static class OllamaServeSupervisor
{
	private static readonly object Gate = new();
	private static Process? _serveProcess;

	public static bool TryStartManagedServe()
	{
		lock (Gate)
		{
			if (IsServeProcessAlive())
			{
				return true;
			}

			var binaryPath = ResolveManagedServeBinaryPath();
			if (binaryPath is null)
			{
				return false;
			}

			try
			{
				var startInfo = new ProcessStartInfo
				{
					FileName = binaryPath,
					UseShellExecute = false,
					CreateNoWindow = true,
				};
				startInfo.ArgumentList.Add("serve");

				_serveProcess = Process.Start(startInfo);
				return _serveProcess is not null;
			}
			catch
			{
				return false;
			}
		}
	}

	private static bool IsServeProcessAlive()
	{
		try
		{
			return _serveProcess is { HasExited: false };
		}
		catch
		{
			return false;
		}
	}

	private static string? ResolveManagedServeBinaryPath()
	{
		if (OperatingSystem.IsMacOS())
		{
			var macBinary = OllamaPaths.GetManagedMacBinaryPath();
			return File.Exists(macBinary) ? macBinary : null;
		}

		if (OperatingSystem.IsLinux())
		{
			var linuxBinary = OllamaPaths.GetManagedLinuxBinaryPath();
			return File.Exists(linuxBinary) ? linuxBinary : null;
		}

		return null;
	}
}
