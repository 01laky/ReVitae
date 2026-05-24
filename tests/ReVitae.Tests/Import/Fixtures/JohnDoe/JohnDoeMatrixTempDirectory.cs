namespace ReVitae.Tests.Import.Fixtures.JohnDoe;

internal static class JohnDoeMatrixTempDirectory
{
    private const string RootFolderName = "revitae-matrix";

    public static string RootPath => Path.Combine(Path.GetTempPath(), RootFolderName);

    public static string CreateVariantDirectory()
    {
        var directory = Path.Combine(RootPath, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    public static void DeleteDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        try
        {
            foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(directoryPath, recursive: true);
        }
        catch
        {
            // Best-effort cleanup; stale roots are removed by CleanupStaleRoots.
        }
    }

    public static void CleanupStaleRoots(TimeSpan maxAge)
    {
        if (!Directory.Exists(RootPath))
        {
            return;
        }

        var cutoff = DateTime.UtcNow - maxAge;
        foreach (var directory in Directory.EnumerateDirectories(RootPath))
        {
            try
            {
                if (Directory.GetCreationTimeUtc(directory) < cutoff)
                {
                    DeleteDirectory(directory);
                }
            }
            catch
            {
                // Ignore directories locked by other processes.
            }
        }
    }
}
