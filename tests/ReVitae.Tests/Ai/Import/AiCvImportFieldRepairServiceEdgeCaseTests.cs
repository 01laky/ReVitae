using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Import;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiCvImportFieldRepairServiceEdgeCaseTests
{
	private static AiCvImportFieldRepairService Service(IAiBackendRuntime runtime) =>
		new(
			AiImportTestHelpers.CreateConfigService(AiImportTestHelpers.LocalSettings("gemma2-2b")),
			new AiImportTestHelpers.FixedRuntimeResolver(runtime));

	private static CvTextImportAttempt Attempt() =>
		AiImportTestHelpers.CreateAttempt(
			AiImportTestHelpers.ThinSuccess("source text body", 3),
			"source text body");

	private static AiImportFieldRepairTarget T(string key, string value, CvImportSectionId section = CvImportSectionId.PersonalInformation) =>
		new(section, key, null, value, CvImportConfidence.Low);

	[Fact]
	public async Task RepairImportFieldsAsync_MultipleSections_AllBatchesApplied()
	{
		var runtime = new AiImportTestHelpers.SequenceRuntime(["1: FixedPersonal", "1: FixedWork"]);
		var service = Service(runtime);
		var targets = new List<AiImportFieldRepairTarget>
		{
			T("p.name", "Jon", CvImportSectionId.PersonalInformation),
			T("w.title", "Engneer", CvImportSectionId.WorkExperience),
		};

		var outcome = await service.RepairImportFieldsAsync(Attempt(), targets, "en");

		Assert.True(outcome.Succeeded);
		Assert.Equal(2, outcome.Repairs.Count);
		Assert.Contains(outcome.Repairs, r => r.RepairedValue == "FixedPersonal");
		Assert.Contains(outcome.Repairs, r => r.RepairedValue == "FixedWork");
	}

	[Fact]
	public async Task RepairImportFieldsAsync_PartialMap_SecondIndexOnly()
	{
		var runtime = new AiImportTestHelpers.SequenceRuntime(["2: OnlySecond"]);
		var service = Service(runtime);
		var targets = new List<AiImportFieldRepairTarget>
		{
			T("a", "AAA"),
			T("b", "BBB"),
		};

		var outcome = await service.RepairImportFieldsAsync(Attempt(), targets, "en");

		var first = outcome.Repairs.First(r => r.Target.FieldKey == "a");
		var second = outcome.Repairs.First(r => r.Target.FieldKey == "b");
		Assert.Equal("AAA", first.RepairedValue);
		Assert.False(first.Changed);
		Assert.Equal("OnlySecond", second.RepairedValue);
		Assert.True(second.Changed);
	}

	[Fact]
	public async Task RepairImportFieldsAsync_ModelReturnsExtraIndices_Ignored()
	{
		var runtime = new AiImportTestHelpers.SequenceRuntime(["1: Fixed\n5: ghost\n9: ghost"]);
		var service = Service(runtime);
		var outcome = await service.RepairImportFieldsAsync(Attempt(), [T("a", "AAA")], "en");

		Assert.Single(outcome.Repairs);
		Assert.Equal("Fixed", outcome.Repairs[0].RepairedValue);
	}

	[Fact]
	public async Task RepairImportFieldsAsync_EmptyModelValue_KeepsCurrent()
	{
		var runtime = new AiImportTestHelpers.SequenceRuntime(["1:    "]);
		var service = Service(runtime);
		var outcome = await service.RepairImportFieldsAsync(Attempt(), [T("a", "Original")], "en");

		Assert.Equal("Original", outcome.Repairs[0].RepairedValue);
		Assert.False(outcome.Repairs[0].Changed);
	}

	[Fact]
	public async Task RepairImportFieldsAsync_ReportsRequestedSentDropped()
	{
		var runtime = new AiImportTestHelpers.SequenceRuntime(["1: x"]);
		var service = Service(runtime);
		var targets = Enumerable.Range(0, AiImportLimits.MaxRepairFields + 3)
			.Select(i => T($"f{i}", $"v{i}"))
			.ToList();

		var outcome = await service.RepairImportFieldsAsync(Attempt(), targets, "en");

		Assert.Equal(AiImportLimits.MaxRepairFields + 3, outcome.RequestedFieldCount);
		Assert.Equal(AiImportLimits.MaxRepairFields, outcome.SentFieldCount);
		Assert.Equal(3, outcome.DroppedFieldCount);
	}

	[Fact]
	public async Task RepairImportFieldsAsync_OutcomeChangedFieldCount_CountsOnlyChanged()
	{
		var runtime = new AiImportTestHelpers.SequenceRuntime(["1: Changed\n2: Same"]);
		var service = Service(runtime);
		var outcome = await service.RepairImportFieldsAsync(
			Attempt(),
			[T("a", "Original"), T("b", "Same")],
			"en");

		Assert.Equal(1, outcome.ChangedFieldCount);
	}

	[Fact]
	public async Task RepairImportFieldsAsync_RuntimeOverride_UsedWhenNoBackend()
	{
		var service = new AiCvImportFieldRepairService(
			AiImportTestHelpers.CreateConfigService(AiSettingsDocument.Empty),
			new AiBackendRuntimeResolver());

		var outcome = await service.RepairImportFieldsAsync(
			Attempt(),
			[T("a", "Jon")],
			"en",
			runtimeOverride: new AiImportTestHelpers.SequenceRuntime(["1: John"]));

		Assert.True(outcome.Succeeded);
		Assert.Equal("John", outcome.Repairs[0].RepairedValue);
	}
}
