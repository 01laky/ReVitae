using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ReVitae.Core.Cv.ProfilePhoto;

namespace ReVitae.Core.Export.Pdf;

public static class CvPdfPhotoHelpers
{
	public static void ComposeSidebarPhotoOrInitials(
		IContainer container,
		CvExportDocument document,
		float sizePt,
		string initialsBackgroundHex,
		string initialsForegroundHex,
		bool circular = true)
	{
		if (TryComposePhoto(container, document.PhotoPath, sizePt, circular))
		{
			return;
		}

		ComposeInitials(
			container,
			ProfilePhotoInitials.Derive(document.FirstName, document.LastName),
			sizePt,
			initialsBackgroundHex,
			initialsForegroundHex,
			circular);
	}

	public static void ComposeHeaderPhoto(IContainer container, CvExportDocument document, float sizePt)
	{
		if (!TryComposePhoto(container, document.PhotoPath, sizePt, circular: true))
		{
			container.Height(sizePt).Width(sizePt);
		}
	}

	public static bool TryComposePhoto(IContainer container, string? photoPath, float sizePt, bool circular)
	{
		var bytes = ProfilePhotoBytes.TryRead(photoPath);
		if (bytes is null || bytes.Length == 0)
		{
			return false;
		}

		var imageContainer = container.Height(sizePt).Width(sizePt);
		if (circular)
		{
			imageContainer = imageContainer.CornerRadius(sizePt / 2f);
		}

		imageContainer.Image(bytes).FitArea();
		return true;
	}

	public static void ComposeInitials(
		IContainer container,
		string initials,
		float sizePt,
		string backgroundHex,
		string foregroundHex,
		bool circular)
	{
		if (string.IsNullOrWhiteSpace(initials))
		{
			container.Height(sizePt).Width(sizePt);
			return;
		}

		var shape = container
			.Height(sizePt)
			.Width(sizePt)
			.Background(backgroundHex)
			.AlignCenter()
			.AlignMiddle();

		if (circular)
		{
			shape = shape.CornerRadius(sizePt / 2f);
		}

		shape.Text(initials)
			.FontSize(sizePt * 0.35f)
			.Bold()
			.FontColor(foregroundHex);
	}
}
