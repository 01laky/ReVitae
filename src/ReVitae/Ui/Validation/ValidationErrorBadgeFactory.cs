using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Ui;

namespace ReVitae.Ui.Validation;

public static class ValidationErrorBadgeFactory
{
	public static (StackPanel Panel, TextBlock TextBlock) Create()
	{
		var textBlock = new TextBlock
		{
			IsVisible = false,
			FontWeight = FontWeight.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		};
		textBlock.Classes.Add(UiClasses.ErrorText);

		var panel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 4,
			IsVisible = false,
			VerticalAlignment = VerticalAlignment.Center,
			Children =
			{
				MaterialIconFactory.Create(MaterialIconKind.AlertCircle, 16),
				textBlock
			}
		};

		return (panel, textBlock);
	}

	public static void Update(
		StackPanel panel,
		TextBlock textBlock,
		int errorCount,
		bool isCollapsed,
		string formattedText,
		Action? onClicked = null)
	{
		var show = errorCount > 0 && isCollapsed;
		panel.IsVisible = show;
		textBlock.IsVisible = show;
		textBlock.Text = show ? formattedText : string.Empty;

		if (onClicked is null)
		{
			return;
		}

		panel.PointerPressed -= OnPointerPressed;
		if (show)
		{
			panel.PointerPressed += OnPointerPressed;
		}

		void OnPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
		{
			onClicked();
			e.Handled = true;
		}
	}
}
