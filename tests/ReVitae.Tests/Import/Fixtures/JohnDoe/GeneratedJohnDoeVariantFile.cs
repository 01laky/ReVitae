namespace ReVitae.Tests.Import.Fixtures.JohnDoe;

/// <summary>
/// Temp file created for one matrix variant. Always dispose (or use <c>using</c>) so generated CV bytes are deleted.
/// </summary>
public sealed class GeneratedJohnDoeVariantFile : IDisposable
{
	private readonly string _tempDirectory;
	private bool _disposed;

	private GeneratedJohnDoeVariantFile(string path, string tempDirectory, long byteLength)
	{
		Path = path;
		_tempDirectory = tempDirectory;
		ByteLength = byteLength;
	}

	public string Path { get; }

	public string TempDirectory => _tempDirectory;

	public long ByteLength { get; }

	public static GeneratedJohnDoeVariantFile Write(JohnDoeVariantSpec spec, byte[] contents) =>
		Write(spec, contents.AsMemory());

	public static GeneratedJohnDoeVariantFile Write(JohnDoeVariantSpec spec, ReadOnlyMemory<byte> contents)
	{
		if (contents.IsEmpty)
		{
			throw new InvalidOperationException($"Refusing to write empty bytes for John Doe variant {spec.Id}.");
		}

		var tempDirectory = JohnDoeMatrixTempDirectory.CreateVariantDirectory();
		var path = System.IO.Path.Combine(tempDirectory, spec.FileName);
		File.WriteAllBytes(path, contents.Span);
		VerifyWrittenFile(path, contents.Length);
		return new GeneratedJohnDoeVariantFile(path, tempDirectory, contents.Length);
	}

	public static GeneratedJohnDoeVariantFile WriteText(JohnDoeVariantSpec spec, string contents)
	{
		if (string.IsNullOrEmpty(contents))
		{
			throw new InvalidOperationException($"Refusing to write empty text for John Doe variant {spec.Id}.");
		}

		var tempDirectory = JohnDoeMatrixTempDirectory.CreateVariantDirectory();
		var path = System.IO.Path.Combine(tempDirectory, spec.FileName);
		File.WriteAllText(path, contents);
		var byteLength = new FileInfo(path).Length;
		VerifyWrittenFile(path, byteLength);
		return new GeneratedJohnDoeVariantFile(path, tempDirectory, byteLength);
	}

	internal static void VerifyWrittenFile(string path, long expectedLength)
	{
		if (!File.Exists(path))
		{
			throw new InvalidOperationException($"Expected generated file at '{path}' but it does not exist.");
		}

		var actualLength = new FileInfo(path).Length;
		if (actualLength <= 0)
		{
			throw new InvalidOperationException($"Generated file '{path}' is empty.");
		}

		if (expectedLength > 0 && actualLength != expectedLength)
		{
			throw new InvalidOperationException(
				$"Generated file '{path}' length {actualLength} does not match expected {expectedLength} bytes.");
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		JohnDoeMatrixTempDirectory.DeleteDirectory(_tempDirectory);
	}
}
