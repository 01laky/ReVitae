using ReVitae.Core.Localization;

namespace ReVitae.Core.Cv.Languages;

public static class CefrLevelExtensions
{
    public static string ToTranslationKey(this CefrLevel level)
    {
        return level switch
        {
            CefrLevel.A1 => TranslationKeys.CefrA1,
            CefrLevel.A2 => TranslationKeys.CefrA2,
            CefrLevel.B1 => TranslationKeys.CefrB1,
            CefrLevel.B2 => TranslationKeys.CefrB2,
            CefrLevel.C1 => TranslationKeys.CefrC1,
            CefrLevel.C2 => TranslationKeys.CefrC2,
            _ => TranslationKeys.CefrB1
        };
    }

    public static IReadOnlyList<CefrLevel> SupportedValues { get; } =
    [
        CefrLevel.A1,
        CefrLevel.A2,
        CefrLevel.B1,
        CefrLevel.B2,
        CefrLevel.C1,
        CefrLevel.C2
    ];
}
