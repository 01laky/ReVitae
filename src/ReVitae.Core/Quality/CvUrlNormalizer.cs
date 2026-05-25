namespace ReVitae.Core.Quality;

public static class CvUrlNormalizer
{
	public static string NormalizeForComparison(string? url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return string.Empty;
		}

		var trimmed = url.Trim();
		if (trimmed.Length == 0)
		{
			return string.Empty;
		}

		if (!trimmed.Contains("://", StringComparison.Ordinal))
		{
			trimmed = "https://" + trimmed;
		}

		if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
		{
			return trimmed.TrimEnd('/').ToLowerInvariant();
		}

		var host = uri.Host.ToLowerInvariant();
		if (host.StartsWith("www.", StringComparison.Ordinal))
		{
			host = host[4..];
		}

		var path = uri.AbsolutePath.TrimEnd('/');
		var normalized = $"{uri.Scheme.ToLowerInvariant()}://{host}{path}";
		if (!string.IsNullOrEmpty(uri.Query))
		{
			normalized += uri.Query;
		}

		return normalized;
	}

	public static bool AreEquivalent(string? left, string? right)
	{
		var normalizedLeft = NormalizeForComparison(left);
		var normalizedRight = NormalizeForComparison(right);
		return normalizedLeft.Length > 0
			&& normalizedRight.Length > 0
			&& string.Equals(normalizedLeft, normalizedRight, StringComparison.Ordinal);
	}
}
