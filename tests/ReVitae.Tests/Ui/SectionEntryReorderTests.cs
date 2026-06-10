using ReVitae.Ui;

namespace ReVitae.Tests.Ui;

public sealed class SectionEntryReorderTests
{
	private sealed record Item(string Id);

	private static List<Item> Make(params string[] ids) => ids.Select(id => new Item(id)).ToList();

	private static IReadOnlyList<string> Ids(List<Item> list) => list.Select(i => i.Id).ToList();

	[Fact]
	public void MoveToIndex_ForwardMove_AccountsForRemovalShift()
	{
		var list = Make("a", "b", "c", "d", "e");
		var moved = SectionEntryReorder.MoveToIndex(list, i => i.Id, "a", 3);

		Assert.True(moved);
		Assert.Equal(new[] { "b", "c", "a", "d", "e" }, Ids(list));
	}

	[Fact]
	public void MoveToIndex_BackwardMove_InsertsBeforeTarget()
	{
		var list = Make("a", "b", "c", "d");
		var moved = SectionEntryReorder.MoveToIndex(list, i => i.Id, "d", 1);

		Assert.True(moved);
		Assert.Equal(new[] { "a", "d", "b", "c" }, Ids(list));
	}

	[Fact]
	public void MoveToIndex_ToStart()
	{
		var list = Make("a", "b", "c");
		SectionEntryReorder.MoveToIndex(list, i => i.Id, "c", 0);
		Assert.Equal(new[] { "c", "a", "b" }, Ids(list));
	}

	[Fact]
	public void MoveToIndex_BeyondEnd_ClampsToLast()
	{
		var list = Make("a", "b", "c");
		var moved = SectionEntryReorder.MoveToIndex(list, i => i.Id, "a", 99);
		Assert.True(moved);
		Assert.Equal(new[] { "b", "c", "a" }, Ids(list));
	}

	[Fact]
	public void MoveToIndex_NegativeTarget_ClampsToStart()
	{
		var list = Make("a", "b", "c");
		SectionEntryReorder.MoveToIndex(list, i => i.Id, "c", -5);
		Assert.Equal(new[] { "c", "a", "b" }, Ids(list));
	}

	[Fact]
	public void MoveToIndex_SameIndex_NoChange()
	{
		var list = Make("a", "b", "c");
		var moved = SectionEntryReorder.MoveToIndex(list, i => i.Id, "b", 1);
		Assert.False(moved);
		Assert.Equal(new[] { "a", "b", "c" }, Ids(list));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void MoveToIndex_BlankSourceId_ReturnsFalse(string? sourceId)
	{
		var list = Make("a", "b");
		Assert.False(SectionEntryReorder.MoveToIndex(list, i => i.Id, sourceId, 0));
		Assert.Equal(new[] { "a", "b" }, Ids(list));
	}

	[Fact]
	public void MoveToIndex_UnknownSourceId_ReturnsFalse()
	{
		var list = Make("a", "b");
		Assert.False(SectionEntryReorder.MoveToIndex(list, i => i.Id, "zzz", 0));
		Assert.Equal(new[] { "a", "b" }, Ids(list));
	}

	[Fact]
	public void MoveToIndex_SingleElement_SameIndex_NoChange()
	{
		var list = Make("only");
		Assert.False(SectionEntryReorder.MoveToIndex(list, i => i.Id, "only", 0));
	}

	[Fact]
	public void MoveToIndex_NullArguments_Throw()
	{
		Assert.Throws<ArgumentNullException>(() =>
			SectionEntryReorder.MoveToIndex<Item>(null!, i => i.Id, "a", 0));
		Assert.Throws<ArgumentNullException>(() =>
			SectionEntryReorder.MoveToIndex(Make("a"), null!, "a", 0));
	}

	[Fact]
	public void MoveToIndex_AdjacentSwapForward()
	{
		var list = Make("a", "b", "c");
		SectionEntryReorder.MoveToIndex(list, i => i.Id, "a", 1);
		// Removing index 0 then inserting at adjusted index 0 keeps order — no visible swap.
		Assert.Equal(new[] { "a", "b", "c" }, Ids(list));
	}

	[Fact]
	public void FindIndexById_ExistingId_ReturnsIndex()
	{
		var list = Make("a", "b", "c");
		Assert.Equal(1, SectionEntryReorder.FindIndexById(list, i => i.Id, "b"));
		Assert.Equal(0, SectionEntryReorder.FindIndexById(list, i => i.Id, "a"));
		Assert.Equal(2, SectionEntryReorder.FindIndexById(list, i => i.Id, "c"));
	}

	[Fact]
	public void FindIndexById_UnknownId_ReturnsNull()
	{
		var list = Make("a", "b");
		Assert.Null(SectionEntryReorder.FindIndexById(list, i => i.Id, "zzz"));
	}

	[Fact]
	public void FindIndexById_NullId_ReturnsNull()
	{
		var list = Make("a", "b");
		Assert.Null(SectionEntryReorder.FindIndexById(list, i => i.Id, null));
	}

	[Fact]
	public void FindIndexById_EmptyList_ReturnsNull()
	{
		Assert.Null(SectionEntryReorder.FindIndexById(new List<Item>(), i => i.Id, "a"));
	}

	[Fact]
	public void FindIndexById_FirstMatchWins()
	{
		var list = Make("dup", "dup", "x");
		Assert.Equal(0, SectionEntryReorder.FindIndexById(list, i => i.Id, "dup"));
	}

	[Fact]
	public void FindIndexById_NullArguments_Throw()
	{
		Assert.Throws<ArgumentNullException>(() =>
			SectionEntryReorder.FindIndexById<Item>(null!, i => i.Id, "a"));
		Assert.Throws<ArgumentNullException>(() =>
			SectionEntryReorder.FindIndexById(Make("a"), null!, "a"));
	}
}
