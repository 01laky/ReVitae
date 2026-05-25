using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Import;

[Flags]
public enum AiCvImportTriggerFlags
{
    None = 0,
    DeterministicFailed = 1 << 0,
    DeterministicThin = 1 << 1,
    DeterministicLowConfidence = 1 << 2,
    UserRequested = 1 << 3,
}

public static class AiCvImportTriggerEvaluator
{
    public static AiCvImportTriggerFlags Evaluate(CvTextImportAttempt attempt)
    {
        if (!CvImportSectionMetrics.IsTextRouteFormat(attempt.Format))
        {
            return AiCvImportTriggerFlags.None;
        }

        if (attempt.NonWhitespaceCharCount < AiImportLimits.MinSourceCharsForAi)
        {
            return AiCvImportTriggerFlags.None;
        }

        var flags = AiCvImportTriggerFlags.None;
        var deterministic = attempt.Deterministic;
        var sectionCount = CvImportSectionMetrics.CountPopulatedSections(deterministic.SectionHasData);

        if (!deterministic.Success &&
            string.Equals(deterministic.ErrorMessageKey, TranslationKeys.ImportErrorNoStructuredData, StringComparison.Ordinal))
        {
            flags |= AiCvImportTriggerFlags.DeterministicFailed;
        }

        if (deterministic.Success && sectionCount <= 2)
        {
            flags |= AiCvImportTriggerFlags.DeterministicThin;
        }

        var lowConfidenceCount = deterministic.FieldConfidences.Count(f => f.Confidence == CvImportConfidence.Low);
        var ocrUsed = deterministic.Warnings.Any(w => w.MessageKey == TranslationKeys.ImportWarningOcrUsed);
        if (deterministic.Success &&
            (lowConfidenceCount >= 5 || (ocrUsed && sectionCount <= 4)))
        {
            flags |= AiCvImportTriggerFlags.DeterministicLowConfidence;
        }

        return flags;
    }

    public static bool ShouldOfferAi(CvTextImportAttempt attempt, AiCvImportTriggerFlags extraFlags = AiCvImportTriggerFlags.None)
    {
        if (attempt.NonWhitespaceCharCount < AiImportLimits.MinSourceCharsForAi)
        {
            return false;
        }

        if (CvImportSectionMetrics.IsStructuredFormat(attempt.Format))
        {
            var sectionCount = CvImportSectionMetrics.CountPopulatedSections(attempt.Deterministic.SectionHasData);
            if (attempt.Deterministic.Success && sectionCount >= 5)
            {
                return false;
            }
        }

        var flags = Evaluate(attempt) | extraFlags;
        return flags != AiCvImportTriggerFlags.None;
    }

    public static bool ShouldSkipForStructuredSuccess(CvImportFormat format, CvImportResult result)
    {
        if (!CvImportSectionMetrics.IsStructuredFormat(format))
        {
            return false;
        }

        return result.Success &&
               CvImportSectionMetrics.CountPopulatedSections(result.SectionHasData) >= 5;
    }
}
