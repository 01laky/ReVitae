namespace ReVitae.Core.Import;

/// <summary>Per-import session flags (e.g. force OCR on text PDFs).</summary>
public sealed record CvImportSessionOptions(bool ForceOcr = false)
{
    public static CvImportSessionOptions Default { get; } = new();

    private static readonly AsyncLocal<CvImportSessionOptions?> Current = new();

    public static CvImportSessionOptions Session => Current.Value ?? Default;

    public static IDisposable Begin(CvImportSessionOptions options)
    {
        var previous = Current.Value;
        Current.Value = options;
        return new Scope(previous);
    }

    private sealed class Scope(CvImportSessionOptions? previous) : IDisposable
    {
        public void Dispose()
        {
            Current.Value = previous;
        }
    }
}
