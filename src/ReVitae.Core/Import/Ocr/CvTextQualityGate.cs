namespace ReVitae.Core.Import.Ocr;

public sealed record CvTextQualityGateResult(
	bool IsUsable,
	int NonWhitespaceCount,
	double? AverageNonWhitespacePerPage,
	string? RejectReason);

/// <summary>Decides whether PdfPig text is sufficient or OCR fallback should run.</summary>
public static class CvTextQualityGate
{
	private const int MinimumNonWhitespaceCharacters = 40;
	private const int MinimumAverageCharactersPerPage = 8;

	public static bool IsUsable(string? text, int? pageCount) =>
		Evaluate(text, pageCount).IsUsable;

	public static CvTextQualityGateResult Evaluate(string? text, int? pageCount)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return new CvTextQualityGateResult(false, 0, null, "text is null or whitespace only");
		}

		var nonWhitespaceCount = text.Count(static character => !char.IsWhiteSpace(character));
		if (nonWhitespaceCount < MinimumNonWhitespaceCharacters)
		{
			return new CvTextQualityGateResult(
				false,
				nonWhitespaceCount,
				null,
				$"non-whitespace chars {nonWhitespaceCount} < minimum {MinimumNonWhitespaceCharacters}");
		}

		if (pageCount is > 1)
		{
			var averagePerPage = nonWhitespaceCount / (double)pageCount.Value;
			if (averagePerPage < MinimumAverageCharactersPerPage)
			{
				return new CvTextQualityGateResult(
					false,
					nonWhitespaceCount,
					averagePerPage,
					$"average {averagePerPage:F1} chars/page < minimum {MinimumAverageCharactersPerPage} " +
					$"(pages={pageCount}, total={nonWhitespaceCount})");
			}

			return new CvTextQualityGateResult(true, nonWhitespaceCount, averagePerPage, null);
		}

		return new CvTextQualityGateResult(true, nonWhitespaceCount, null, null);
	}
}
