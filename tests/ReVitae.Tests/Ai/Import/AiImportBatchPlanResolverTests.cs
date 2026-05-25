using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Import;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiImportBatchPlanResolverTests
{
	[Theory]
	[InlineData("gemma2-2b", 1200)]
	[InlineData("phi3-mini", 2000)]
	[InlineData("llama32-3b", 2400)]
	[InlineData("mistral-7b", 5000)]
	[InlineData("llama31-70b", 16000)]
	public void Resolve_EveryCatalogEntry_ReturnsProfileWithMaxInput(string modelId, int expectedMaxInput)
	{
		var profile = AiImportBatchPlanResolver.Resolve(AiImportTestHelpers.LocalSettings(modelId));
		Assert.Equal(expectedMaxInput, profile.MaxInputChars);
	}

	[Fact]
	public void Resolve_AllCatalogIds_MapWithoutThrow()
	{
		foreach (var entry in AiModelCatalog.Default)
		{
			var profile = AiImportBatchPlanResolver.Resolve(AiImportTestHelpers.LocalSettings(entry.Id));
			Assert.True(profile.MaxInputChars > 0);
			Assert.False(string.IsNullOrWhiteSpace(profile.ProfileId));
		}
	}

	[Fact]
	public void Resolve_Gemma2_2b_UsesCompactProfileWithCombinedSkillsLanguages()
	{
		var profile = AiImportBatchPlanResolver.Resolve(AiImportTestHelpers.LocalSettings("gemma2-2b"));
		Assert.Equal(1200, profile.MaxInputChars);
		Assert.True(profile.CombineSkillsAndLanguages);
		Assert.Equal(AiImportPhaseMode.SequentialMicro, profile.PhaseMode);
	}

	[Fact]
	public void Resolve_UnknownOnlineModelId_DefaultsToSmall()
	{
		var settings = AiImportTestHelpers.OnlineSettings("openai", "unknown-model-xyz-2026");
		var profile = AiImportBatchPlanResolver.Resolve(settings);
		Assert.Equal(2400, profile.MaxInputChars);
		Assert.StartsWith("online:", profile.ProfileId, StringComparison.Ordinal);
	}

	[Fact]
	public void Resolve_AzureOpenAi_UsesDeploymentName()
	{
		var settings = AiImportTestHelpers.OnlineSettings("azure-openai", "ignored", "my-gpt-4o-deployment");
		var profile = AiImportBatchPlanResolver.Resolve(settings);
		Assert.Equal(10_000, profile.MaxInputChars);
	}

	[Fact]
	public void BuildPlan_JohnDoeFixture_GemmaCompact_HasAtLeastFiveBatches()
	{
		var text = SampleCvText.JohnDoeMultiSection();
		var segmentation = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));
		var profile = AiImportBatchPlanResolver.Resolve(AiImportTestHelpers.LocalSettings("gemma2-2b"));
		var plan = AiImportBatchPlanResolver.BuildPlan(text, segmentation, profile);
		Assert.True(plan.TotalBatchCount >= 5);
	}
}

internal static class SampleCvText
{
	public static string JohnDoeMultiSection() =>
		"""
        John Doe
        john.doe@example.com
        +1 555 010 2030
        San Francisco, CA

        SUMMARY
        Senior engineer with distributed systems experience across fintech and healthcare platforms.

        WORK EXPERIENCE
        Acme Corp — Senior Engineer — 2020 – Present
        Built APIs and led migration to cloud-native architecture.

        Globex Inc — Software Engineer — 2016 – 2020
        Delivered payment processing services and observability tooling.

        EDUCATION
        MIT — B.S. Computer Science — 2012 – 2016

        SKILLS
        C#, TypeScript, PostgreSQL, Kubernetes

        LANGUAGES
        English — Native
        Slovak — Professional

        CERTIFICATES
        AWS Solutions Architect — 2021

        PROJECTS
        Open Source CLI — Developer tooling for CI pipelines

        LINKS
        LinkedIn: https://linkedin.com/in/john-doe

        ADDITIONAL
        Open to remote leadership roles.
        """;

	public static string GarbledCv(int length = 2000) =>
		string.Concat(Enumerable.Repeat("Experience at Company ABC 2019 engineer project skill language ", length / 60));

	public static string SlovakNameSample() =>
		"""
        Ján Horváth
        jan.horvath@example.sk
        Bratislava, Slovakia

        PRACOVNÉ SKÚSENOSTI
        SoftCorp — Vedúci vývojár — 2018 – súčasnosť
        """ + new string('x', 100);
}
