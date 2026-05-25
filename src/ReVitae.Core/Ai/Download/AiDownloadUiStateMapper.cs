namespace ReVitae.Core.Ai.Download;

public static class AiDownloadUiStateMapper
{
	public static bool ShouldShowDock(
		AiDownloadJobState state,
		bool isIntroVisible,
		bool isAiModalVisible)
	{
		if (isIntroVisible)
		{
			return false;
		}

		return state is AiDownloadJobState.Downloading
			or AiDownloadJobState.Paused
			or AiDownloadJobState.Interrupted
			or AiDownloadJobState.Failed
			or AiDownloadJobState.Completed;
	}

	public static bool ShouldShowHeaderBadge(AiDownloadJobState state, bool isAiModalVisible)
	{
		if (isAiModalVisible)
		{
			return false;
		}

		return state is AiDownloadJobState.Downloading or AiDownloadJobState.Interrupted;
	}

	public static bool HasActiveJob(AiDownloadJobState state) =>
		state is AiDownloadJobState.Downloading
			or AiDownloadJobState.Paused
			or AiDownloadJobState.Interrupted
			or AiDownloadJobState.Failed
			or AiDownloadJobState.Completed;
}
