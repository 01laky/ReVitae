using ReVitae.Core.Ai.Import;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiImportSourceChunkerTests
{
	[Fact]
	public void BuildBatches_CompactProfile_NeverExceedsMaxInputSlice()
	{
		var chunker = new AiImportSourceChunker(AiImportBatchProfile.Compact);
		var text = SampleCvText.JohnDoeMultiSection();
		var segmentation = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));
		var batches = chunker.BuildBatches(text, segmentation);
		var maxSlice = chunker.MaxSliceChars(0);
		Assert.All(batches, batch => Assert.True(batch.SliceText.Length <= maxSlice + 8));
	}

	[Fact]
	public void BuildBatches_GarbledCv_UsesOverlappingWindowsForCompact()
	{
		var chunker = new AiImportSourceChunker(AiImportBatchProfile.Compact);
		var text = SampleCvText.GarbledCv(5000);
		var batches = chunker.BuildBatches(text, AiImportTestHelpers.EmptySegmentation());
		Assert.NotEmpty(batches);
		Assert.True(batches.Count >= 2);
	}

	[Fact]
	public void TruncateSourceText_FiftyThousandChars_TruncatesBeforePlan()
	{
		var longText = new string('a', 150_000);
		var truncated = AiImportLimits.TruncateSourceText(longText);
		Assert.Equal(AiImportLimits.MaxSourceChars, truncated.Length);
	}

	[Fact]
	public void BuildBatches_WorkSectionTwentyEntries_Compact_CreatesManyWorkBatches()
	{
		var workBody = string.Join(
			"\n\n",
			Enumerable.Range(1, 20).Select(i => $"Company {i}\nEngineer\n201{i % 10} – 202{i % 10}"));
		var segmentation = AiImportTestHelpers.SegmentationWithWork(workBody);
		var chunker = new AiImportSourceChunker(AiImportBatchProfile.Compact);
		var batches = chunker.BuildBatches("header", segmentation);
		var workBatches = batches.Where(b => b.Phase == AiImportPhase.Work).ToList();
		Assert.Equal(20, workBatches.Count);
	}

	[Fact]
	public void BuildBatches_SmallProfile_SkillsAndLanguagesSeparatePhases()
	{
		var text = SampleCvText.JohnDoeMultiSection();
		var segmentation = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));
		var chunker = new AiImportSourceChunker(AiImportBatchProfile.Small);
		var batches = chunker.BuildBatches(text, segmentation);
		Assert.Contains(batches, b => b.Phase == AiImportPhase.Skills);
		Assert.Contains(batches, b => b.Phase == AiImportPhase.Languages);
	}
}
