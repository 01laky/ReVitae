using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Localization;
using ReVitae.Preview;
using ReVitae.ProfilePhoto;
using ReVitae.Ui;

namespace ReVitae;

public partial class MainWindow
{
    private readonly ProfilePhotoStorage _profilePhotoStorage = new();
    private string? _profilePhotoPath;

    private async void OnProfilePhotoUploadClicked(object? sender, RoutedEventArgs e)
    {
        await PickAndSaveProfilePhotoAsync();
    }

    private void OnProfilePhotoRemoveClicked(object? sender, RoutedEventArgs e)
    {
        ClearProfilePhoto(showMissingWarning: false);
        MarkProjectDirty();
        UpdatePreview();
        UpdateValidationState();
    }

    private async Task PickAndSaveProfilePhotoAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            ProfilePhotoFilePickerOptions.Create(_localizer));
        if (files.Count == 0)
        {
            return;
        }

        var localPath = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(localPath))
        {
            return;
        }

        var saveResult = _profilePhotoStorage.TrySaveCopy(localPath, _profilePhotoPath);
        if (!saveResult.Success)
        {
            ProfilePhotoErrorTextBlock.Text = saveResult.Error switch
            {
                ProfilePhotoSaveError.UnsupportedFormat => _localizer.Get(TranslationKeys.ProfilePhotoUnsupportedFormat),
                ProfilePhotoSaveError.FileTooLarge => _localizer.Get(TranslationKeys.ProfilePhotoFileTooLarge),
                ProfilePhotoSaveError.EmptyFile or ProfilePhotoSaveError.UnreadableImage =>
                    _localizer.Get(TranslationKeys.ProfilePhotoUnreadable),
                _ => _localizer.Get(TranslationKeys.ProfilePhotoUnreadable)
            };
            return;
        }

        _profilePhotoPath = saveResult.StoredPath;
        ProfilePhotoErrorTextBlock.Text = string.Empty;
        MarkProjectDirty();
        RefreshProfilePhotoUi();
        UpdatePreview();
        UpdateValidationState();
    }

    private void ClearProfilePhoto(bool showMissingWarning)
    {
        _profilePhotoStorage.TryDelete(_profilePhotoPath);
        _profilePhotoPath = null;
        ProfilePhotoErrorTextBlock.Text = showMissingWarning
            ? _localizer.Get(TranslationKeys.ProfilePhotoMissingOnDisk)
            : string.Empty;
        RefreshProfilePhotoUi();
    }

    private void ClearProfilePhotoBeforeImport()
    {
        _profilePhotoStorage.TryDelete(_profilePhotoPath);
        _profilePhotoPath = null;
        ProfilePhotoErrorTextBlock.Text = string.Empty;
        RefreshProfilePhotoUi();
    }

    private void ApplyImportedProfilePhoto(string? importedPath)
    {
        if (ProfilePhotoStorage.FileExists(importedPath))
        {
            _profilePhotoPath = importedPath;
        }
        else
        {
            _profilePhotoPath = null;
        }

        RefreshProfilePhotoUi();
    }

    private void RefreshProfilePhotoUi()
    {
        const double size = 112;
        var hasPhoto = ProfilePhotoStorage.FileExists(_profilePhotoPath);
        ProfilePhotoRemoveButton.IsVisible = hasPhoto;
        ProfilePhotoHintTextBlock.Text = hasPhoto
            ? _localizer.Get(TranslationKeys.ProfilePhotoChangeHint)
            : _localizer.Get(TranslationKeys.ProfilePhotoUploadHint);
        ToolTip.SetTip(ProfilePhotoUploadButton, ProfilePhotoHintTextBlock.Text);
        AutomationProperties.SetName(
            ProfilePhotoUploadButton,
            hasPhoto
                ? _localizer.Get(TranslationKeys.ProfilePhotoChangeHint)
                : _localizer.Get(TranslationKeys.ProfilePhotoUploadButton));

        Control preview = hasPhoto
            ? ProfilePhotoPreviewFactory.CreateFormPhotoImage(_profilePhotoPath!, size)
            : ProfilePhotoPreviewFactory.CreateFormPhotoPlaceholder(
                size,
                Brush.Parse("#E8E8E8"),
                Brush.Parse("#666666"));

        ProfilePhotoPreviewHost.Child = preview;
    }
}
