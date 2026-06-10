using ReVitae.Ui;

namespace ReVitae.Tests.Ui;

public sealed class SectionEntryReorderEdgeCaseTests
{
	private sealed record Item(string Id);

	private static List<Item> Make(params string[] ids) => ids.Select(id => new Item(id)).ToList();

	private static IReadOnlyList<string> Ids(List<Item> list) => list.Select(i => i.Id).ToList();

	[Fact]
	public void MoveToIndex_DuplicateIds_MovesFirstOccurrence()
	{
		var list = Make("dup", "x", "dup", "y");
		var moved = SectionEntryReorder.MoveToIndex(list, i => i.Id, "dup", 3);

		Assert.True(moved);
		// First "dup" (index 0) removed and re-inserted; the second "dup" stays.
		Assert.Equal(new[] { "x", "dup", "dup", "y" }, Ids(list));
	}

	[Fact]
	public void MoveToIndex_LastToLast_SameIndex_NoChange()
	{
		var list = Make("a", "b", "c");
		Assert.False(SectionEntryReorder.MoveToIndex(list, i => i.Id, "c", 2));
		Assert.Equal(new[] { "a", "b", "c" }, Ids(list));
	}

	[Fact]
	public void MoveToIndex_MiddleToMiddle_LargeList()
	{
		var list = Make(Enumerable.Range(0, 50).Select(i => i.ToString()).ToArray());
		var moved = SectionEntryReorder.MoveToIndex(list, i => i.Id, "10", 30);

		Assert.True(moved);
		Assert.Equal(50, list.Count);
		// Removing index 10 then re-inserting at the decremented target (29) lands "10" at 29.
		Assert.DoesNotContain("10", Ids(list).Take(29));
		Assert.Equal("10", list[29].Id);
	}

	[Fact]
	public void MoveToIndex_TwoElementSwapBothDirections()
	{
		var list = Make("a", "b");
		// Move "b" to the front.
		Assert.True(SectionEntryReorder.MoveToIndex(list, i => i.Id, "b", 0));
		Assert.Equal(new[] { "b", "a" }, Ids(list));
		// Move "b" to the end to swap back (a forward move to count clamps to last).
		Assert.True(SectionEntryReorder.MoveToIndex(list, i => i.Id, "b", 2));
		Assert.Equal(new[] { "a", "b" }, Ids(list));
	}

	[Fact]
	public void MoveToIndex_FromFirstToEnd_Clamped()
	{
		var list = Make("a", "b", "c", "d");
		SectionEntryReorder.MoveToIndex(list, i => i.Id, "a", list.Count);
		Assert.Equal(new[] { "b", "c", "d", "a" }, Ids(list));
	}

	[Fact]
	public void MoveToIndex_PreservesOtherElementsOrder()
	{
		var list = Make("a", "b", "c", "d", "e");
		SectionEntryReorder.MoveToIndex(list, i => i.Id, "c", 0);
		Assert.Equal(new[] { "c", "a", "b", "d", "e" }, Ids(list));
	}

	[Fact]
	public void MoveToIndex_IdSelectorReturningNull_MatchesNullSourceFalse()
	{
		// idSelector yields null; sourceEntryId is blank → no move (blank short-circuits).
		var list = Make("a", "b");
		Assert.False(SectionEntryReorder.MoveToIndex(list, _ => null!, "", 0));
	}

	[Fact]
	public void FindIndexById_DuplicateIds_ReturnsFirst()
	{
		var list = Make("a", "dup", "dup", "b");
		Assert.Equal(1, SectionEntryReorder.FindIndexById(list, i => i.Id, "dup"));
	}

	[Fact]
	public void FindIndexById_CaseSensitive()
	{
		var list = Make("Abc");
		Assert.Null(SectionEntryReorder.FindIndexById(list, i => i.Id, "abc"));
		Assert.Equal(0, SectionEntryReorder.FindIndexById(list, i => i.Id, "Abc"));
	}

	[Fact]
	public void FindIndexById_LastElement()
	{
		var list = Make("a", "b", "c");
		Assert.Equal(2, SectionEntryReorder.FindIndexById(list, i => i.Id, "c"));
	}

	[Fact]
	public void MoveToIndex_SingleElementToZero_NoChange()
	{
		var list = Make("only");
		Assert.False(SectionEntryReorder.MoveToIndex(list, i => i.Id, "only", 0));
		Assert.Single(list);
	}
}
