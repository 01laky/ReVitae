using SixLabors.ImageSharp;

namespace ReVitae.Core.Import.Ocr;

public sealed record OcrOptions(
    string Languages = "eng",
    int TimeoutSeconds = OcrLimits.PageTimeoutSeconds);

public sealed record OcrTextLine(string Text, int Top, int Left, int Bottom, int Right);

public sealed record OcrRecognitionResult(
    string Text,
    IReadOnlyList<OcrTextLine>? Lines = null);

public interface IOcrEngine
{
    string EngineName { get; }

    bool IsAvailable { get; }

    OcrRecognitionResult Recognize(Image image, OcrOptions options);
}
