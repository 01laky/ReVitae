namespace ReVitae.Core.Ai;

public interface IDiskSpaceChecker
{
	long? GetAvailableBytesForLocalData();

	bool HasSpaceForDownload(long approxDownloadBytes, double bufferFactor = 1.1);
}

public sealed class DiskSpaceChecker : IDiskSpaceChecker
{
	public long? GetAvailableBytesForLocalData()
	{
		try
		{
			var root = ReVitaeLocalDataPaths.GetReVitaeRootDirectory();
			Directory.CreateDirectory(root);
			var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(root))!);
			return drive.IsReady ? drive.AvailableFreeSpace : null;
		}
		catch
		{
			return null;
		}
	}

	public bool HasSpaceForDownload(long approxDownloadBytes, double bufferFactor = 1.1)
	{
		var available = GetAvailableBytesForLocalData();
		if (available is null)
		{
			return false;
		}

		var required = (long)(approxDownloadBytes * bufferFactor);
		return available.Value >= required;
	}
}
