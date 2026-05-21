namespace ReVitae.Core.Cv.Certificates;

public static class CertificatesFieldKeys
{
    public const string Prefix = "certificates";

    public const string Name = "name";
    public const string Issuer = "issuer";
    public const string IssueMonth = "issueMonth";
    public const string IssueYear = "issueYear";
    public const string ExpirationMonth = "expirationMonth";
    public const string ExpirationYear = "expirationYear";
    public const string CredentialId = "credentialId";
    public const string CredentialUrl = "credentialUrl";
    public const string Description = "description";
    public const string DateRange = "dateRange";

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
