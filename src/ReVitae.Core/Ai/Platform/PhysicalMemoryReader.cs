using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace ReVitae.Core.Ai.Platform;

internal static class PhysicalMemoryReader
{
	public static long? TryGetTotalPhysicalMemoryBytes()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return TryGetWindowsMemory();
		}

		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return TryGetMacMemory();
		}

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return TryGetLinuxMemory();
		}

		return null;
	}

	private static long? TryGetWindowsMemory()
	{
		var status = new MemoryStatusEx
		{
			Length = (uint)Marshal.SizeOf<MemoryStatusEx>(),
		};

		return GlobalMemoryStatusEx(ref status)
			? (long)status.TotalPhysical
			: null;
	}

	private static long? TryGetMacMemory()
	{
		try
		{
			using var process = new System.Diagnostics.Process();
			process.StartInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "/usr/sbin/sysctl",
				Arguments = "-n hw.memsize",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			process.Start();
			var output = process.StandardOutput.ReadToEnd().Trim();
			process.WaitForExit(2_000);

			return long.TryParse(output, out var bytes) ? bytes : null;
		}
		catch
		{
			return null;
		}
	}

	private static long? TryGetLinuxMemory()
	{
		try
		{
			foreach (var line in File.ReadLines("/proc/meminfo"))
			{
				if (!line.StartsWith("MemTotal:", StringComparison.Ordinal))
				{
					continue;
				}

				var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 2 && long.TryParse(parts[1], out var kilobytes))
				{
					return kilobytes * 1024L;
				}
			}
		}
		catch
		{
			return null;
		}

		return null;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	private struct MemoryStatusEx
	{
		public uint Length;
		public uint MemoryLoad;
		public ulong TotalPhysical;
		public ulong AvailablePhysical;
		public ulong TotalPageFile;
		public ulong AvailablePageFile;
		public ulong TotalVirtual;
		public ulong AvailableVirtual;
		public ulong AvailableExtendedVirtual;
	}
}
