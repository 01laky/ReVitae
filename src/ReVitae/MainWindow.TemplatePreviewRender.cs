using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReVitae.Core.Export;

namespace ReVitae;

public partial class MainWindow
{
	// 047 T1: the preview is the rasterized real export PDF (single rendering pipeline), updated
	// off the UI thread, debounced per keystroke, and cached by document content hash.
	private const int PreviewDebounceMs = 220;

	// Docnet/pdfium is not safe for concurrent use — serialize preview rasterizations.
	private static readonly SemaphoreSlim PreviewRenderLock = new(1, 1);

	private CancellationTokenSource? _previewRenderCts;
	private string? _lastPreviewKey;
	private IReadOnlyList<Bitmap>? _lastPreviewBitmaps;

	private void UpdatePreview() => SchedulePreviewRender();

	private void SchedulePreviewRender()
	{
		_previewRenderCts?.Cancel();
		_previewRenderCts?.Dispose();
		var cts = new CancellationTokenSource();
		_previewRenderCts = cts;
		_ = RenderPreviewDebouncedAsync(cts.Token);
	}

	private async Task RenderPreviewDebouncedAsync(CancellationToken token)
	{
		try
		{
			await Task.Delay(PreviewDebounceMs, token).ConfigureAwait(true);
			if (token.IsCancellationRequested)
			{
				return;
			}

			var document = BuildExportDocument();
			var key = ComputePreviewKey(document);

			IReadOnlyList<Bitmap> bitmaps;
			if (key == _lastPreviewKey && _lastPreviewBitmaps is not null)
			{
				bitmaps = _lastPreviewBitmaps;
			}
			else
			{
				await PreviewRenderLock.WaitAsync(token).ConfigureAwait(true);
				IReadOnlyList<byte[]> pages;
				try
				{
					pages = await Task.Run(() => CvTemplatePreviewImage.RenderPagesPng(document), token)
						.ConfigureAwait(true);
				}
				finally
				{
					PreviewRenderLock.Release();
				}

				if (token.IsCancellationRequested)
				{
					return;
				}

				bitmaps = pages.Select(ToBitmap).ToList();
				_lastPreviewKey = key;
				_lastPreviewBitmaps = bitmaps;
			}

			PreviewContentControl.Content = BuildPreviewPagesView(bitmaps);
			PreviewExpandContentControl.Content = BuildPreviewPagesView(bitmaps);
		}
		catch (OperationCanceledException)
		{
			// Superseded by a newer edit; the latest scheduled render will win.
		}
		catch (Exception)
		{
			// Transient render failure — keep the previous preview rather than blanking it.
		}
	}

	private static Bitmap ToBitmap(byte[] png)
	{
		using var stream = new MemoryStream(png);
		return new Bitmap(stream);
	}

	private static Control BuildPreviewPagesView(IReadOnlyList<Bitmap> bitmaps)
	{
		var stack = new StackPanel
		{
			Spacing = 12,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		foreach (var bitmap in bitmaps)
		{
			var border = new Border
			{
				Background = Brushes.White,
				BorderBrush = Brush.Parse("#E0E0E0"),
				BorderThickness = new Avalonia.Thickness(1),
				Child = new Image
				{
					Source = bitmap,
					Stretch = Stretch.Uniform,
				},
			};
			stack.Children.Add(border);
		}

		return stack;
	}

	private string ComputePreviewKey(CvExportDocument document)
	{
		try
		{
			var json = JsonSerializer.Serialize(document);
			var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
			return Convert.ToHexString(hash);
		}
		catch (Exception)
		{
			// If the document cannot be serialized, fall back to always re-rendering.
			return Guid.NewGuid().ToString("N");
		}
	}
}
