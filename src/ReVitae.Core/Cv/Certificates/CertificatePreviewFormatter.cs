using System.Globalization;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Cv.Certificates;

public static class CertificatePreviewFormatter
{
    public static string FormatMainLine(CertificateEntry entry, AppLocalizer localizer)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(entry.Name))
        {
            parts.Add(entry.Name.Trim());
        }

        if (!string.IsNullOrWhiteSpace(entry.Issuer))
        {
            parts.Add(entry.Issuer.Trim());
        }

        var issueDate = FormatPreviewDate(entry.IssueMonth, entry.IssueYear);
        if (!string.IsNullOrEmpty(issueDate))
        {
            parts.Add(issueDate);
        }

        var expirationDate = FormatPreviewDate(entry.ExpirationMonth, entry.ExpirationYear);
        if (!string.IsNullOrEmpty(expirationDate))
        {
            parts.Add($"{localizer.Get(TranslationKeys.PreviewValidUntil)} {expirationDate}");
        }

        return string.Join(" · ", parts);
    }

    public static IReadOnlyList<string> FormatDetailLines(CertificateEntry entry, AppLocalizer localizer)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(entry.CredentialId))
        {
            lines.Add($"{localizer.Get(TranslationKeys.PreviewCredentialId)}: {entry.CredentialId.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(entry.CredentialUrl))
        {
            lines.Add($"{localizer.Get(TranslationKeys.PreviewCredentialUrl)}: {entry.CredentialUrl.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(entry.Description))
        {
            lines.Add(entry.Description.Trim());
        }

        return lines;
    }

    private static string FormatPreviewDate(int? month, int? year)
    {
        if (month is null || year is null)
        {
            return string.Empty;
        }

        var date = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return date.ToString("MMM yyyy", CultureInfo.CurrentCulture);
    }
}
