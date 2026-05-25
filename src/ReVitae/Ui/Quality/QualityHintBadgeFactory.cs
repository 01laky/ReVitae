using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Ui;

namespace ReVitae.Ui.Quality;

public static class QualityHintBadgeFactory
{
	public static (StackPanel Panel, TextBlock TextBlock) Create()
	{
		var textBlock = new TextBlock
		{
			IsVisible = false,
			FontWeight = FontWeight.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		};
		textBlock.Classes.Add(UiClasses.SecondaryText);

		var panel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 4,
			IsVisible = false,
			VerticalAlignment = VerticalAlignment.Center,
			Focusable = true,
			Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
			Children =
			{
				MaterialIconFactory.Create(MaterialIconKind.LightbulbOutline, 16),
				textBlock
			}
		};

		return (panel, textBlock);
	}
}
