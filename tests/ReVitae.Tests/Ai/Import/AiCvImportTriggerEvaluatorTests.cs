using ReVitae.Core.Ai.Import;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiCvImportTriggerEvaluatorTests
{
	[Fact]
	public void ShouldOfferAi_StructuredReVitaeSuccessSixSections_ReturnsFalse()
	{
		var flags = Enum.GetValues<CvImportSectionId>().ToDictionary(id => id, id => id != CvImportSectionId.AdditionalInformation);
		var result = new CvImportResult { Success = true, SectionHasData = flags };
		var attempt = AiImportTestHelpers.CreateAttempt(result, new string('x', 500), CvImportFormat.ReVitaeJson);
		Assert.False(AiCvImportTriggerEvaluator.ShouldOfferAi(attempt));
	}

	[Fact]
	public void Evaluate_DeterministicFailedWith500Chars_SetsFailedFlag()
	{
		var attempt = AiImportTestHelpers.CreateAttempt(
			CvImportResult.Failed(TranslationKeys.ImportErrorNoStructuredData),
			new string('a', 500));
		var flags = AiCvImportTriggerEvaluator.Evaluate(attempt);
		Assert.True(flags.HasFlag(AiCvImportTriggerFlags.DeterministicFailed));
	}

	[Fact]
	public void ShouldOfferAi_FortyChars_ReturnsFalse()
	{
		var attempt = AiImportTestHelpers.CreateAttempt(
			CvImportResult.Failed(TranslationKeys.ImportErrorNoStructuredData),
			new string('a', 40));
		Assert.False(AiCvImportTriggerEvaluator.ShouldOfferAi(attempt));
	}

	[Fact]
	public void Evaluate_ThinSuccessTwoSections_SetsThinFlag()
	{
		var deterministic = AiImportTestHelpers.ThinSuccess(new string('x', 200), 2);
		var attempt = AiImportTestHelpers.CreateAttempt(deterministic, new string('x', 200));
		var flags = AiCvImportTriggerEvaluator.Evaluate(attempt);
		Assert.True(flags.HasFlag(AiCvImportTriggerFlags.DeterministicThin));
	}

	[Fact]
	public void Evaluate_OcrWarningAndThreeSections_SetsLowConfidenceFlag()
	{
		var flags = new Dictionary<CvImportSectionId, bool>
		{
			[CvImportSectionId.PersonalInformation] = true,
			[CvImportSectionId.WorkExperience] = true,
			[CvImportSectionId.Education] = true,
		};
		foreach (var id in Enum.GetValues<CvImportSectionId>())
		{
			flags.TryAdd(id, false);
		}

		var result = new CvImportResult
		{
			Success = true,
			Personal = new PersonalInformationImport { FirstName = "John" },
			SectionHasData = flags,
			Warnings = [new CvImportWarning(TranslationKeys.ImportWarningOcrUsed)],
		};
		var attempt = AiImportTestHelpers.CreateAttempt(result, new string('x', 500));
		var evaluated = AiCvImportTriggerEvaluator.Evaluate(attempt);
		Assert.True(evaluated.HasFlag(AiCvImportTriggerFlags.DeterministicLowConfidence));
	}

	[Fact]
	public void Evaluate_SuccessFivePlusSections_AllTriggersFalse()
	{
		var flags = Enum.GetValues<CvImportSectionId>().ToDictionary(id => id, _ => true);
		var result = new CvImportResult { Success = true, SectionHasData = flags };
		var attempt = AiImportTestHelpers.CreateAttempt(result, new string('x', 500));
		Assert.Equal(AiCvImportTriggerFlags.None, AiCvImportTriggerEvaluator.Evaluate(attempt));
	}

	[Fact]
	public void ShouldSkipForStructuredSuccess_ReVitaeJsonWithFiveSections_ReturnsTrue()
	{
		var flags = Enum.GetValues<CvImportSectionId>().Take(5).ToDictionary(id => id, _ => true);
		foreach (var id in Enum.GetValues<CvImportSectionId>().Skip(5))
		{
			flags[id] = false;
		}

		var result = new CvImportResult { Success = true, SectionHasData = flags };
		Assert.True(AiCvImportTriggerEvaluator.ShouldSkipForStructuredSuccess(CvImportFormat.ReVitaeJson, result));
	}

	[Fact]
	public void ShouldOfferAi_UserRequestedFlag_AllowsWhenTextLongEnough()
	{
		var attempt = AiImportTestHelpers.CreateAttempt(
			AiImportTestHelpers.ThinSuccess(new string('x', 200), 5),
			new string('x', 200));
		Assert.True(AiCvImportTriggerEvaluator.ShouldOfferAi(attempt, AiCvImportTriggerFlags.UserRequested));
	}

	[Theory]
	[InlineData(3)]
	[InlineData(4)]
	public void Evaluate_PartialSuccessThreeOrFourSections_SetsPartialFlag(int sectionCount)
	{
		var deterministic = AiImportTestHelpers.ThinSuccess(new string('x', 300), sectionCount);
		var attempt = AiImportTestHelpers.CreateAttempt(deterministic, new string('x', 300));
		var flags = AiCvImportTriggerEvaluator.Evaluate(attempt);
		Assert.True(flags.HasFlag(AiCvImportTriggerFlags.DeterministicPartial));
	}

	[Fact]
	public void Evaluate_OneLowConfidenceField_SetsHasLowFieldsFlag()
	{
		var flags = Enum.GetValues<CvImportSectionId>().Take(3).ToDictionary(id => id, _ => true);
		foreach (var id in Enum.GetValues<CvImportSectionId>().Skip(3))
		{
			flags[id] = false;
		}

		var result = new CvImportResult
		{
			Success = true,
			SectionHasData = flags,
			FieldConfidences = [new ImportedFieldConfidence("personal.firstName", CvImportConfidence.Low)],
		};
		var attempt = AiImportTestHelpers.CreateAttempt(result, new string('x', 300));
		var evaluated = AiCvImportTriggerEvaluator.Evaluate(attempt);
		Assert.True(evaluated.HasFlag(AiCvImportTriggerFlags.DeterministicHasLowFields));
	}

	[Fact]
	public void Evaluate_FivePlusSections_DoesNotSetPartialOrLowFields()
	{
		var flags = Enum.GetValues<CvImportSectionId>().ToDictionary(id => id, _ => true);
		var result = new CvImportResult { Success = true, SectionHasData = flags };
		var attempt = AiImportTestHelpers.CreateAttempt(result, new string('x', 500));
		var evaluated = AiCvImportTriggerEvaluator.Evaluate(attempt);
		Assert.False(evaluated.HasFlag(AiCvImportTriggerFlags.DeterministicPartial));
	}
}
