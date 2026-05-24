namespace ReVitae.Tests.Import.Fixtures.JohnDoe;

/// <summary>
/// Temp file created for one matrix variant. Always dispose (or use <c>using</c>) so generated CV bytes are deleted.
/// </summary>
public sealed class GeneratedJohnDoeVariantFile : IDisposable
{
    private readonly string _tempDirectory;
    private bool _disposed;

    private GeneratedJohnDoeVariantFile(string path, string tempDirectory)
    {
        Path = path;
        _tempDirectory = tempDirectory;
    }

    public string Path { get; }

    public string TempDirectory => _tempDirectory;

    public static GeneratedJohnDoeVariantFile Write(JohnDoeVariantSpec spec, byte[] contents) =>
        Write(spec, contents.AsMemory());

    public static GeneratedJohnDoeVariantFile Write(JohnDoeVariantSpec spec, ReadOnlyMemory<byte> contents)
    {
        var tempDirectory = JohnDoeMatrixTempDirectory.CreateVariantDirectory();
        var path = System.IO.Path.Combine(tempDirectory, spec.FileName);
        File.WriteAllBytes(path, contents.Span);
        return new GeneratedJohnDoeVariantFile(path, tempDirectory);
    }

    public static GeneratedJohnDoeVariantFile WriteText(JohnDoeVariantSpec spec, string contents)
    {
        var tempDirectory = JohnDoeMatrixTempDirectory.CreateVariantDirectory();
        var path = System.IO.Path.Combine(tempDirectory, spec.FileName);
        File.WriteAllText(path, contents);
        return new GeneratedJohnDoeVariantFile(path, tempDirectory);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        JohnDoeMatrixTempDirectory.DeleteDirectory(_tempDirectory);
    }
}
