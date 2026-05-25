using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ReVitae.Core.Import.Ocr;

/// <summary>Deterministic OCR stub for unit tests.</summary>
public sealed class FakeOcrEngine(string? fixedText = null) : IOcrEngine
{
	public string EngineName => "FakeOcrEngine";

	public bool IsAvailable => true;

	public OcrRecognitionResult Recognize(Image image, OcrOptions options)
	{
		return new OcrRecognitionResult(fixedText ?? string.Empty);
	}
}

/// <summary>OCR engine stub that reports unavailable (tessdata missing).</summary>
public sealed class UnavailableOcrEngine : IOcrEngine
{
	public string EngineName => "UnavailableOcrEngine";

	public bool IsAvailable => false;

	public OcrRecognitionResult Recognize(Image image, OcrOptions options) =>
		new(string.Empty);
}

/// <summary>Fixed CV-shaped text for pipeline integration tests.</summary>
public sealed class FixtureOcrEngine : IOcrEngine
{
	public const string SampleCvText = """
        Jane Doe
        jane@example.com
        +1 555 0100

        Summary
        Product designer with ten years of experience.

        Work Experience
        Senior Designer at Acme Corp
        01/2020 - Present

        Education
        BA Design, Example University
        2012 - 2016

        Skills
        Figma, Sketch, UX Research
        """;

	public string EngineName => "FixtureOcrEngine";

	public bool IsAvailable => true;

	public OcrRecognitionResult Recognize(Image image, OcrOptions options)
	{
		return new OcrRecognitionResult(SampleCvText);
	}
}

internal static class OcrImageEncoder
{
	public static byte[] EncodePng(Image image)
	{
		using var stream = new MemoryStream();
		image.SaveAsPng(stream);
		return stream.ToArray();
	}

	public static Image<Rgba32> LoadRgba(Image source)
	{
		if (source is Image<Rgba32> rgba)
		{
			return rgba;
		}

		return source.CloneAs<Rgba32>();
	}
}
