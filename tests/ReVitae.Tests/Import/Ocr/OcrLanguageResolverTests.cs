using ReVitae.Core.Import.Ocr;

namespace ReVitae.Tests.Import.Ocr;

public sealed class OcrLanguageResolverTests
{
	[Theory]
	[InlineData("en", "eng")]
	[InlineData("sk", "eng")]
	[InlineData(null, "eng")]
	public void ResolveLanguages_DefaultsToEnglishWhenOptionalPackMissing(string? culture, string expectedPrefix)
	{
		var languages = OcrLanguageResolver.ResolveLanguages(culture);
		Assert.StartsWith(expectedPrefix, languages, StringComparison.Ordinal);
		Assert.DoesNotContain('+', languages);
	}

	[Fact]
	public void ResolveLanguages_WhenSlkPackPresentInActiveTessdata_ReturnsEngPlusSlk()
	{
		var tessdataDirectory = Path.Combine(AppContext.BaseDirectory, "tessdata");
		var slkPath = Path.Combine(tessdataDirectory, "slk.traineddata");
		var createdSlk = false;

		if (!File.Exists(slkPath))
		{
			Directory.CreateDirectory(tessdataDirectory);
			File.WriteAllBytes(slkPath, [0x00]);
			createdSlk = true;
		}

		try
		{
			var languages = OcrLanguageResolver.ResolveLanguages("sk");
			Assert.Equal("eng+slk", languages);
		}
		finally
		{
			if (createdSlk && File.Exists(slkPath))
			{
				File.Delete(slkPath);
			}
		}
	}

	[Fact]
	public void ResolveLanguages_CsCulture_AppendsCesWhenPackPresent()
	{
		var tessdataDirectory = Path.Combine(AppContext.BaseDirectory, "tessdata");
		var cesPath = Path.Combine(tessdataDirectory, "ces.traineddata");
		var createdCes = false;

		if (!File.Exists(cesPath))
		{
			Directory.CreateDirectory(tessdataDirectory);
			File.WriteAllBytes(cesPath, [0x00]);
			createdCes = true;
		}

		try
		{
			var languages = OcrLanguageResolver.ResolveLanguages("cs");
			Assert.Equal("eng+ces", languages);
		}
		finally
		{
			if (createdCes && File.Exists(cesPath))
			{
				File.Delete(cesPath);
			}
		}
	}
}
