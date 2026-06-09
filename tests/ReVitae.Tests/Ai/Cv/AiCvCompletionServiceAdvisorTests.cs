using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Ai.Providers.Chat;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;
using Helpers = ReVitae.Tests.Ai.Import.AiImportTestHelpers;
using CvWorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;
using SkillItem = ReVitae.Core.Cv.Skills.SkillItem;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvCompletionServiceAdvisorTests
{
	[Fact]
	public async Task AdviseSectionAsync_MockRuntime_ReturnsSuggestions()
	{
		var runtime = new Helpers.SequenceRuntime(["- Group skills by category — easier to scan\n- Lead with relevant skills"]);
		var service = Service(runtime);

		var result = await service.AdviseSectionAsync(SkillsSnapshot(), CvImportSectionId.Skills, "en");

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Suggestions.Count);
		Assert.Equal("easier to scan", result.Suggestions[0].Rationale);
		Assert.False(result.FromCache);
	}

	[Fact]
	public async Task AdviseSectionAsync_NoBackend_ReturnsNoBackendConfigured()
	{
		var service = new AiCvCompletionService(
			Helpers.CreateConfigService(AiSettingsDocument.Empty),
			new AiBackendRuntimeResolver());

		var result = await service.AdviseSectionAsync(SkillsSnapshot(), CvImportSectionId.Skills, "en");

		Assert.False(result.Succeeded);
		Assert.Equal(TranslationKeys.AiCvNoBackendConfigured, result.ErrorMessageKey);
	}

	[Fact]
	public async Task AdviseSectionAsync_Cancellation_ReturnsCancelled()
	{
		var service = Service(new Helpers.SequenceRuntime(["- x"]));
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		var result = await service.AdviseSectionAsync(SkillsSnapshot(), CvImportSectionId.Skills, "en", cancellationToken: cts.Token);

		Assert.True(result.Cancelled);
	}

	[Fact]
	public async Task AdviseSectionAsync_SameContentTwice_SecondIsCachedAndRuntimeCalledOnce()
	{
		var runtime = new CountingRuntime("- Group skills — scan faster");
		var service = Service(runtime);
		var snapshot = SkillsSnapshot();

		var first = await service.AdviseSectionAsync(snapshot, CvImportSectionId.Skills, "en");
		var second = await service.AdviseSectionAsync(snapshot, CvImportSectionId.Skills, "en");

		Assert.False(first.FromCache);
		Assert.True(second.FromCache);
		Assert.Equal(1, runtime.Calls);
	}

	[Fact]
	public async Task AdviseForQualityHintAsync_SkillsGrouping_ReturnsAdvice()
	{
		var runtime = new Helpers.SequenceRuntime(["- Split into Languages and Tools"]);
		var service = Service(runtime);
		var hint = new CvQualityHint(
			CvQualityHintIds.SkillsSingleLargeGroup,
			"k",
			CvQualityHintSeverity.Suggestion,
			CvImportSectionId.Skills);

		var result = await service.AdviseForQualityHintAsync(SkillsSnapshot(), hint, "en");

		Assert.True(result.Succeeded);
		Assert.Single(result.Suggestions);
	}

	[Fact]
	public async Task CompleteForQualityHintAsync_AdviceListHint_RoutedAway()
	{
		var service = Service(new Helpers.SequenceRuntime(["whatever"]));
		var hint = new CvQualityHint(
			CvQualityHintIds.SkillsSectionEmpty,
			"k",
			CvQualityHintSeverity.Suggestion,
			CvImportSectionId.Skills);

		var result = await service.CompleteForQualityHintAsync(EmptySnapshot(), hint, "en");

		Assert.False(result.Succeeded);
		Assert.Equal(TranslationKeys.AiCvTaskFailed, result.ErrorMessageKey);
	}

	[Fact]
	public async Task CompleteForQualityHintAsync_RewriteAddsFabricatedNumber_SetsEntityGuard()
	{
		var runtime = new Helpers.SequenceRuntime(["Improved checkout reliability by 40% across all teams."]);
		var service = Service(runtime);
		var entryId = "w1";
		var hint = new CvQualityHint(
			CvQualityHintIds.WorkGenericDescription,
			"k",
			CvQualityHintSeverity.Suggestion,
			CvImportSectionId.WorkExperience,
			"workExperience.w1.description",
			entryId);
		var snapshot = EmptySnapshot() with
		{
			WorkExperience = [new CvWorkExperienceEntry(entryId) { JobTitle = "Engineer", Company = "Acme", Description = "Improved checkout reliability." }],
		};

		var result = await service.CompleteForQualityHintAsync(snapshot, hint, "en");

		Assert.True(result.Succeeded);
		Assert.NotNull(result.EntityGuard);
		Assert.True(result.EntityGuard!.HasUnsupportedEntities);
	}

	[Theory]
	[InlineData(AiCvTaskKind.ImproveWorkDescription, "Viedol som tím a riešil som škálovanie a kvalitu kódu.", "en", "sk")]
	[InlineData(AiCvTaskKind.ImproveWorkDescription, "Led the team and owned scaling.", "en", "en")]
	[InlineData(AiCvTaskKind.SectionAdvisor, "Viedol som tím a riešil som škálovanie.", "en", "en")]
	public void ResolveTaskCulture_ContentTasksFollowCvLanguage(AiCvTaskKind task, string text, string ui, string expected)
	{
		Assert.Equal(expected, AiCvCompletionService.ResolveTaskCulture(task, text, ui));
	}

	private static AiCvCompletionService Service(IAiBackendRuntime runtime) =>
		new(
			Helpers.CreateConfigService(Helpers.LocalSettings("gemma2-2b")),
			new Helpers.FixedRuntimeResolver(runtime));

	private static CvExportSourceData SkillsSnapshot()
	{
		var group = new SkillsGroupEntry { Category = "All" };
		group.Skills.Add(new SkillItem { Name = "C#" });
		group.Skills.Add(new SkillItem { Name = "SQL" });
		group.Skills.Add(new SkillItem { Name = "Azure" });
		return EmptySnapshot() with { Skills = [group] };
	}

	private static CvExportSourceData EmptySnapshot() =>
		new(new PersonalInformationImport(), [], [], [], [], [], [], [], null);

	private sealed class CountingRuntime(string content) : IAiBackendRuntime
	{
		public int Calls { get; private set; }

		public AiBackendKind Kind => AiBackendKind.Local;

		public string DescribeActiveBackend(AppLocalizer localizer) => "Counting";

		public Task<AiChatCompletionResult> CompleteAsync(
			AiCvPromptMessages messages,
			CancellationToken cancellationToken = default)
		{
			Calls++;
			return Task.FromResult(new AiChatCompletionResult(true, content, null));
		}
	}
}
