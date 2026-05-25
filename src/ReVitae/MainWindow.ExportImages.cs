using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ReVitae.Core.Export;
using ReVitae.Core.Export.Images;
using ReVitae.Core.Localization;
using ReVitae.Export;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReVitae;

public partial class MainWindow
{
	private bool _isExportImageOptionsVisible;
	private bool _isImageExportInProgress;
	private int _exportImageTotalPages;

	private void WireExportImageOptionsEvents()
	{
		ExportImageFormatPngRadio.IsCheckedChanged += (_, _) => RefreshExportImageOptionsUi();
		ExportImageFormatJpegRadio.IsCheckedChanged += (_, _) => RefreshExportImageOptionsUi();
		ExportImageFormatWebpRadio.IsCheckedChanged += (_, _) => RefreshExportImageOptionsUi();
		ExportImageDeliveryZipRadio.IsCheckedChanged += (_, _) => RefreshExportImageOptionsUi();
		ExportImageDeliverySeparateRadio.IsCheckedChanged += (_, _) => RefreshExportImageOptionsUi();
		ExportImageScale1xRadio.IsCheckedChanged += (_, _) => RefreshExportImageOptionsUi();
		ExportImageScale2xRadio.IsCheckedChanged += (_, _) => RefreshExportImageOptionsUi();
		ExportImagePagesAllRadio.IsCheckedChanged += (_, _) => RefreshExportImageOptionsUi();
		ExportImagePagesRangeRadio.IsCheckedChanged += (_, _) => RefreshExportImageOptionsUi();
		ExportImageQualitySlider.PropertyChanged += (_, e) =>
		{
			if (e.Property.Name == nameof(RangeBase.Value))
			{
				ExportImageQualityValueTextBlock.Text =
					((int)ExportImageQualitySlider.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
				RefreshExportImageSizeEstimate();
			}
		};
		ExportImagePageFromNumeric.PropertyChanged += (_, e) =>
		{
			if (e.Property.Name is nameof(NumericUpDown.Value))
			{
				RefreshExportImageOptionsUi();
			}
		};
		ExportImagePageToNumeric.PropertyChanged += (_, e) =>
		{
			if (e.Property.Name is nameof(NumericUpDown.Value))
			{
				RefreshExportImageOptionsUi();
			}
		};
	}

	private void ApplyExportImageLocalization()
	{
		ExportImageOptionsTitleTextBlock.Text = _localizer.Get(TranslationKeys.ExportImageOptionsTitle);
		ExportImageFormatLabelTextBlock.Text = _localizer.Get(TranslationKeys.ExportImageFormatLabel);
		ExportImageFormatPngRadio.Content = _localizer.Get(TranslationKeys.ExportImageFormatPng);
		ExportImageFormatJpegRadio.Content = _localizer.Get(TranslationKeys.ExportImageFormatJpeg);
		ExportImageFormatWebpRadio.Content = _localizer.Get(TranslationKeys.ExportImageFormatWebp);
		ExportImageDeliveryLabelTextBlock.Text = _localizer.Get(TranslationKeys.ExportImageDeliveryLabel);
		ExportImageDeliveryZipRadio.Content = _localizer.Get(TranslationKeys.ExportImageDeliveryZip);
		ExportImageDeliverySeparateRadio.Content = _localizer.Get(TranslationKeys.ExportImageDeliverySeparate);
		ExportImageQualityLabelTextBlock.Text = _localizer.Get(TranslationKeys.ExportImageQualityLabel);
		ExportImageScaleLabelTextBlock.Text = _localizer.Get(TranslationKeys.ExportImageScaleLabel);
		ExportImageScale1xRadio.Content = _localizer.Get(TranslationKeys.ExportImageScale1x);
		ExportImageScale2xRadio.Content = _localizer.Get(TranslationKeys.ExportImageScale2x);
		ExportImagePagesLabelTextBlock.Text = _localizer.Get(TranslationKeys.ExportImagePagesLabel);
		ExportImagePagesAllRadio.Content = _localizer.Get(TranslationKeys.ExportImagePagesAll);
		ExportImagePagesRangeRadio.Content = _localizer.Get(TranslationKeys.ExportImagePagesRange);
		ExportImagePageFromLabelTextBlock.Text = _localizer.Get(TranslationKeys.ExportImagePageFromLabel);
		ExportImagePageToLabelTextBlock.Text = _localizer.Get(TranslationKeys.ExportImagePageToLabel);
		ExportImageBackButton.Content = _localizer.Get(TranslationKeys.ExportImageBackButton);
		ExportImageExportButton.Content = _localizer.Get(TranslationKeys.ExportImageExportButton);
		ExportImageRangeErrorTextBlock.Text = _localizer.Get(TranslationKeys.ExportImageRangeInvalid);
		RefreshExportImageOptionsUi();
	}

	private void ShowExportImageOptionsPanel()
	{
		_isExportImageOptionsVisible = true;
		ExportFormatCategoriesPanel.IsVisible = false;
		ExportImageOptionsPanel.IsVisible = true;
		ExportModalSubtitleTextBlock.Text = _localizer.Get(TranslationKeys.ExportFormatImagesHint);

		try
		{
			_exportImageTotalPages = CvImageExporter.GetPageCount(BuildExportDocument());
		}
		catch
		{
			_exportImageTotalPages = 1;
		}

		ExportImagePageFromNumeric.Maximum = Math.Max(1, _exportImageTotalPages);
		ExportImagePageToNumeric.Maximum = Math.Max(1, _exportImageTotalPages);
		ExportImagePageFromNumeric.Value = 1;
		ExportImagePageToNumeric.Value = Math.Max(1, _exportImageTotalPages);

		RefreshExportImageOptionsUi();
	}

	private void HideExportImageOptionsPanel()
	{
		_isExportImageOptionsVisible = false;
		ExportImageOptionsPanel.IsVisible = false;
		ExportFormatCategoriesPanel.IsVisible = true;
		ExportModalSubtitleTextBlock.Text = _localizer.Get(TranslationKeys.ExportModalSubtitle);
		ExportImageProgressTextBlock.IsVisible = false;
		SetExportImageControlsEnabled(true);
	}

	private void RefreshExportImageOptionsUi()
	{
		var showQuality = ExportImageFormatJpegRadio.IsChecked == true ||
						  ExportImageFormatWebpRadio.IsChecked == true;
		ExportImageQualityLabelTextBlock.IsVisible = showQuality;
		ExportImageQualitySlider.IsVisible = showQuality;
		ExportImageQualityValueTextBlock.IsVisible = showQuality;

		var showRange = ExportImagePagesRangeRadio.IsChecked == true;
		ExportImagePageRangeGrid.IsVisible = showRange;

		var rangeValid = ValidateExportImagePageRange(out _);
		ExportImageRangeErrorTextBlock.IsVisible = showRange && !rangeValid;
		ExportImageExportButton.IsEnabled = rangeValid && !_isImageExportInProgress;

		RefreshExportImageSizeEstimate();
	}

	private void RefreshExportImageSizeEstimate()
	{
		if (_exportImageTotalPages <= 0)
		{
			ExportImageSizeEstimateTextBlock.Text = _localizer.Get(TranslationKeys.ExportImageSizeEstimateUnknown);
			return;
		}

		var options = BuildExportImageOptionsFromUi();
		var rangeResult = CvImagePageRangeResolver.Resolve(_exportImageTotalPages, options.PageRange);
		var pageCount = rangeResult.IsValid ? rangeResult.PageIndices.Count : _exportImageTotalPages;
		var bytes = CvImageExportSizeEstimator.EstimateBytes(
			pageCount,
			options.Format,
			options.Scale,
			options.Quality);
		var sizeLabel = CvImageExportSizeEstimator.FormatMegabytes(bytes);
		var formatLabel = CvImageExportSizeEstimator.FormatLabel(options.Format, options.Scale);
		ExportImageSizeEstimateTextBlock.Text = _localizer.Format(
			TranslationKeys.ExportImageSizeEstimate,
			pageCount,
			sizeLabel,
			formatLabel);
	}

	private bool ValidateExportImagePageRange(out CvImagePageRange range)
	{
		if (ExportImagePagesAllRadio.IsChecked == true)
		{
			range = CvImagePageRange.AllPages;
			return CvImagePageRangeResolver.Resolve(_exportImageTotalPages, range).IsValid;
		}

		var from = (int)(ExportImagePageFromNumeric.Value ?? 1);
		var to = (int)(ExportImagePageToNumeric.Value ?? 1);
		range = new CvImagePageRange(from, to);
		return CvImagePageRangeResolver.Resolve(_exportImageTotalPages, range).IsValid;
	}

	private CvImageExportOptions BuildExportImageOptionsFromUi()
	{
		var format = ExportImageFormatJpegRadio.IsChecked == true
			? CvImageExportFormat.Jpeg
			: ExportImageFormatWebpRadio.IsChecked == true
				? CvImageExportFormat.WebP
				: CvImageExportFormat.Png;

		var delivery = ExportImageDeliverySeparateRadio.IsChecked == true
			? CvImageExportDelivery.SeparateFiles
			: CvImageExportDelivery.ZipArchive;

		var scale = ExportImageScale1xRadio.IsChecked == true
			? CvImageExportScale.Standard
			: CvImageExportScale.High;

		var quality = (int)ExportImageQualitySlider.Value;
		ValidateExportImagePageRange(out var range);

		return new CvImageExportOptions(format, delivery, scale, quality, range);
	}

	private void OnExportImageBackClicked(object? sender, RoutedEventArgs e)
	{
		if (!_isImageExportInProgress)
		{
			HideExportImageOptionsPanel();
		}
	}

	private async void OnExportImageExportClicked(object? sender, RoutedEventArgs e)
	{
		if (_isImageExportInProgress || !ValidateExportImagePageRange(out _))
		{
			return;
		}

		var topLevel = TopLevel.GetTopLevel(this);
		if (topLevel is null)
		{
			ExportStatusTextBlock.Text = _localizer.Get(TranslationKeys.ExportFilePickerUnavailable);
			return;
		}

		var options = BuildExportImageOptionsFromUi();
		SetExportModalVisible(false);

		CvImageExportDestination? destination = null;

		if (options.Delivery == CvImageExportDelivery.ZipArchive)
		{
			var suggestedFilename = CvImageExportFilenameHelper.SuggestImageZipFilename(
				FirstNameTextBox.Text,
				LastNameTextBox.Text);
			var file = await topLevel.StorageProvider.SaveFilePickerAsync(
				CvExportFilePickerOptions.CreateZipSaveOptions(_localizer, suggestedFilename));
			if (file is null)
			{
				return;
			}

			var localPath = file.TryGetLocalPath();
			if (string.IsNullOrWhiteSpace(localPath))
			{
				ExportStatusTextBlock.Text = _localizer.Get(TranslationKeys.ExportFailed);
				return;
			}

			destination = new CvImageExportDestination.ZipFile(localPath);
		}
		else
		{
			var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
				CvExportFilePickerOptions.CreateFolderPickerOptions(_localizer));
			var folder = folders.FirstOrDefault();
			if (folder is null)
			{
				return;
			}

			var localPath = folder.TryGetLocalPath();
			if (string.IsNullOrWhiteSpace(localPath))
			{
				ExportStatusTextBlock.Text = _localizer.Get(TranslationKeys.ExportFailed);
				return;
			}

			destination = new CvImageExportDestination.Folder(localPath);
		}

		await RunImageExportAsync(BuildExportDocument(), options, destination);
	}

