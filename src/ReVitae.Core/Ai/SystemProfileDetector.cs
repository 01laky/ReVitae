using System.Runtime.InteropServices;
using ReVitae.Core.Ai.Platform;

namespace ReVitae.Core.Ai;

public interface ISystemProfileDetector
{
	Task<SystemProfile> DetectAsync(CancellationToken cancellationToken = default);
}

public sealed class SystemProfileDetector : ISystemProfileDetector
{
	public Task<SystemProfile> DetectAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var platform = DetectPlatform();
		var architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
		var memoryBytes = PhysicalMemoryReader.TryGetTotalPhysicalMemoryBytes();
		var processorCount = Environment.ProcessorCount;

		return Task.FromResult(new SystemProfile(
			platform,
			architecture,
			memoryBytes,
			processorCount,
			DetectionWarningKey: null));
	}

	private static AiPlatform DetectPlatform()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return AiPlatform.Windows;
		}

		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return AiPlatform.MacOS;
		}

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return AiPlatform.Linux;
		}

		return AiPlatform.Unknown;
	}
}
