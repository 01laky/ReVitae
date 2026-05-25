using SixLabors.ImageSharp;

namespace ReVitae.Core.Import.Ocr;

public interface IPdfPageRenderer
{
	IReadOnlyList<Image> RenderPages(string filePath, int dpi);
}