	private async Task RunImageExportAsync(
		CvExportDocument document,
		CvImageExportOptions options,
		CvImageExportDestination destination)
	{
		_isImageExportInProgress = true;
		HideExportPostActions();

		var progress = new UiImageExportProgress(message =>
		{
			Dispatcher.UIThread.Post(() => ExportStatusTextBlock.Text = message);
		}, _localizer);

		CvImageExportResult result;
		try
		{
			result = await Task.Run(() =>
				CvImageExporter.Export(document, options, destination, progress));
		}
		catch
		{
			result = CvImageExportResult.Failed(TranslationKeys.ExportFailed);
		}

		_isImageExportInProgress = false;

		if (!result.Success)
		{
			ExportStatusTextBlock.Text = result.ErrorMessageKey switch
			{
				TranslationKeys.ExportImageTooManyPages =>
					_localizer.Format(TranslationKeys.ExportImageTooManyPages, result.ErrorMessageArg ?? CvImageExportLimits.MaxPageCount),
				_ => _localizer.Get(result.ErrorMessageKey ?? TranslationKeys.ExportFailed)
			};
			return;
		}

		_lastExportedFilePath = result.OutputPath;
		ExportStatusTextBlock.Text = destination switch
		{
			CvImageExportDestination.ZipFile zip =>
				_localizer.Format(
					TranslationKeys.ExportedImagesToZip,
					result.ExportedPageCount,
					Path.GetFileName(zip.Path)),
			_ => _localizer.Format(TranslationKeys.ExportedImagesToFolder, result.ExportedPageCount)
		};
		ShowExportPostActions();
	}

