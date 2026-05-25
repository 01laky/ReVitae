using System.Text.RegularExpressions;
using ReVitae.Core.Export;

namespace ReVitae.Core.Import.Pdf;

public static partial class ReVitaePdfExportHintsBuilder
{
    public static ReVitaePdfExportHints Build(
        CvExportTemplateId? metadataTemplateId,
        bool metadataIsReVitaeProducer,
        string extractedText,
        bool usesDeferredSidebar)
    {
        if (metadataIsReVitaeProducer || metadataTemplateId is not null)
        {
            var profile = metadataTemplateId is { } templateId
                ? ReVitaePdfLayoutProfiles.Get(templateId)
                : ReVitaePdfLayoutProfiles.DefaultTwoColumn;

            return new ReVitaePdfExportHints(
                true,
                metadataTemplateId,
                profile.SidebarWidthRatio,
                usesDeferredSidebar);
        }

        if (!LooksLikeReVitaeExportText(extractedText))
        {
            return ReVitaePdfExportHints.None with { UsesDeferredSidebar = usesDeferredSidebar };
        }

        return new ReVitaePdfExportHints(
            true,
            null,
            ReVitaePdfLayoutProfiles.DefaultSidebarRatio,
            usesDeferredSidebar);
    }

    internal static bool LooksLikeReVitaeExportText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var skillPreviewMatches = 0;
        var workMetaMatches = 0;
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || !trimmed.Contains('·', StringComparison.Ordinal))
            {
                continue;
            }

            if (SkillPreviewLinePattern().IsMatch(trimmed))
            {
                skillPreviewMatches++;
            }

            if (WorkMetaLinePattern().IsMatch(trimmed))
            {
                workMetaMatches++;
            }
        }

        return skillPreviewMatches >= 3 || (skillPreviewMatches >= 1 && workMetaMatches >= 2);
    }

    [GeneratedRegex(@"^[^\n·]+\s·\s[^\n·]+\s·\s\d+\s+yrs?\b", RegexOptions.IgnoreCase)]
    private static partial Regex SkillPreviewLinePattern();

    [GeneratedRegex(@"^[^\n·]+\s·\s[^\n·]+\s·\s[^\n·]+\s·\s[\d\s/–—-]+(Present|Current)?\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex WorkMetaLinePattern();
}
