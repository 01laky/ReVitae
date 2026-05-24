namespace ReVitae.Core.Ai.Download;

public static class AiDownloadProgress
{
    public static int? TryGetPercent(long? completedBytes, long? totalBytes)
    {
        if (completedBytes is null or < 0 || totalBytes is null or <= 0)
        {
            return null;
        }

        return (int)Math.Clamp(completedBytes.Value * 100 / totalBytes.Value, 0, 100);
    }
}
