using ReVitae.Core.Localization;

namespace ReVitae.Core.Cv.Skills;

public static class ProficiencyLevelExtensions
{
    public static string ToTranslationKey(this ProficiencyLevel level)
    {
        return level switch
        {
            ProficiencyLevel.Beginner => TranslationKeys.ProficiencyBeginner,
            ProficiencyLevel.Intermediate => TranslationKeys.ProficiencyIntermediate,
            ProficiencyLevel.Advanced => TranslationKeys.ProficiencyAdvanced,
            ProficiencyLevel.Expert => TranslationKeys.ProficiencyExpert,
            _ => TranslationKeys.ProficiencyIntermediate
        };
    }

    public static IReadOnlyList<ProficiencyLevel> SupportedValues { get; } =
    [
        ProficiencyLevel.Beginner,
        ProficiencyLevel.Intermediate,
        ProficiencyLevel.Advanced,
        ProficiencyLevel.Expert
    ];
}
