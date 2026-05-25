using System.Collections.Generic;
using Avalonia.Platform.Storage;
using ReVitae.Core.Localization;

namespace ReVitae.Import;

internal static class CvImportFilePickerOptions
{
	private static readonly string[] AllPatterns =
	[
		"*.pdf",
		"*.docx",
		"*.doc",
		"*.odt",
		"*.rtf",
		"*.txt",
		"*.md",
		"*.markdown",
		"*.html",
		"*.htm",
		"*.tex",
		"*.abw",
		"*.wps",
		"*.pages",
		"*.json",
		"*.revitae.json",
		"*.xml",
		"*.yaml",
		"*.yml",
		"*.csv",
		"*.tsv",
		"*.jpg",
		"*.jpeg",
		"*.png",
		"*.webp",
		"*.tif",
		"*.tiff",
		"*.bmp"
	];

	private static readonly string[] DocumentPatterns =
	[
		"*.pdf",
		"*.docx",
		"*.doc",
		"*.odt",
		"*.rtf",
		"*.txt",
		"*.md",
		"*.markdown",
		"*.html",
		"*.htm",
		"*.tex",
		"*.abw",
		"*.wps",
		"*.pages",
		"*.jpg",
		"*.jpeg",
		"*.png",
		"*.webp",
		"*.tif",
		"*.tiff",
		"*.bmp"
	];

	private static readonly string[] StructuredPatterns =
	[
		"*.json",
		"*.revitae.json",
		"*.xml",
		"*.yaml",
		"*.yml",
		"*.csv",
		"*.tsv"
	];

	private static readonly string[] RasterImagePatterns =
	[
		"*.jpg",
		"*.jpeg",
		"*.png",
		"*.webp",
		"*.tif",
		"*.tiff",
		"*.bmp"
	];

	private static readonly string[] DocumentMimeTypes =
	[
		"application/pdf",
		"application/vnd.openxmlformats-officedocument.wordprocessingml.document",
		"application/msword",
		"application/vnd.oasis.opendocument.text",
		"application/rtf",
		"text/plain",
		"text/markdown",
		"text/html",
		"application/x-tex",
		"image/jpeg",
		"image/png",
		"image/webp",
		"image/tiff",
		"image/bmp"
	];

	private static readonly string[] DocumentAppleUtis =
	[
		"com.adobe.pdf",
		"org.openxmlformats.wordprocessingml.document",
		"com.microsoft.word.doc",
		"org.oasis-open.opendocument.text",
		"public.rtf",
		"public.plain-text",
		"net.daringfireball.markdown",
		"public.html",
		"public.tex",
		"public.jpeg",
		"public.png",
		"org.webmproject.webp",
		"public.tiff",
		"com.microsoft.bmp"
	];

	public static IReadOnlyList<FilePickerFileType> CreateFileTypeFilter(AppLocalizer localizer)
	{
		var supportedLabel = localizer.Get(TranslationKeys.ImportPdfFileType);
		var imageLabel = localizer.Get(TranslationKeys.ImportRasterImageFileType);
		var pdfLabel = localizer.Get(TranslationKeys.ImportPdfOnlyFileType);
		var textLabel = localizer.Get(TranslationKeys.ImportPlainTextFileType);

		return
		[
			new FilePickerFileType(supportedLabel)
			{
				Patterns = AllPatterns,
				MimeTypes = DocumentMimeTypes,
				AppleUniformTypeIdentifiers = DocumentAppleUtis
			},
			new FilePickerFileType(pdfLabel)
			{
				Patterns = ["*.pdf"],
				MimeTypes = ["application/pdf"],
				AppleUniformTypeIdentifiers = ["com.adobe.pdf"]
			},
			new FilePickerFileType(textLabel)
			{
				Patterns = ["*.txt"],
				MimeTypes = ["text/plain"],
				AppleUniformTypeIdentifiers = ["public.plain-text"]
			},
			new FilePickerFileType($"{supportedLabel} — documents")
			{
				Patterns = DocumentPatterns,
				MimeTypes = DocumentMimeTypes,
				AppleUniformTypeIdentifiers = DocumentAppleUtis
			},
			new FilePickerFileType(imageLabel)
			{
				Patterns = RasterImagePatterns,
				MimeTypes = ["image/jpeg", "image/png", "image/webp", "image/tiff", "image/bmp"],
				AppleUniformTypeIdentifiers = ["public.image"]
			},
			new FilePickerFileType($"{supportedLabel} — structured")
			{
				Patterns = StructuredPatterns,
				MimeTypes = ["application/json", "application/xml", "text/yaml", "text/csv", "text/tab-separated-values"],
				AppleUniformTypeIdentifiers = ["public.json", "public.xml", "public.yaml", "public.comma-separated-values-text"]
			},
			FilePickerFileTypes.All
		];
	}
}
