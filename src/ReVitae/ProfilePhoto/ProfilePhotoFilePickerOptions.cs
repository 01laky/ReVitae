using Avalonia.Platform.Storage;
using ReVitae.Core.Localization;

namespace ReVitae.ProfilePhoto;

internal static class ProfilePhotoFilePickerOptions
{
    public static FilePickerOpenOptions Create(AppLocalizer localizer) =>
        new()
        {
            Title = localizer.Get(TranslationKeys.ProfilePhotoFilePickerTitle),
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(localizer.Get(TranslationKeys.ProfilePhotoFileType))
                {
                    Patterns = ["*.jpg", "*.jpeg", "*.png", "*.webp"],
                    MimeTypes = ["image/jpeg", "image/png", "image/webp"]
                }
            ]
        };
}
