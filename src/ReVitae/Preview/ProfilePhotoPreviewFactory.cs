using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Export;

namespace ReVitae.Preview;

internal static class ProfilePhotoPreviewFactory
{
	public static Control CreateSidebarPhotoOrInitials(
		CvExportDocument document,
		double size,
		IBrush initialsBackground,
		IBrush initialsForeground)
	{
		if (ProfilePhotoStorage.FileExists(document.PhotoPath))
		{
			return WrapCircle(CreatePhotoImage(document.PhotoPath!, size), size);
		}

		return CreateInitialsCircle(
			ProfilePhotoInitials.Derive(document.FirstName, document.LastName),
			size,
			initialsBackground,
			initialsForeground);
	}

	public static Control? CreateHeaderPhotoIfPresent(CvExportDocument document, double size)
	{
		if (!ProfilePhotoStorage.FileExists(document.PhotoPath))
		{
			return null;
		}

		return WrapCircle(CreatePhotoImage(document.PhotoPath!, size), size);
	}

	public static Control CreateFormPhotoPlaceholder(double size, IBrush background, IBrush foreground)
	{
		return new Border
		{
			Width = size,
			Height = size,
			CornerRadius = new CornerRadius(size / 2),
			Background = background,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Child = new TextBlock
			{
				Text = "+",
				FontSize = size * 0.35,
				FontWeight = FontWeight.Bold,
				Foreground = foreground,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
	}

	public static Control CreateFormPhotoImage(string photoPath, double size)
	{
		return WrapCircle(CreatePhotoImage(photoPath, size), size);
	}

	private static Image CreatePhotoImage(string photoPath, double size)
	{
		return new Image
		{
			Source = new Bitmap(photoPath),
			Width = size,
			Height = size,
			Stretch = Stretch.UniformToFill
		};
	}

	private static Control WrapCircle(Control content, double size)
	{
		return new Border
		{
			Width = size,
			Height = size,
			CornerRadius = new CornerRadius(size / 2),
			ClipToBounds = true,
			Child = content
		};
	}

	private static Control CreateInitialsCircle(
		string initials,
		double size,
		IBrush background,
		IBrush foreground)
	{
		if (string.IsNullOrWhiteSpace(initials))
		{
			return new Border { Width = size, Height = size };
		}

		return new Border
		{
			Width = size,
			Height = size,
			CornerRadius = new CornerRadius(size / 2),
			Background = background,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Child = new TextBlock
			{
				Text = initials,
				FontSize = size * 0.35,
				FontWeight = FontWeight.Bold,
				Foreground = foreground,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
	}
}
