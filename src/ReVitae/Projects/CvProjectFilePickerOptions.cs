using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform.Storage;
using ReVitae.Core.Export;
using ReVitae.Core.Localization;

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

	public static FilePickerOpenOptions CreateOpenOptions(AppLocalizer localizer) =>
		new()
		{
			Title = localizer.Get(TranslationKeys.ProjectOpenDialogTitle),
			AllowMultiple = false,
			FileTypeFilter =
			[
				CreateProjectFileType(localizer),
				CreateGenericJsonFileType(localizer)
			]
		};

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
