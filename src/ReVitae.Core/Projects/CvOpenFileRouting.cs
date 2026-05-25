using ReVitae.Core.Import;

namespace ReVitae.Core.Projects;

public static class CvOpenFileRouting
{
	public static bool ShouldLoadAsSavedProject(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return false;
		}

		if (filePath.EndsWith(".revitae.json", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return CvImportFormatDetector.DetectFormat(filePath) == CvImportFormat.ReVitaeJson;
	}

	public static bool IsImportableCvFile(string filePath) =>
		CvImportFormatDetector.DetectFormat(filePath) is not CvImportFormat.Unknown;
}
