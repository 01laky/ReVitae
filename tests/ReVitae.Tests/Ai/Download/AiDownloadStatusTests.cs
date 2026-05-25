using ReVitae.Core.Ai.Download;

namespace ReVitae.Tests.Ai.Download;

public sealed class AiDownloadStatusTests
{
	[Fact]
	public void FromTranslationKey_RoundTripsThroughTryGetTranslationKey()
	{
		const string key = "aiDownload.preparingEngine";
		var statusText = AiDownloadStatus.FromTranslationKey(key);

		Assert.True(AiDownloadStatus.TryGetTranslationKey(statusText, out var parsedKey));
		Assert.Equal(key, parsedKey);
	}

	[Fact]
	public void TryGetTranslationKey_ReturnsFalseForPlainStatus()
	{
		Assert.False(AiDownloadStatus.TryGetTranslationKey("pulling manifest", out _));
	}
}
