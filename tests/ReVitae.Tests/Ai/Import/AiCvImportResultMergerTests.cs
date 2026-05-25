using ReVitae.Core.Ai.Import;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiCvImportResultMergerTests
{
	[Fact]
	public void MergeFragment_DuplicateWorkEntry_AppendsBothWhenDifferentCompany()
	{
		var accumulated = AiCvImportResultMerger.CreateEmptyDocument();
		AiCvImportResultMerger.MergeFragment(accumulated, AiImportTestHelpers.WorkJson(("Acme", 2020)));
		AiCvImportResultMerger.MergeFragment(accumulated, AiImportTestHelpers.WorkJson(("Acme", 2020), ("Globex", 2018)));
		var result = AiCvImportResponseParser.MapAccumulated(accumulated, []);
		Assert.Equal(3, result.WorkExperienceEntries.Count);
	}

	[Fact]
	public void MergeForApply_DuplicateWorkEntry_DedupesByCompanyStartDate()
	{
		var baseline = new CvImportResult
		{
			Success = true,
			WorkExperienceEntries = [new WorkExperienceEntry("w1") { Company = "Acme", StartYear = 2020, StartMonth = 1 }],
		};
		var ai = new CvImportResult
		{
			Success = true,
			WorkExperienceEntries =
			[
				new WorkExperienceEntry("w1") { Company = "Acme", StartYear = 2020, StartMonth = 1 },
				new WorkExperienceEntry("w2") { Company = "Globex", StartYear = 2018, StartMonth = 1 },
			],
			Warnings = [],
		};
		var merged = AiCvImportResultMerger.MergeForApply(ai, baseline, AiCvImportMergeMode.FillEmptyOnly, null);
		Assert.Equal(2, merged.WorkExperienceEntries.Count);
	}

	[Fact]
	public void MergeForApply_FillEmptyOnly_KeepsDeterministicEmail()
	{
		var baseline = new CvImportResult
		{
			Success = true,
			Personal = new PersonalInformationImport { Email = "keep@example.com", FirstName = "John" },
			WorkExperienceEntries = [new WorkExperienceEntry("w1") { Company = "Acme", StartYear = 2020, StartMonth = 1 }],
		};
		var ai = new CvImportResult
		{
			Success = true,
			Personal = new PersonalInformationImport { Email = "ai@example.com", FirstName = "John", LastName = "Doe" },
			WorkExperienceEntries =
			[
				new WorkExperienceEntry("w1") { Company = "Acme", StartYear = 2020, StartMonth = 1 },
				new WorkExperienceEntry("w2") { Company = "Globex", StartYear = 2018, StartMonth = 6 },
			],
			Warnings = [new CvImportWarning(TranslationKeys.ImportWarningAiAssisted)],
		};

		var merged = AiCvImportResultMerger.MergeForApply(ai, baseline, AiCvImportMergeMode.FillEmptyOnly, null);
		Assert.Equal("keep@example.com", merged.Personal.Email);
		Assert.Equal("Doe", merged.Personal.LastName);
		Assert.Equal(2, merged.WorkExperienceEntries.Count);
	}

	[Fact]
	public void MergeForApply_ReplaceAll_UsesAiPersonal()
	{
		var ai = new CvImportResult
		{
			Success = true,
			Personal = new PersonalInformationImport { FirstName = "AI", LastName = "User" },
			Warnings = [],
		};
		var merged = AiCvImportResultMerger.MergeForApply(ai, null, AiCvImportMergeMode.ReplaceAll, null);
		Assert.Equal("AI", merged.Personal.FirstName);
	}

	[Fact]
	public void MergeForApply_PreservesExistingProfilePhotoPath()
	{
		var ai = new CvImportResult
		{
			Success = true,
			Personal = new PersonalInformationImport { FirstName = "John" },
			Warnings = [],
		};
		var merged = AiCvImportResultMerger.MergeForApply(
			ai,
			null,
			AiCvImportMergeMode.ReplaceAll,
			"/tmp/photo.jpg");
		Assert.Equal("/tmp/photo.jpg", merged.Personal.ProfilePhotoPath);
	}

	[Fact]
	public void BuildFinalResult_AiOnlySuccess_MatchesStructuredDataHeuristic()
	{
		var accumulated = AiCvImportResultMerger.CreateEmptyDocument();
		AiCvImportResultMerger.MergeFragment(accumulated, AiImportTestHelpers.PersonalJson());
		AiCvImportResultMerger.MergeFragment(accumulated, AiImportTestHelpers.WorkJson(("Acme", 2020)));
		var result = AiCvImportResultMerger.BuildFinalResult(
			accumulated,
			null,
			AiCvImportMergeMode.ReplaceAll,
			[],
			0,
			null);
		Assert.True(result.Success);
		Assert.Contains(result.Warnings, w => w.MessageKey == TranslationKeys.ImportWarningAiAssisted);
	}

	[Fact]
	public void BuildFinalResult_PartialBatchFailure_AddsPartialWarning()
	{
		var accumulated = AiImportTestHelpers.PersonalJson();
		var result = AiCvImportResultMerger.BuildFinalResult(
			accumulated,
			null,
			AiCvImportMergeMode.ReplaceAll,
			[],
			batchesFailed: 2,
			null);
		Assert.Contains(result.Warnings, w => w.MessageKey == TranslationKeys.ImportWarningAiPartial);
	}
}
