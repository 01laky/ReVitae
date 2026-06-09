using ReVitae.Core.Ai.Import;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiCvImportTriggerEvaluatorEdgeCaseTests
{
	private static CvImportResult SuccessWith(int sections, params ImportedFieldConfidence[] confidences)
	{
		var flags = Enum.GetValues<CvImportSectionId>().Take(sections).ToDictionary(id => id, _ => true);
		foreach (var id in Enum.GetValues<CvImportSectionId>().Skip(sections))
		{
			flags[id] = false;
		}

		return new CvImportResult
		{
			Success = true,
			Personal = new PersonalInformationImport { FirstName = "John" },
			SectionHasData = flags,
			FieldConfidences = confidences,
		};
	}

	[Fact]
	public void Evaluate_ExactlyTwoSections_ThinNotPartial()
	{
		var attempt = AiImportTestHelpers.CreateAttempt(SuccessWith(2), new string('x', 300));
		var flags = AiCvImportTriggerEvaluator.Evaluate(attempt);
		Assert.True(flags.HasFlag(AiCvImportTriggerFlags.DeterministicThin));
		Assert.False(flags.HasFlag(AiCvImportTriggerFlags.DeterministicPartial));
	}

	[Fact]
	public void Evaluate_FiveSectionsNoLowFields_None()
	{
		var attempt = AiImportTestHelpers.CreateAttempt(SuccessWith(5), new string('x', 300));
		Assert.Equal(AiCvImportTriggerFlags.None, AiCvImportTriggerEvaluator.Evaluate(attempt));
	}

	[Fact]
	public void Evaluate_PartialPlusLowField_BothFlags()
	{
		var attempt = AiImportTestHelpers.CreateAttempt(
			SuccessWith(3, new ImportedFieldConfidence("firstName", CvImportConfidence.Low)),
			new string('x', 300));
		var flags = AiCvImportTriggerEvaluator.Evaluate(attempt);
		Assert.True(flags.HasFlag(AiCvImportTriggerFlags.DeterministicPartial));
		Assert.True(flags.HasFlag(AiCvImportTriggerFlags.DeterministicHasLowFields));
	}

	[Fact]
	public void Evaluate_FailedImport_NoLowFieldFlag()
	{
		var attempt = AiImportTestHelpers.CreateAttempt(
			CvImportResult.Failed(TranslationKeys.ImportErrorNoStructuredData),
			new string('x', 300));
		var flags = AiCvImportTriggerEvaluator.Evaluate(attempt);
		Assert.True(flags.HasFlag(AiCvImportTriggerFlags.DeterministicFailed));
		Assert.False(flags.HasFlag(AiCvImportTriggerFlags.DeterministicHasLowFields));
	}

	[Fact]
	public void Evaluate_MediumConfidenceOnly_NoLowFieldFlag()
	{
		var attempt = AiImportTestHelpers.CreateAttempt(
			SuccessWith(4, new ImportedFieldConfidence("email", CvImportConfidence.Medium)),
			new string('x', 300));
		var flags = AiCvImportTriggerEvaluator.Evaluate(attempt);
		Assert.False(flags.HasFlag(AiCvImportTriggerFlags.DeterministicHasLowFields));
	}

	[Fact]
	public void Evaluate_ShortText_None()
	{
		var attempt = AiImportTestHelpers.CreateAttempt(SuccessWith(3), new string('x', 40));
		Assert.Equal(AiCvImportTriggerFlags.None, AiCvImportTriggerEvaluator.Evaluate(attempt));
	}

	[Fact]
	public void ShouldOfferAi_PartialParse_ReturnsTrue()
	{
		var attempt = AiImportTestHelpers.CreateAttempt(SuccessWith(4), new string('x', 300));
		Assert.True(AiCvImportTriggerEvaluator.ShouldOfferAi(attempt));
	}
}
