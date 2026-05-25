namespace ReVitae.Core.Import.Ocr;

/// <summary>Maps UI culture to Tesseract language packs available on disk.</summary>
public static class OcrLanguageResolver
{
	public static string ResolveLanguages(string? uiLanguageCode)
	{
		var tessdataDirectory = TessdataLocator.FindTessdataDirectory();
		if (tessdataDirectory is null)
		{
			return "eng";
		}

		var languages = new List<string> { "eng" };
		var culture = NormalizeCulture(uiLanguageCode);

		foreach (var (culturePrefix, tesseractCode) in AdditionalLanguageMap)
		{
			if (!culture.StartsWith(culturePrefix, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (TessdataLocator.HasLanguageFile(tessdataDirectory, tesseractCode))
			{
				languages.Add(tesseractCode);
			}
			else
			{
				CvImportDiagnosticsLogger.LogStep(
					"ocr-languages",
					$"Optional pack '{tesseractCode}.traineddata' not found — using eng only for culture {culture}");
			}

			break;
		}

		return string.Join('+', languages);
	}

	private static string NormalizeCulture(string? uiLanguageCode)
	{
		if (string.IsNullOrWhiteSpace(uiLanguageCode))
		{
			return "en";
		}

		return uiLanguageCode.Trim();
	}

	private static readonly (string CulturePrefix, string TesseractCode)[] AdditionalLanguageMap =
	[
		("sk", "slk"),
		("cs", "ces"),
	];
}
