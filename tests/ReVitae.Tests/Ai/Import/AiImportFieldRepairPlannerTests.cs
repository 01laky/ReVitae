using ReVitae.Core.Ai.Import;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiImportFieldRepairPlannerTests
{
	[Fact]
	public void SelectTargets_BelowCap_KeepsAllAndReportsNoDrop()
	{
		var targets = MakeTargets(5, CvImportConfidence.Low);
		var selected = AiImportFieldRepairPlanner.SelectTargets(targets, out var dropped);

		Assert.Equal(5, selected.Count);
		Assert.Equal(0, dropped);
	}

	[Fact]
	public void SelectTargets_AboveCap_CapsAndReportsDrop()
	{
		var targets = MakeTargets(30, CvImportConfidence.Low);
		var selected = AiImportFieldRepairPlanner.SelectTargets(targets, out var dropped);

		Assert.Equal(AiImportLimits.MaxRepairFields, selected.Count);
		Assert.Equal(30 - AiImportLimits.MaxRepairFields, dropped);
	}

	[Fact]
	public void SelectTargets_LowestConfidenceFirst()
	{
		var targets = new List<AiImportFieldRepairTarget>
		{
			Target("high", CvImportConfidence.High),
			Target("medium", CvImportConfidence.Medium),
			Target("low", CvImportConfidence.Low),
		};

		var selected = AiImportFieldRepairPlanner.SelectTargets(targets, out _);

		Assert.Equal(CvImportConfidence.Low, selected[0].Confidence);
	}

	[Fact]
	public void SelectTargets_DropsEmptyCurrentValues()
	{
		var targets = new List<AiImportFieldRepairTarget>
		{
			Target("ok", CvImportConfidence.Low),
			new(CvImportSectionId.Skills, "skills.empty", null, "   ", CvImportConfidence.Low),
		};

		var selected = AiImportFieldRepairPlanner.SelectTargets(targets, out _);

		Assert.Single(selected);
	}

	[Fact]
	public void GroupBySection_GroupsTargets()
	{
		var targets = new List<AiImportFieldRepairTarget>
		{
			new(CvImportSectionId.PersonalInformation, "p.name", null, "Jon", CvImportConfidence.Low),
			new(CvImportSectionId.WorkExperience, "w.title", "w1", "Engneer", CvImportConfidence.Low),
			new(CvImportSectionId.WorkExperience, "w.company", "w1", "Acme", CvImportConfidence.Low),
		};

		var groups = AiImportFieldRepairPlanner.GroupBySection(targets);

		Assert.Equal(2, groups.Count);
	}

	private static List<AiImportFieldRepairTarget> MakeTargets(int count, CvImportConfidence confidence) =>
		Enumerable.Range(0, count)
			.Select(i => Target($"value{i}", confidence))
			.ToList();

	private static AiImportFieldRepairTarget Target(string value, CvImportConfidence confidence) =>
		new(CvImportSectionId.PersonalInformation, $"field.{value}", null, value, confidence);
}
