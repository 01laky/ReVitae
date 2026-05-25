using System;
using System.Collections.Generic;
using ReVitae.Core.Quality;

namespace ReVitae.Ui.Quality;

public sealed class QualityHintDismissalStore
{
    private readonly HashSet<string> _dismissedKeys = new(StringComparer.Ordinal);

    public IReadOnlySet<string> DismissedKeys => _dismissedKeys;

    public void Dismiss(CvQualityHint hint) => _dismissedKeys.Add(CvQualityAnalyzer.BuildDismissKey(hint));

    public void Restore(IEnumerable<string> keys)
    {
        _dismissedKeys.Clear();
        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _dismissedKeys.Add(key);
            }
        }
    }

    public void Clear() => _dismissedKeys.Clear();
}
