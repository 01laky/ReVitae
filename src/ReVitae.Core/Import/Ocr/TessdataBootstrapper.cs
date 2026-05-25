namespace ReVitae.Core.Import.Ocr;

/// <summary>Ensures tessdata exists under local app data when bundled resources are present.</summary>
internal static class TessdataBootstrapper
{
	public static string? EnsureDefaultTessdata()
	{
		var existing = TessdataLocator.FindTessdataDirectory();
		if (existing is not null)
		{
			return existing;
		}

		var bundledDirectory = Path.Combine(AppContext.BaseDirectory, "tessdata");
		if (!TessdataLocator.HasLanguageFile(bundledDirectory, "eng"))
		{
			CvImportDiagnosticsLogger.LogStep(
				"tessdata",
				$"No bundled tessdata at {bundledDirectory} — bootstrap skipped");
			return null;
		}

		var targetDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"ReVitae",
			"tessdata");

		CvImportDiagnosticsLogger.LogStep(
			"tessdata",
			$"Bootstrapping eng.traineddata from {bundledDirectory} → {targetDirectory}");

		try
		{
			Directory.CreateDirectory(targetDirectory);
			CopyIfMissing(
				Path.Combine(bundledDirectory, "eng.traineddata"),
				Path.Combine(targetDirectory, "eng.traineddata"));
			var result = TessdataLocator.HasLanguageFile(targetDirectory, "eng") ? targetDirectory : null;
			CvImportDiagnosticsLogger.LogStep(
				"tessdata",
				result is not null ? $"Bootstrap OK: {result}" : "Bootstrap failed — target has no eng.traineddata");
			return result;
		}
		catch (IOException ex)
		{
			CvImportDiagnosticsLogger.LogStep("tessdata", $"Bootstrap IOException: {ex.Message}");
			return TessdataLocator.HasLanguageFile(bundledDirectory, "eng") ? bundledDirectory : null;
		}
		catch (UnauthorizedAccessException ex)
		{
			CvImportDiagnosticsLogger.LogStep("tessdata", $"Bootstrap UnauthorizedAccessException: {ex.Message}");
			return TessdataLocator.HasLanguageFile(bundledDirectory, "eng") ? bundledDirectory : null;
		}
	}

	private static void CopyIfMissing(string sourcePath, string targetPath)
	{
		if (File.Exists(targetPath))
		{
			CvImportDiagnosticsLogger.LogStep("tessdata", $"Already present: {targetPath}");
			return;
		}

		if (!File.Exists(sourcePath))
		{
			CvImportDiagnosticsLogger.LogStep("tessdata", $"Source missing: {sourcePath}");
			return;
		}

		File.Copy(sourcePath, targetPath);
		CvImportDiagnosticsLogger.LogStep("tessdata", $"Copied {sourcePath} → {targetPath}");
	}
}
