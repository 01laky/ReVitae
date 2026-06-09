using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvAdvisorCacheEdgeCaseTests
{
	private static AiCvAdvisorResult Ok() =>
		new(true, [new AiCvAdvisorSuggestion("tip")], null, null, CvImportSectionId.Skills);

	[Fact]
	public void ComputeKey_TargetContextChangesKey()
	{
		var noTarget = AiCvAdvisorCache.ComputeKey(CvImportSectionId.Skills, "C#", null, "en");
		var withTarget = AiCvAdvisorCache.ComputeKey(
			CvImportSectionId.Skills, "C#", new AiCvTargetContext("Engineer", null), "en");
		Assert.NotEqual(noTarget, withTarget);
	}

	[Fact]
	public void ComputeKey_CultureChangesKey()
	{
		var en = AiCvAdvisorCache.ComputeKey(CvImportSectionId.Skills, "C#", null, "en");
		var sk = AiCvAdvisorCache.ComputeKey(CvImportSectionId.Skills, "C#", null, "sk");
		Assert.NotEqual(en, sk);
	}

	[Fact]
	public void ComputeKey_SectionChangesKey()
	{
		var skills = AiCvAdvisorCache.ComputeKey(CvImportSectionId.Skills, "X", null, "en");
		var work = AiCvAdvisorCache.ComputeKey(CvImportSectionId.WorkExperience, "X", null, "en");
		Assert.NotEqual(skills, work);
	}

	[Fact]
	public void Set_SameKeyTwice_UpdatesValueWithoutGrowing()
	{
		var cache = new AiCvAdvisorCache();
		cache.Set("k", Ok());
		cache.Set("k", Ok() with { Suggestions = [new AiCvAdvisorSuggestion("new")] });

		Assert.Equal(1, cache.Count);
		Assert.True(cache.TryGet("k", out var cached));
		Assert.Equal("new", cached.Suggestions[0].Text);
	}

	[Fact]
	public void Capacity_One_KeepsOnlyNewest()
	{
		var cache = new AiCvAdvisorCache(capacity: 1);
		cache.Set("a", Ok());
		cache.Set("b", Ok());

		Assert.False(cache.TryGet("a", out _));
		Assert.True(cache.TryGet("b", out _));
		Assert.Equal(1, cache.Count);
	}

	[Fact]
	public void Capacity_BelowOne_ClampedToOne()
	{
		var cache = new AiCvAdvisorCache(capacity: 0);
		cache.Set("a", Ok());
		Assert.Equal(1, cache.Count);
	}

	[Fact]
	public void Set_CancelledResult_NotCached()
	{
		var cache = new AiCvAdvisorCache();
		cache.Set("k", AiCvAdvisorResult.CancelledResult(CvImportSectionId.Skills));
		Assert.False(cache.TryGet("k", out _));
	}

	[Fact]
	public void TryGet_DoesNotMutateStoredFromCacheFlag()
	{
		var cache = new AiCvAdvisorCache();
		cache.Set("k", Ok());
		Assert.True(cache.TryGet("k", out var first));
		Assert.True(cache.TryGet("k", out var second));
		Assert.True(first.FromCache);
		Assert.True(second.FromCache);
	}

	[Fact]
	public void TryGet_RefreshesLruOrdering()
	{
		var cache = new AiCvAdvisorCache(capacity: 2);
		cache.Set("a", Ok());
		cache.Set("b", Ok());
		// Access "a" so "b" becomes least-recently-used.
		cache.TryGet("a", out _);
		cache.Set("c", Ok());

		Assert.True(cache.TryGet("a", out _));
		Assert.False(cache.TryGet("b", out _));
	}
}
