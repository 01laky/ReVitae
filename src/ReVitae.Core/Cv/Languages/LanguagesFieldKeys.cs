namespace ReVitae.Core.Cv.Languages;

public static class LanguagesFieldKeys
{
    public const string Prefix = "languages";

    public const string Language = "language";
    public const string Proficiency = "proficiency";
    public const string CefrLevel = "cefrLevel";
    public const string Certificate = "certificate";
    public const string Reading = "reading";
    public const string Writing = "writing";
    public const string Speaking = "speaking";
    public const string Listening = "listening";

    public static string Build(string entryId, string fieldName)
    {
        return $"{Prefix}.{entryId}.{fieldName}";
    }

    public static bool TryParseEntryId(string fieldKey, out string entryId, out string fieldName)
    {
        entryId = string.Empty;
        fieldName = string.Empty;

        if (!fieldKey.StartsWith(Prefix + ".", StringComparison.Ordinal))
        {
            return false;
        }

        var remainder = fieldKey[(Prefix.Length + 1)..];
        var separatorIndex = remainder.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex >= remainder.Length - 1)
        {
            return false;
        }

        entryId = remainder[..separatorIndex];
        fieldName = remainder[(separatorIndex + 1)..];
        return true;
    }
}