	private void SetExportImageControlsEnabled(bool enabled)
	{
		ExportImageFormatPngRadio.IsEnabled = enabled;
		ExportImageFormatJpegRadio.IsEnabled = enabled;
		ExportImageFormatWebpRadio.IsEnabled = enabled;
		ExportImageDeliveryZipRadio.IsEnabled = enabled;
		ExportImageDeliverySeparateRadio.IsEnabled = enabled;
		ExportImageQualitySlider.IsEnabled = enabled;
		ExportImageScale1xRadio.IsEnabled = enabled;
		ExportImageScale2xRadio.IsEnabled = enabled;
		ExportImagePagesAllRadio.IsEnabled = enabled;
		ExportImagePagesRangeRadio.IsEnabled = enabled;
		ExportImagePageFromNumeric.IsEnabled = enabled;
		ExportImagePageToNumeric.IsEnabled = enabled;
		ExportImageBackButton.IsEnabled = enabled;
		ExportImageExportButton.IsEnabled = enabled && ValidateExportImagePageRange(out _);
	}

	private sealed class UiImageExportProgress(Action<string> reportStatus, AppLocalizer localizer)
		: IImageExportProgress
	{
		public void Report(ImageExportProgressPhase phase, int currentPage, int totalPages)
		{
			var message = phase switch
			{
				ImageExportProgressPhase.Rendering =>
					localizer.Format(TranslationKeys.ExportImageProgressRendering, currentPage, totalPages),
				ImageExportProgressPhase.Writing =>
					localizer.Get(TranslationKeys.ExportImageProgressWriting),
				_ => string.Empty
			};

			if (!string.IsNullOrWhiteSpace(message))
			{
				reportStatus(message);
			}
		}
	}
}
