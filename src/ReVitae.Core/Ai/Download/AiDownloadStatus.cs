namespace ReVitae.Core.Ai.Download;

public static class AiDownloadStatus
{
    public const string KeyPrefix = "#key:";

    public static string FromTranslationKey(string translationKey) => KeyPrefix + translationKey;

    public static bool TryGetTranslationKey(string? statusText, out string translationKey)
    {
        if (statusText is not null &&
            statusText.StartsWith(KeyPrefix, StringComparison.Ordinal))
        {
            translationKey = statusText[KeyPrefix.Length..];
            return true;
        }

        translationKey = string.Empty;
        return false;
    }
}
