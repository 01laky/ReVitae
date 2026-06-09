using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvApplyUndoBufferEdgeCaseTests
{
	private static AiCvFieldTarget T(string key) =>
		new(CvImportSectionId.PersonalInformation, key);

	[Fact]
	public void Restore_Twice_SecondIsEmpty()
	{
		var buffer = new AiCvApplyUndoBuffer();
		buffer.CaptureSingle(T("a"), "old");

		Assert.Single(buffer.Restore());
		Assert.Empty(buffer.Restore());
		Assert.False(buffer.CanUndo);
	}

	[Fact]
	public void Restore_NothingCaptured_ReturnsEmpty()
	{
		var buffer = new AiCvApplyUndoBuffer();
		Assert.Empty(buffer.Restore());
	}

	[Fact]
	public void CaptureBatch_ThenSingle_ReplacesWholeBatch()
	{
		var buffer = new AiCvApplyUndoBuffer();
		buffer.Capture([new AiCvFieldValueSnapshot(T("a"), "1"), new AiCvFieldValueSnapshot(T("b"), "2")]);
		buffer.CaptureSingle(T("c"), "3");

		var restored = buffer.Restore();
		Assert.Single(restored);
		Assert.Equal("c", restored[0].Target.FieldKey);
	}

	[Fact]
	public void Capture_PreservesPriorEmptyValue()
	{
		var buffer = new AiCvApplyUndoBuffer();
		buffer.CaptureSingle(T("a"), string.Empty);
		// An empty prior value is still a valid undo target.
		Assert.True(buffer.CanUndo);
		Assert.Equal(string.Empty, buffer.Restore()[0].PriorValue);
	}

	[Fact]
	public void Clear_AfterCapture_NothingToRestore()
	{
		var buffer = new AiCvApplyUndoBuffer();
		buffer.Capture([new AiCvFieldValueSnapshot(T("a"), "x")]);
		buffer.Clear();
		Assert.False(buffer.CanUndo);
		Assert.Empty(buffer.Restore());
	}

	[Fact]
	public void Capture_EmptyEnumerable_LeavesNothing()
	{
		var buffer = new AiCvApplyUndoBuffer();
		buffer.Capture(System.Array.Empty<AiCvFieldValueSnapshot>());
		Assert.False(buffer.CanUndo);
	}
}
