using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ReVitae.Core.Export;

namespace ReVitae.Export;

internal static class CvExportShellHelper
{
	public static bool OpenFile(string path)
	{
		if (!CvExportPathHelper.IsExistingFile(path))
		{
			return false;
		}

		try
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return StartProcess("open", path);
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return StartProcess(new ProcessStartInfo(path) { UseShellExecute = true });
			}

			return StartProcess("xdg-open", path);
		}
		catch
		{
			return false;
		}
	}

	public static bool RevealInFolder(string path)
	{
		if (!CvExportPathHelper.IsExistingFile(path))
		{
			return false;
		}

		try
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return StartProcess("open", "-R", path);
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return StartProcess(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
			}

			var directory = Path.GetDirectoryName(path);
			if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
			{
				return false;
			}

			return StartProcess("xdg-open", directory);
		}
		catch
		{
			return false;
		}
	}

	private static bool StartProcess(ProcessStartInfo startInfo)
	{
		using var process = Process.Start(startInfo);
		return process is not null;
	}

	private static bool StartProcess(string fileName, params string[] arguments)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = fileName,
			UseShellExecute = false
		};

		foreach (var argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		return StartProcess(startInfo);
	}
}
