using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Import;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiCvImportFieldRepairServiceTests
{
	[Fact]
	public async Task RepairImportFieldsAsync_OnlyTargetedFieldsChange_OthersPreserved()
	{
		var runtime = new AiImportTestHelpers.SequenceRuntime(["1: Engineer"]);
		var service = Service(runtime);
		var targets = new List<AiImportFieldRepairTarget>
		{
			new(CvImportSectionId.WorkExperience, "w.title", "w1", "Engneer", CvImportConfidence.Low),
			new(CvImportSectionId.WorkExperience, "w.company", "w1", "Acme", CvImportConfidence.Low),
		};

		var outcome = await service.RepairImportFieldsAsync(Attempt(), targets, "en");

		Assert.True(outcome.Succeeded);
		Assert.Equal(2, outcome.Repairs.Count);
		var title = outcome.Repairs.First(r => r.Target.FieldKey == "w.title");
		var company = outcome.Repairs.First(r => r.Target.FieldKey == "w.company");
		Assert.Equal("Engineer", title.RepairedValue);
		Assert.True(title.Changed);
		Assert.Equal("Acme", company.RepairedValue);
		Assert.False(company.Changed);
	}

	[Fact]
	public async Task RepairImportFieldsAsync_ModelReturnsSameValue_NotChanged()
	{
		var runtime = new AiImportTestHelpers.SequenceRuntime(["1: Acme"]);
		var service = Service(runtime);
		var targets = new List<AiImportFieldRepairTarget>
		{
			new(CvImportSectionId.WorkExperience, "w.company", "w1", "Acme", CvImportConfidence.Low),
		};

		var outcome = await service.RepairImportFieldsAsync(Attempt(), targets, "en");

		Assert.True(outcome.Succeeded);
		Assert.False(outcome.Repairs[0].Changed);
		Assert.Equal(0, outcome.ChangedFieldCount);
	}

	[Fact]
	public async Task RepairImportFieldsAsync_AboveCap_SendsCapAndReportsDropped()
	{
		var runtime = new AiImportTestHelpers.SequenceRuntime(["1: fixed"]);
		var service = Service(runtime);
		var targets = Enumerable.Range(0, 30)
			.Select(i => new AiImportFieldRepairTarget(
				CvImportSectionId.PersonalInformation, $"f{i}", null, $"value{i}", CvImportConfidence.Low))
			.ToList();

		var outcome = await service.RepairImportFieldsAsync(Attempt(), targets, "en");

		Assert.Equal(30, outcome.RequestedFieldCount);
		Assert.Equal(AiImportLimits.MaxRepairFields, outcome.SentFieldCount);
		Assert.Equal(30 - AiImportLimits.MaxRepairFields, outcome.DroppedFieldCount);
	}

	[Fact]
	public async Task RepairImportFieldsAsync_NoBackend_Fails()
	{
		var service = new AiCvImportFieldRepairService(
			AiImportTestHelpers.CreateConfigService(AiSettingsDocument.Empty),
			new AiBackendRuntimeResolver());

		var targets = new List<AiImportFieldRepairTarget>
		{
			new(CvImportSectionId.PersonalInformation, "p.name", null, "Jon", CvImportConfidence.Low),
		};

		var outcome = await service.RepairImportFieldsAsync(Attempt(), targets, "en");

		Assert.False(outcome.Succeeded);
		Assert.False(string.IsNullOrEmpty(outcome.ErrorMessageKey));
	}

	[Fact]
	public async Task RepairImportFieldsAsync_NoUsableFields_Fails()
	{
		var service = Service(new AiImportTestHelpers.SequenceRuntime(["1: x"]));
		var targets = new List<AiImportFieldRepairTarget>
		{
			new(CvImportSectionId.PersonalInformation, "p.name", null, "  ", CvImportConfidence.Low),
		};

		var outcome = await service.RepairImportFieldsAsync(Attempt(), targets, "en");

		Assert.False(outcome.Succeeded);
		Assert.Equal(0, outcome.SentFieldCount);
	}

	[Fact]
	public async Task RepairImportFieldsAsync_Cancellation_ReturnsCancelled()
	{
		var service = Service(new AiImportTestHelpers.SequenceRuntime(["1: x"]));
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();
		var targets = new List<AiImportFieldRepairTarget>
		{
			new(CvImportSectionId.PersonalInformation, "p.name", null, "Jon", CvImportConfidence.Low),
		};

		var outcome = await service.RepairImportFieldsAsync(Attempt(), targets, "en", cts.Token);

		Assert.True(outcome.Cancelled);
	}

	[Fact]
	public async Task RepairImportFieldsAsync_BatchFails_PreservesCurrentValues()
	{
		var service = Service(new AiImportTestHelpers.FailingRuntime(TranslationKeys.AiSetupProviderInvalidKey));
		var targets = new List<AiImportFieldRepairTarget>
		{
			new(CvImportSectionId.PersonalInformation, "p.name", null, "Jon", CvImportConfidence.Low),
		};

		var outcome = await service.RepairImportFieldsAsync(Attempt(), targets, "en");

		// Single batch failed → overall failure, but values preserved (no fabrication).
		Assert.False(outcome.Succeeded);
		Assert.Equal(1, outcome.BatchesFailed);
	}

	private static AiCvImportFieldRepairService Service(ReVitae.Core.Ai.Cv.IAiBackendRuntime runtime) =>
		new(
			AiImportTestHelpers.CreateConfigService(AiImportTestHelpers.LocalSettings("gemma2-2b")),
			new AiImportTestHelpers.FixedRuntimeResolver(runtime));

	private static CvTextImportAttempt Attempt() =>
		AiImportTestHelpers.CreateAttempt(
			AiImportTestHelpers.ThinSuccess("John Doe\nEngneer at Acme", 3),
			"John Doe\nEngneer at Acme");
}
