using ReVitae.Core.Localization;

namespace ReVitae.Core.Cv.Education;

public static class DegreeTypeExtensions
{
    public static string ToTranslationKey(this DegreeType degreeType)
    {
        return degreeType switch
        {
            DegreeType.HighSchool => TranslationKeys.DegreeTypeHighSchool,
            DegreeType.Associate => TranslationKeys.DegreeTypeAssociate,
            DegreeType.Bachelor => TranslationKeys.DegreeTypeBachelor,
            DegreeType.Master => TranslationKeys.DegreeTypeMaster,
            DegreeType.Doctorate => TranslationKeys.DegreeTypeDoctorate,
            DegreeType.Certificate => TranslationKeys.DegreeTypeCertificate,
            DegreeType.Other => TranslationKeys.DegreeTypeOther,
            _ => TranslationKeys.DegreeTypeBachelor
        };
    }

    public static IReadOnlyList<DegreeType> SupportedValues { get; } =
    [
        DegreeType.HighSchool,
        DegreeType.Associate,
        DegreeType.Bachelor,
        DegreeType.Master,
        DegreeType.Doctorate,
        DegreeType.Certificate,
        DegreeType.Other
    ];
}
