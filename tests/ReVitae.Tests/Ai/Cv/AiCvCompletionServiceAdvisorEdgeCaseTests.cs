using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Ai.Providers.Chat;
using ReVitae.Core.Cv;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;
using Helpers = ReVitae.Tests.Ai.Import.AiImportTestHelpers;
using CvWorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;
using SkillItem = ReVitae.Core.Cv.Skills.SkillItem;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvCompletionServiceAdvisorEdgeCaseTests
{
	[Fact]
	public async Task AdviseSectionAsync_ForceRefresh_BypassesCache()
	{
		var runtime = new CountingRuntime("- Tip");
		var service = Service(runtime);
		var snapshot = SkillsSnapshot();

		await service.AdviseSectionAsync(snapshot, CvImportSectionId.Skills, "en");
		var refreshed = await service.AdviseSectionAsync(snapshot, CvImportSectionId.Skills, "en", forceRefresh: true);

		Assert.False(refreshed.FromCache);
		Assert.Equal(2, runtime.Calls);
	}

	[Fact]
	public async Task AdviseSectionAsync_ParseFailure_ReturnsEmptyResponseError()
	{
		var service = Service(new Helpers.SequenceRuntime(["   \n   "]));
		var result = await service.AdviseSectionAsync(SkillsSnapshot(), CvImportSectionId.Skills, "en");

		Assert.False(result.Succeeded);
		Assert.Equal(TranslationKeys.AiCvEmptyResponse, result.ErrorMessageKey);
	}

	[Fact]
	public async Task AdviseSectionAsync_ProviderError_ReturnsErrorKey()
	{
		var service = Service(new Helpers.FailingRuntime(TranslationKeys.AiSetupProviderInvalidKey));
		var result = await service.AdviseSectionAsync(SkillsSnapshot(), CvImportSectionId.Skills, "en");

		Assert.False(result.Succeeded);
		Assert.Equal(TranslationKeys.AiSetupProviderInvalidKey, result.ErrorMessageKey);
	}

	[Fact]
	public async Task AdviseSectionAsync_EmptySection_StillProducesAdvice()
	{
		var service = Service(new Helpers.SequenceRuntime(["- Add your top languages"]));
		var result = await service.AdviseSectionAsync(EmptySnapshot(), CvImportSectionId.Languages, "en");

		Assert.True(result.Succeeded);
		Assert.Single(result.Suggestions);
	}

	[Fact]
	public async Task SuggestMeasurableResultsAsync_ReturnsAdvice()
	{
		var service = Service(new Helpers.SequenceRuntime(["- State the team size — shows scope"]));
		var snapshot = EmptySnapshot() with
		{
			WorkExperience = [new CvWorkExperienceEntry("w1") { JobTitle = "Lead", Company = "Acme", Description = "Led migration." }],
		};

		var result = await service.SuggestMeasurableResultsAsync(snapshot, "w1", "en");

		Assert.True(result.Succeeded);
		Assert.Equal("shows scope", result.Suggestions[0].Rationale);
	}

	[Theory]
	[InlineData(CvQualityHintIds.EducationSectionEmpty)]
	[InlineData(CvQualityHintIds.LanguagesSectionEmpty)]
	[InlineData(CvQualityHintIds.SkillsSectionEmpty)]
	public async Task AdviseForQualityHintAsync_EmptySectionHints_ReturnAdvice(string hintId)
	{
		var service = Service(new Helpers.SequenceRuntime(["- Add this section"]));
		var hint = new CvQualityHint(hintId, "k", CvQualityHintSeverity.Suggestion, SectionForHint(hintId));

		var result = await service.AdviseForQualityHintAsync(EmptySnapshot(), hint, "en");

		Assert.True(result.Succeeded);
	}

	[Fact]
	public async Task AdviseForQualityHintAsync_UnsupportedHint_Fails()
	{
		var service = Service(new Helpers.SequenceRuntime(["- x"]));
		var hint = new CvQualityHint(
			CvQualityHintIds.WorkSectionEmpty, "k", CvQualityHintSeverity.Suggestion, CvImportSectionId.WorkExperience);

		var result = await service.AdviseForQualityHintAsync(EmptySnapshot(), hint, "en");

		Assert.False(result.Succeeded);
	}

	[Fact]
	public void IsAdviceAndSingleValueHint_AreMutuallyConsistent()
	{
		var service = new AiCvCompletionService();
		Assert.True(service.IsAdviceQualityHint(CvQualityHintIds.SkillsSectionEmpty));
		Assert.False(service.IsSingleValueQualityHint(CvQualityHintIds.SkillsSectionEmpty));

		Assert.True(service.IsSingleValueQualityHint(CvQualityHintIds.WorkGenericDescription));
		Assert.False(service.IsAdviceQualityHint(CvQualityHintIds.WorkGenericDescription));

		Assert.False(service.IsAdviceQualityHint(CvQualityHintIds.WorkSectionEmpty));
		Assert.False(service.IsSingleValueQualityHint(CvQualityHintIds.WorkSectionEmpty));
	}

	[Fact]
	public async Task CompleteForQualityHintAsync_ShortenSummary_SucceedsWithGuard()
	{
		var service = Service(new Helpers.SequenceRuntime(["Concise senior engineer summary."]));
		var hint = new CvQualityHint(
			CvQualityHintIds.PersonalSummaryTooLong,
			"k",
			CvQualityHintSeverity.Suggestion,
			CvImportSectionId.PersonalInformation,
			MainPersonalInformationFieldKeys.ShortSummary);
		var snapshot = EmptySnapshot() with
		{
			Personal = new PersonalInformationImport { ShortSummary = "A very long original professional summary." },
		};

		var result = await service.CompleteForQualityHintAsync(snapshot, hint, "en");

		Assert.True(result.Succeeded);
		Assert.NotNull(result.EntityGuard);
	}

	private static AiCvCompletionService Service(IAiBackendRuntime runtime) =>
		new(
			Helpers.CreateConfigService(Helpers.LocalSettings("gemma2-2b")),
			new Helpers.FixedRuntimeResolver(runtime));

	private static CvImportSectionId SectionForHint(string hintId) => hintId switch
	{
		CvQualityHintIds.EducationSectionEmpty => CvImportSectionId.Education,
		CvQualityHintIds.LanguagesSectionEmpty => CvImportSectionId.Languages,
		_ => CvImportSectionId.Skills,
	};

	private static CvExportSourceData SkillsSnapshot()
	{
		var group = new SkillsGroupEntry { Category = "All" };
		group.Skills.Add(new SkillItem { Name = "C#" });
		return EmptySnapshot() with { Skills = [group] };
	}

	private static CvExportSourceData EmptySnapshot() =>
		new(new PersonalInformationImport(), [], [], [], [], [], [], [], null);

	private sealed class CountingRuntime(string content) : IAiBackendRuntime
	{
		public int Calls { get; private set; }

		public AiBackendKind Kind => AiBackendKind.Local;

		public string DescribeActiveBackend(AppLocalizer localizer) => "Counting";

		public Task<AiChatCompletionResult> CompleteAsync(AiCvPromptMessages messages, CancellationToken cancellationToken = default)
		{
			Calls++;
			return Task.FromResult(new AiChatCompletionResult(true, content, null));
		}
	}
}
