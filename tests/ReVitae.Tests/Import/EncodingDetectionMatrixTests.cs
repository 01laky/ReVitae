using System.Text;
using ReVitae.Core.Import.Extraction;

namespace ReVitae.Tests.Import;

/// <summary>
/// Prompt 049 B16 — import encoding-detection matrix. The same diacritic-rich CV encoded in
/// each supported encoding must survive <see cref="PlainTextExtractor"/>'s BOM / UTF-8 /
/// Windows-1250 probe, or at minimum degrade to a clean non-empty draft — never crash or
/// silently lose characters. High value for the Slovak/Czech user base.
/// </summary>
[Trait("Category", "ImportExtraction")]
public sealed class EncodingDetectionMatrixTests : IDisposable
{
	private const string Sample =
		"Ladislav Kostolný\nSoftware Engineer\nKošice\nPracoval som až do súčasnosť na rôznych projektoch.";

	private readonly string _directory;

	public EncodingDetectionMatrixTests()
	{
		_directory = Path.Combine(Path.GetTempPath(), "revitae-encoding-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_directory);
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
	}

	public void Dispose()
	{
		try
		{
			Directory.Delete(_directory, recursive: true);
		}
		catch
		{
			// Best-effort cleanup.
		}
	}

	private string WriteFile(byte[] bytes)
	{
		var path = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".txt");
		File.WriteAllBytes(path, bytes);
		return path;
	}

	private string Extract(byte[] bytes)
	{
		var result = new PlainTextExtractor().Extract(WriteFile(bytes));
		Assert.True(result.Success, $"Extraction failed: {result.ErrorMessageKey}");
		return result.Text;
	}

	public static IEnumerable<object[]> DiacriticPreservingEncodings =>
	[
		["utf-8-no-bom"],
		["utf-8-bom"],
		["utf-16-le"],
		["utf-16-be"],
		["windows-1250"]
	];

	private static byte[] Encode(string label)
	{
		return label switch
		{
			"utf-8-no-bom" => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(Sample),
			"utf-8-bom" => Concat(Encoding.UTF8.GetPreamble(), new UTF8Encoding(false).GetBytes(Sample)),
			"utf-16-le" => Concat(Encoding.Unicode.GetPreamble(), Encoding.Unicode.GetBytes(Sample)),
			"utf-16-be" => Concat(Encoding.BigEndianUnicode.GetPreamble(), Encoding.BigEndianUnicode.GetBytes(Sample)),
			"windows-1250" => Encoding.GetEncoding(1250).GetBytes(Sample),
			_ => throw new ArgumentOutOfRangeException(nameof(label), label, null)
		};
	}

	private static byte[] Concat(byte[] first, byte[] second)
	{
		var combined = new byte[first.Length + second.Length];
		first.CopyTo(combined, 0);
		second.CopyTo(combined, first.Length);
		return combined;
	}

	[Theory]
	[MemberData(nameof(DiacriticPreservingEncodings))]
	public void Encoding_PreservesSlovakDiacritics(string label)
	{
		var text = Extract(Encode(label));

		Assert.Contains("Kostolný", text, StringComparison.Ordinal);
		Assert.Contains("Košice", text, StringComparison.Ordinal);
		Assert.Contains("súčasnosť", text, StringComparison.Ordinal);
	}

	[Fact]
	public void Iso8859_2_ExtractsCleanNonEmptyDraftWithoutCrashing()
	{
		// ISO-8859-2 high bytes are decoded via the Windows-1250 fallback path; exact Slovak
		// glyph fidelity is not guaranteed (the two code pages differ), but the contract is:
		// a clean, non-empty, reviewable draft — never a crash or empty result.
		var bytes = Encoding.GetEncoding(28592).GetBytes(Sample);
		var text = Extract(bytes);

		Assert.False(string.IsNullOrWhiteSpace(text));
		Assert.Contains("Ladislav", text, StringComparison.Ordinal);
		Assert.Contains("Software Engineer", text, StringComparison.Ordinal);
	}

	[Fact]
	public void Utf8WithBom_StripsToReadableContent()
	{
		var text = Extract(Encode("utf-8-bom"));
		Assert.Contains("Kostolný", text, StringComparison.Ordinal);
	}

	[Fact]
	public void MojibakeBytes_DoNotCrashExtractor()
	{
		// Random high bytes that are not valid UTF-8: must degrade gracefully, not throw.
		var bytes = Enumerable.Range(0, 200).Select(i => (byte)(0x80 + (i % 0x40))).ToArray();
		var result = new PlainTextExtractor().Extract(WriteFile(bytes));

		Assert.True(result.Success || result.ErrorMessageKey is not null);
	}
}
