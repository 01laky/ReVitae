using System.Security.Cryptography;
using System.Text;
using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Cv;

/// <summary>
/// Session-only, in-memory LRU cache of advisor results (045 C.8). Avoids re-calling the
/// model when a section's content, target context, and culture are unchanged. Bounded;
/// the least-recently-used entry is evicted past capacity. Keys are content-derived, so a
/// section edit naturally produces a different key (a miss).
/// </summary>
public sealed class AiCvAdvisorCache
{
	public const int DefaultCapacity = 12;

	private readonly int _capacity;
	private readonly Dictionary<string, AiCvAdvisorResult> _entries = new(StringComparer.Ordinal);
	private readonly LinkedList<string> _usage = new();
	private readonly Dictionary<string, LinkedListNode<string>> _nodes = new(StringComparer.Ordinal);
	private readonly object _lock = new();

	public AiCvAdvisorCache(int capacity = DefaultCapacity)
	{
		_capacity = capacity < 1 ? 1 : capacity;
	}

	public int Count
	{
		get
		{
			lock (_lock)
			{
				return _entries.Count;
			}
		}
	}

	/// <summary>Stable key from section, content, optional target context, and culture.</summary>
	public static string ComputeKey(
		CvImportSectionId section,
		string sectionContent,
		AiCvTargetContext? targetContext,
		string culture)
	{
		var target = targetContext is null
			? string.Empty
			: $"{targetContext.Role}{targetContext.JobDescriptionExcerpt}";
		var raw = $"{(int)section}{sectionContent}{target}{culture}";
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
		return Convert.ToHexString(bytes);
	}

	public bool TryGet(string key, out AiCvAdvisorResult result)
	{
		lock (_lock)
		{
			if (_entries.TryGetValue(key, out var stored))
			{
				Touch(key);
				result = stored with { FromCache = true };
				return true;
			}

			result = default!;
			return false;
		}
	}

	public void Set(string key, AiCvAdvisorResult result)
	{
		if (!result.Succeeded)
		{
			return; // never cache failures or cancellations
		}

		lock (_lock)
		{
			if (_entries.ContainsKey(key))
			{
				_entries[key] = result with { FromCache = false };
				Touch(key);
				return;
			}

			_entries[key] = result with { FromCache = false };
			var node = _usage.AddFirst(key);
			_nodes[key] = node;

			while (_entries.Count > _capacity)
			{
				var lru = _usage.Last;
				if (lru is null)
				{
					break;
				}

				_usage.RemoveLast();
				_entries.Remove(lru.Value);
				_nodes.Remove(lru.Value);
			}
		}
	}

	public void Clear()
	{
		lock (_lock)
		{
			_entries.Clear();
			_usage.Clear();
			_nodes.Clear();
		}
	}

	private void Touch(string key)
	{
		if (_nodes.TryGetValue(key, out var node))
		{
			_usage.Remove(node);
			_usage.AddFirst(node);
		}
	}
}
