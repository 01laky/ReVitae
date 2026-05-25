using ReVitae.Core.Ai.Import;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiCvImportReviewSummaryBuilderTests
{
	[Fact]
	public void Build_EmptyWorkToFourWork_MarksWorkImproved()
	{
		var before = new CvImportResult
		{
			Success = true,
			WorkExperienceEntries = [],
		};
		var after = new CvImportResult
		{
			Success = true,
			Personal = new PersonalInformationImport { FirstName = "John", Email = "j@example.com" },
			WorkExperienceEntries =
			[
				new WorkExperienceEntry("1") { Company = "A" },
				new WorkExperienceEntry("2") { Company = "B" },
				new WorkExperienceEntry("3") { Company = "C" },
				new WorkExperienceEntry("4") { Company = "D" },
			],
		};

		var summary = AiCvImportReviewSummaryBuilder.Build(before, after);
		var workRow = summary.Rows.First(r => r.SectionId == CvImportSectionId.WorkExperience);
		Assert.True(workRow.IsImproved);
		Assert.Contains(CvImportSectionId.WorkExperience, summary.ImprovedSections);
	}

	[Fact]
	public void Build_SameWorkCounts_NotImproved()
	{
		var entries = new[]
		{
			new WorkExperienceEntry("1") { Company = "Acme" },
			new WorkExperienceEntry("2") { Company = "Globex" },
		};
		var before = new CvImportResult { Success = true, WorkExperienceEntries = entries };
		var after = new CvImportResult { Success = true, WorkExperienceEntries = entries };
		var summary = AiCvImportReviewSummaryBuilder.Build(before, after);
		var workRow = summary.Rows.First(r => r.SectionId == CvImportSectionId.WorkExperience);
		Assert.False(workRow.IsImproved);
	}

	[Fact]
	public void Build_PartialPersonalToComplete_MarksPersonalImproved()
	{
		var before = new CvImportResult
		{
			Success = true,
			Personal = new PersonalInformationImport { FirstName = "John" },
		};
		var after = new CvImportResult
		{
			Success = true,
			Personal = new PersonalInformationImport
			{
				FirstName = "John",
				LastName = "Doe",
				Email = "john@example.com",
			},
		};
		var summary = AiCvImportReviewSummaryBuilder.Build(before, after);
		Assert.Contains(CvImportSectionId.PersonalInformation, summary.ImprovedSections);
	}

	[Fact]
	public void Build_FailedImportBaseline_UsesEmptyBeforeCounts()
	{
		var after = new CvImportResult
		{
			Success = true,
			WorkExperienceEntries = [new WorkExperienceEntry("1") { Company = "Acme" }],
		};
		var summary = AiCvImportReviewSummaryBuilder.Build(null, after);
		Assert.Equal("—", summary.Rows.First(r => r.SectionId == CvImportSectionId.WorkExperience).BeforeLabel);
	}
}
