using ReVitae.Core.Ai;

namespace ReVitae.Core.Projects;

public static class CvProjectService
{
	public static void Save(string filePath, CvProjectSaveRequest request) =>
		CvProjectSerializer.Save(filePath, request);

	public static CvProjectLoadResult Load(string filePath) =>
		CvProjectSerializer.Load(filePath);

	public static void WriteRecovery(CvProjectSaveRequest request) =>
		CvProjectSerializer.Save(ReVitaeLocalDataPaths.GetProjectAutosaveRecoveryPath(), request);

	public static void DeleteRecovery()
	{
		var path = ReVitaeLocalDataPaths.GetProjectAutosaveRecoveryPath();
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	public static bool RecoveryExists()
	{
		var path = ReVitaeLocalDataPaths.GetProjectAutosaveRecoveryPath();
		return File.Exists(path) && new FileInfo(path).Length > 0;
	}

	public static CvProjectLoadResult LoadRecovery() =>
		CvProjectSerializer.Load(ReVitaeLocalDataPaths.GetProjectAutosaveRecoveryPath());
}
