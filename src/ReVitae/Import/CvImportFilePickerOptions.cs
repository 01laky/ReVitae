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

    public static IReadOnlyList<FilePickerFileType> CreateFileTypeFilter(AppLocalizer localizer)
    {
        var supportedLabel = localizer.Get(TranslationKeys.ImportPdfFileType);

        return
        [
            new FilePickerFileType(supportedLabel)
            {
                Patterns = AllPatterns
            },
            new FilePickerFileType($"{supportedLabel} — documents")
            {
                Patterns = DocumentPatterns
            },
            new FilePickerFileType($"{supportedLabel} — structured")
            {
                Patterns = StructuredPatterns
            }
        ];
    }
}
