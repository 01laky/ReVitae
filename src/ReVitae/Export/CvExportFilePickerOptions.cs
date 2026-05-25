using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform.Storage;
using ReVitae.Core.Export;
using ReVitae.Core.Localization;

namespace ReVitae.Export;

internal static class CvExportFilePickerOptions
{
	public static FilePickerSaveOptions Create(
		CvExportFormat format,
		AppLocalizer localizer,
		string suggestedFilename)
	{
		var descriptor = CvExportFormatCatalog.Get(format);
		var formatLabel = localizer.Get(descriptor.LabelKey);

		return new FilePickerSaveOptions
		{
			Title = localizer.Format(TranslationKeys.ExportSaveDialogTitleFormat, formatLabel),
			SuggestedFileName = suggestedFilename,
			DefaultExtension = CvExportSaveDialogDefaults.GetDefaultExtension(format),
			FileTypeChoices = [CreateFileType(format, localizer)]
		};
	}

	public static FilePickerSaveOptions CreateZipSaveOptions(AppLocalizer localizer, string suggestedFilename) =>
		new()
		{
			Title = localizer.Get(TranslationKeys.ExportImageOptionsTitle),
			SuggestedFileName = suggestedFilename,
			DefaultExtension = "zip",
			FileTypeChoices =
			[
				new FilePickerFileType(localizer.Get(TranslationKeys.ExportZipFileType))
				{
					Patterns = ["*.zip"],
					MimeTypes = ["application/zip"]
				}
			]
		};

	public static FolderPickerOpenOptions CreateFolderPickerOptions(AppLocalizer localizer) =>
		new()
		{
			Title = localizer.Get(TranslationKeys.ExportFolderPickerTitle),
			AllowMultiple = false
		};

	public static FilePickerFileType CreateFileType(CvExportFormat format, AppLocalizer localizer) =>
		new(localizer.Get(CvExportSaveDialogDefaults.GetFileTypeLabelKey(format)))
		{
			Patterns = CvExportSaveDialogDefaults.GetPatterns(format).ToList(),
			MimeTypes = CvExportSaveDialogDefaults.GetMimeTypes(format).ToList()
		};
}
