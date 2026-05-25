using System.Collections.Generic;
using Avalonia.Platform.Storage;
using ReVitae.Core.Localization;
using ReVitae.Import;

namespace ReVitae.Projects;

internal static class CvProjectFilePickerOptions
{
	public static FilePickerSaveOptions CreateSaveOptions(AppLocalizer localizer, string suggestedFilename) =>
		new()
		{
			Title = localizer.Get(TranslationKeys.ProjectSaveDialogTitle),
			SuggestedFileName = suggestedFilename,
			DefaultExtension = "revitae.json",
			FileTypeChoices =
			[
				CreateProjectFileType(localizer),
				CreateGenericJsonFileType(localizer)
			]
		};

	public static FilePickerOpenOptions CreateOpenOptions(AppLocalizer localizer)
	{
		var filters = new List<FilePickerFileType>(CvImportFilePickerOptions.CreateFileTypeFilter(localizer));
		var defaultFilter = filters[0];
		filters.Insert(1, CreateProjectFileType(localizer));
		filters.Insert(2, CreateGenericJsonFileType(localizer));

		return new FilePickerOpenOptions
		{
			Title = localizer.Get(TranslationKeys.ProjectOpenDialogTitle),
			AllowMultiple = false,
			FileTypeFilter = filters,
			SuggestedFileType = defaultFilter,
		};
	}

	private static FilePickerFileType CreateProjectFileType(AppLocalizer localizer) =>
		new(localizer.Get(TranslationKeys.ProjectFileType))
		{
			Patterns = ["*.revitae.json"],
			MimeTypes = ["application/json"]
		};

	private static FilePickerFileType CreateGenericJsonFileType(AppLocalizer localizer) =>
		new(localizer.Get(TranslationKeys.ProjectGenericJsonFileType))
		{
			Patterns = ["*.json"],
			MimeTypes = ["application/json"]
		};
}
