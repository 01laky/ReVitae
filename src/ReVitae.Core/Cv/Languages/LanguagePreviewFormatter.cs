using ReVitae.Core.Localization;

namespace ReVitae.Core.Cv.Languages;

public static class LanguagePreviewFormatter
{
    public static string FormatMainLine(LanguageEntry entry, AppLocalizer localizer)
    {
        var flag = LanguageFlagResolver.ResolveFlagEmoji(entry.Language);
        var language = entry.Language.Trim();
        var prefix = string.IsNullOrEmpty(flag) ? language : $"{flag} {language}";
        var parts = new List<string>
        {
            prefix,
            localizer.Get(entry.Proficiency.ToTranslationKey())
        };

        if (entry.CefrLevel is not null)
        {
            parts.Add(localizer.Get(entry.CefrLevel.Value.ToTranslationKey()));
        }

        if (!string.IsNullOrWhiteSpace(entry.Certificate))
        {
            parts.Add(entry.Certificate.Trim());
        }

        return string.Join(" · ", parts);
    }

    public static IReadOnlyList<string> FormatSubSkillLines(LanguageEntry entry, AppLocalizer localizer)
    {
        var lines = new List<string>();
        AppendSubSkill(lines, localizer, TranslationKeys.PreviewReading, entry.ReadingProficiency);
        AppendSubSkill(lines, localizer, TranslationKeys.PreviewWriting, entry.WritingProficiency);
        AppendSubSkill(lines, localizer, TranslationKeys.PreviewSpeaking, entry.SpeakingProficiency);
        AppendSubSkill(lines, localizer, TranslationKeys.PreviewListening, entry.ListeningProficiency);
        return lines;
    }

    private static void AppendSubSkill(
        List<string> lines,
        AppLocalizer localizer,
        string labelKey,
        LanguageProficiency? proficiency)
    {
        if (proficiency is null)
        {
            return;
        }

        lines.Add($"{localizer.Get(labelKey)}: {localizer.Get(proficiency.Value.ToTranslationKey())}");
    }
}
