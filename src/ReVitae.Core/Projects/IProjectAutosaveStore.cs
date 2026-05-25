namespace ReVitae.Core.Projects;

public interface IProjectAutosaveStore
{
	void WriteRecovery(CvProjectSaveRequest request);

	void DeleteRecovery();

	bool RecoveryExists();

	CvProjectLoadResult LoadRecovery();

	DateTimeOffset? GetRecoveryLastWriteUtc();
}

public sealed class FileProjectAutosaveStore : IProjectAutosaveStore
{
	public void WriteRecovery(CvProjectSaveRequest request) =>
		CvProjectService.WriteRecovery(request);

	public void DeleteRecovery() =>
		CvProjectService.DeleteRecovery();

	public bool RecoveryExists() =>
		CvProjectService.RecoveryExists();

	public CvProjectLoadResult LoadRecovery() =>
		CvProjectService.LoadRecovery();

	public DateTimeOffset? GetRecoveryLastWriteUtc()
	{
		var path = ReVitae.Core.Ai.ReVitaeLocalDataPaths.GetProjectAutosaveRecoveryPath();
		if (!File.Exists(path))
		{
			return null;
		}

		return File.GetLastWriteTimeUtc(path);
	}
}
