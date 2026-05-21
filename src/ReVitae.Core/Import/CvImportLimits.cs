namespace ReVitae.Core.Import;

/// <summary>Shared guardrails for importing untrusted CV files from disk.</summary>
public static class CvImportLimits
{
    /// <summary>Maximum import file size inclusive (exactly this many bytes may be read).</summary>
    public const long MaxFileBytes = 25L * 1024 * 1024;
}
