using ReVitae.Core.Localization;

namespace ReVitae.Core.Export.Images;

public sealed record CvImagePageRangeResult(
	bool IsValid,
	IReadOnlyList<int> PageIndices,
	string? ErrorMessageKey = null)
{
	public static CvImagePageRangeResult Valid(IReadOnlyList<int> indices) =>
		new(true, indices);

	public static CvImagePageRangeResult Invalid(string errorMessageKey) =>
		new(false, [], errorMessageKey);
}

public static class CvImagePageRangeResolver
{
	public static CvImagePageRangeResult Resolve(int totalPages, CvImagePageRange range)
	{
		if (totalPages <= 0)
		{
			return CvImagePageRangeResult.Invalid(TranslationKeys.ExportImageRasterFailed);
		}

		if (totalPages > CvImageExportLimits.MaxPageCount)
		{
			return CvImagePageRangeResult.Invalid(TranslationKeys.ExportImageTooManyPages);
		}

		if (range.IsAllPages)
		{
			return CvImagePageRangeResult.Valid(Enumerable.Range(1, totalPages).ToArray());
		}

		var from = range.FromPage;
		var to = range.ToPage;
		if (from is null or < 1 || to is null or < 1 || from > to || to > totalPages)
		{
			return CvImagePageRangeResult.Invalid(TranslationKeys.ExportImageRangeInvalid);
		}

		var indices = Enumerable.Range(from.Value, to.Value - from.Value + 1).ToArray();
		return CvImagePageRangeResult.Valid(indices);
	}
}
