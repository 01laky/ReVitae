using System.Text;

namespace ReVitae.Tests.Import;

/// <summary>Creates an isolated temp directory that deletes itself when disposed.</summary>
internal sealed class TempImportDirectory : IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("revitae_import_tests_");

    /// <summary>Absolute path of the temporary directory root.</summary>
    public string RootPath => _directory.FullName;

    public string FilePath(string fileName, ReadOnlySpan<byte> content)
    {
        var path = Path.Combine(_directory.FullName, fileName);
        using var stream = File.Create(path);
        stream.Write(content);
        return path;
    }

    public string FilePath(string fileName, string text) => FilePath(fileName, Encoding.UTF8.GetBytes(text));

    public void Dispose()
    {
        try
        {
            _directory.Delete(recursive: true);
        }
        catch
        {
            // Best effort for test cleanup.
        }
    }
}
