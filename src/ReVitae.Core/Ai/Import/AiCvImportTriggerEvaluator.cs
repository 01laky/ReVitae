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

	// 045 B.1 — broadened, less-conservative offers.
	DeterministicPartial = 1 << 4,
	DeterministicHasLowFields = 1 << 5,
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

		// 045 B.1 — partial parse (3–4 populated sections) is now worth an Enhance offer.
		if (deterministic.Success && sectionCount is >= 3 and <= 4)
		{
			flags |= AiCvImportTriggerFlags.DeterministicPartial;
		}

		// 045 B.1 — any low-confidence field enables the targeted Fix-fields offer (B.2).
		if (deterministic.Success && lowConfidenceCount >= 1)
		{
			flags |= AiCvImportTriggerFlags.DeterministicHasLowFields;
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
