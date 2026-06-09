using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Cv.Skills;
using CvWorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;
using SkillItem = ReVitae.Core.Cv.Skills.SkillItem;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvAdvisorGateTests
{
	[Fact]
	public void ShouldOffer_SummaryWithEnoughText_ReturnsTrue()
	{
		var snapshot = SnapshotWithSummary("Experienced backend engineer focused on payments and reliability.");
		Assert.True(AiCvAdvisorGate.ShouldOffer(CvImportSectionId.Summary, snapshot));
	}

	[Fact]
	public void ShouldOffer_ShortSummary_ReturnsFalse()
	{
		var snapshot = SnapshotWithSummary("Engineer.");
		Assert.False(AiCvAdvisorGate.ShouldOffer(CvImportSectionId.Summary, snapshot));
	}

	[Fact]
	public void ShouldOffer_PopulatedSkills_ReturnsTrue()
	{
		var group = new SkillsGroupEntry { Category = "Backend" };
		group.Skills.Add(new SkillItem { Name = "C#" });
		var snapshot = Empty() with { Skills = [group] };
		Assert.True(AiCvAdvisorGate.ShouldOffer(CvImportSectionId.Skills, snapshot));
	}

	[Fact]
	public void ShouldOffer_EmptyEducation_ReturnsFalse()
	{
		Assert.False(AiCvAdvisorGate.ShouldOffer(CvImportSectionId.Education, Empty()));
	}

	[Fact]
	public void ShouldOffer_NonAdvisorSection_ReturnsFalse()
	{
		Assert.False(AiCvAdvisorGate.ShouldOffer(CvImportSectionId.Certificates, Empty()));
		Assert.False(AiCvAdvisorGate.ShouldOffer(CvImportSectionId.Links, Empty()));
	}

	[Fact]
	public void ShouldOffer_PopulatedWork_ReturnsTrue()
	{
		var snapshot = Empty() with
		{
			WorkExperience = [new CvWorkExperienceEntry("w1") { JobTitle = "Engineer", Company = "Acme", Description = "Built things." }],
		};
		Assert.True(AiCvAdvisorGate.ShouldOffer(CvImportSectionId.WorkExperience, snapshot));
	}

	private static CvExportSourceData SnapshotWithSummary(string summary) =>
		Empty() with { Personal = new PersonalInformationImport { ShortSummary = summary } };

	private static CvExportSourceData Empty() =>
		new(new PersonalInformationImport(), [], [], [], [], [], [], [], null);
}
