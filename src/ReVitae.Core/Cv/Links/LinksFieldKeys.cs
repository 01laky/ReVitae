namespace ReVitae.Core.Cv.Links;

public static class LinksFieldKeys
{
    public const string Prefix = "links";

    public const string Label = "label";
    public const string Url = "url";
    public const string Note = "note";

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
