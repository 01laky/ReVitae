using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvApplyUndoBufferTests
{
	private static readonly AiCvFieldTarget Target =
		new(CvImportSectionId.WorkExperience, "workExperience.w1.description", "w1");

	[Fact]
	public void CaptureSingle_ThenRestore_ReturnsPriorValueAndClears()
	{
		var buffer = new AiCvApplyUndoBuffer();
		buffer.CaptureSingle(Target, "old text");

		Assert.True(buffer.CanUndo);

		var restored = buffer.Restore();

		Assert.Single(restored);
		Assert.Equal("old text", restored[0].PriorValue);
		Assert.False(buffer.CanUndo);
	}

	[Fact]
	public void Capture_Batch_RestoresAll()
	{
		var buffer = new AiCvApplyUndoBuffer();
		buffer.Capture(
		[
			new AiCvFieldValueSnapshot(Target, "a"),
			new AiCvFieldValueSnapshot(new AiCvFieldTarget(CvImportSectionId.Skills, "skills.s1", "s1"), "b"),
		]);

		var restored = buffer.Restore();
		Assert.Equal(2, restored.Count);
	}

	[Fact]
	public void Capture_Replaces_PreviousCapture_OneLevel()
	{
		var buffer = new AiCvApplyUndoBuffer();
		buffer.CaptureSingle(Target, "first");
		buffer.CaptureSingle(Target, "second");

		var restored = buffer.Restore();
		Assert.Single(restored);
		Assert.Equal("second", restored[0].PriorValue);
	}

	[Fact]
	public void Clear_RemovesCapture()
	{
		var buffer = new AiCvApplyUndoBuffer();
		buffer.CaptureSingle(Target, "old");
		buffer.Clear();
		Assert.False(buffer.CanUndo);
		Assert.Empty(buffer.Restore());
	}

	[Fact]
	public void Capture_Empty_LeavesNothingToUndo()
	{
		var buffer = new AiCvApplyUndoBuffer();
		buffer.Capture([]);
		Assert.False(buffer.CanUndo);
	}
}
