using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvAdvisorCacheTests
{
	[Fact]
	public void ComputeKey_SameInputs_ProducesSameKey()
	{
		var a = AiCvAdvisorCache.ComputeKey(CvImportSectionId.Skills, "C#, SQL", null, "en");
		var b = AiCvAdvisorCache.ComputeKey(CvImportSectionId.Skills, "C#, SQL", null, "en");
		Assert.Equal(a, b);
	}

	[Fact]
	public void ComputeKey_DifferentContent_ProducesDifferentKey()
	{
		var a = AiCvAdvisorCache.ComputeKey(CvImportSectionId.Skills, "C#, SQL", null, "en");
		var b = AiCvAdvisorCache.ComputeKey(CvImportSectionId.Skills, "C#, SQL, Go", null, "en");
		Assert.NotEqual(a, b);
	}

	[Fact]
	public void TryGet_AfterSet_ReturnsCachedWithFlag()
	{
		var cache = new AiCvAdvisorCache();
		var key = AiCvAdvisorCache.ComputeKey(CvImportSectionId.Skills, "C#", null, "en");
		var result = new AiCvAdvisorResult(true, [new AiCvAdvisorSuggestion("Group skills")], null, null, CvImportSectionId.Skills);

		cache.Set(key, result);

		Assert.True(cache.TryGet(key, out var cached));
		Assert.True(cached.FromCache);
		Assert.Single(cached.Suggestions);
	}

	[Fact]
	public void TryGet_Miss_ReturnsFalse()
	{
		var cache = new AiCvAdvisorCache();
		Assert.False(cache.TryGet("nope", out _));
	}

	[Fact]
	public void Set_DoesNotCacheFailures()
	{
		var cache = new AiCvAdvisorCache();
		cache.Set("k", AiCvAdvisorResult.Fail("err", CvImportSectionId.Skills));
		Assert.False(cache.TryGet("k", out _));
		Assert.Equal(0, cache.Count);
	}

	[Fact]
	public void Set_BeyondCapacity_EvictsLeastRecentlyUsed()
	{
		var cache = new AiCvAdvisorCache(capacity: 2);
		var r = new AiCvAdvisorResult(true, [new AiCvAdvisorSuggestion("x")], null, null, CvImportSectionId.Skills);

		cache.Set("a", r);
		cache.Set("b", r);
		cache.TryGet("a", out _); // touch "a" so "b" is now LRU
		cache.Set("c", r);

		Assert.True(cache.TryGet("a", out _));
		Assert.False(cache.TryGet("b", out _));
		Assert.True(cache.TryGet("c", out _));
		Assert.Equal(2, cache.Count);
	}

	[Fact]
	public void Clear_RemovesEntries()
	{
		var cache = new AiCvAdvisorCache();
		cache.Set("a", new AiCvAdvisorResult(true, [new AiCvAdvisorSuggestion("x")], null, null, CvImportSectionId.Skills));
		cache.Clear();
		Assert.Equal(0, cache.Count);
	}
}
