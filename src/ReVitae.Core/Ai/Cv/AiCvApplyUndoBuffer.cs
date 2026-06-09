namespace ReVitae.Core.Ai.Cv;

/// <summary>Prior value of a single field, captured before an AI write (045 C.6).</summary>
public sealed record AiCvFieldValueSnapshot(AiCvFieldTarget Target, string PriorValue);

/// <summary>
/// Single-level undo buffer for AI writes (045 C.6): advisor Apply, Improve-with-AI Accept,
/// and field repair (which captures the whole batch). Capturing replaces any prior capture
/// (one level only); <see cref="Restore"/> hands back the captured snapshots and clears the
/// buffer; <see cref="Clear"/> is called on the next manual edit. Session-only, no UI coupling.
/// </summary>
public sealed class AiCvApplyUndoBuffer
{
	private readonly object _lock = new();
	private IReadOnlyList<AiCvFieldValueSnapshot>? _captured;

	public bool CanUndo
	{
		get
		{
			lock (_lock)
			{
				return _captured is { Count: > 0 };
			}
		}
	}

	public void Capture(IEnumerable<AiCvFieldValueSnapshot> priorValues)
	{
		var list = priorValues.ToList();
		lock (_lock)
		{
			_captured = list.Count > 0 ? list : null;
		}
	}

	public void CaptureSingle(AiCvFieldTarget target, string priorValue) =>
		Capture([new AiCvFieldValueSnapshot(target, priorValue)]);

	/// <summary>Returns the captured snapshots and clears the buffer; empty if nothing to undo.</summary>
	public IReadOnlyList<AiCvFieldValueSnapshot> Restore()
	{
		lock (_lock)
		{
			var captured = _captured ?? [];
			_captured = null;
			return captured;
		}
	}

	public void Clear()
	{
		lock (_lock)
		{
			_captured = null;
		}
	}
}
