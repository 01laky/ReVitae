namespace ReVitae.Core.Cv.Languages;

public sealed class LanguageEntry
{
    public LanguageEntry()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    public LanguageEntry(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public string Language { get; set; } = string.Empty;

    public LanguageProficiency Proficiency { get; set; } = LanguageProficiency.Intermediate;

    public CefrLevel? CefrLevel { get; set; }

    public string Certificate { get; set; } = string.Empty;

    public LanguageProficiency? ReadingProficiency { get; set; }

    public LanguageProficiency? WritingProficiency { get; set; }

    public LanguageProficiency? SpeakingProficiency { get; set; }

    public LanguageProficiency? ListeningProficiency { get; set; }

    public bool HasUserInput()
    {
        if (Proficiency != LanguageProficiency.Intermediate)
        {
            return true;
        }

        if (CefrLevel.HasValue)
        {
            return true;
        }

        if (ReadingProficiency.HasValue
            || WritingProficiency.HasValue
            || SpeakingProficiency.HasValue
            || ListeningProficiency.HasValue)
        {
            return true;
        }

        return HasText(Language) || HasText(Certificate);
    }

    public LanguageEntry Duplicate()
    {
        return new LanguageEntry
        {
            Language = Language,
            Proficiency = Proficiency,
            CefrLevel = CefrLevel,
            Certificate = Certificate,
            ReadingProficiency = ReadingProficiency,
            WritingProficiency = WritingProficiency,
            SpeakingProficiency = SpeakingProficiency,
            ListeningProficiency = ListeningProficiency
        };
    }

    public IReadOnlyDictionary<string, string?> ToFieldValues()
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [LanguagesFieldKeys.Build(Id, LanguagesFieldKeys.Language)] = Language,
            [LanguagesFieldKeys.Build(Id, LanguagesFieldKeys.Proficiency)] = Proficiency.ToString(),
            [LanguagesFieldKeys.Build(Id, LanguagesFieldKeys.CefrLevel)] = CefrLevel?.ToString(),
            [LanguagesFieldKeys.Build(Id, LanguagesFieldKeys.Certificate)] = Certificate,
            [LanguagesFieldKeys.Build(Id, LanguagesFieldKeys.Reading)] = ReadingProficiency?.ToString(),
            [LanguagesFieldKeys.Build(Id, LanguagesFieldKeys.Writing)] = WritingProficiency?.ToString(),
            [LanguagesFieldKeys.Build(Id, LanguagesFieldKeys.Speaking)] = SpeakingProficiency?.ToString(),
            [LanguagesFieldKeys.Build(Id, LanguagesFieldKeys.Listening)] = ListeningProficiency?.ToString()
        };
    }

    public string BuildHeaderSummary(string flagEmoji, string proficiencyLabel, string? cefrLabel = null)
    {
        var language = string.IsNullOrWhiteSpace(Language) ? "-" : Language.Trim();
        var prefix = string.IsNullOrEmpty(flagEmoji) ? language : $"{flagEmoji} {language}";
        var parts = new List<string> { prefix, proficiencyLabel };
        if (!string.IsNullOrWhiteSpace(cefrLabel))
        {
            parts.Add(cefrLabel);
        }

        return string.Join(" · ", parts);
    }

    private static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);
}
