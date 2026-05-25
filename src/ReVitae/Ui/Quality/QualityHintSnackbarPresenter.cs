using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using ReVitae.Ui;

namespace ReVitae.Ui.Quality;

public sealed class QualityHintSnackbarPresenter
{
	private readonly Border _border;
	private readonly TextBlock _messageTextBlock;
	private DispatcherTimer? _hideTimer;

	public QualityHintSnackbarPresenter(Border border, TextBlock messageTextBlock)
	{
		_border = border;
		_messageTextBlock = messageTextBlock;
		_border.IsVisible = false;
		_border.Classes.Add(UiClasses.AppCard);
	}

	public void Show(string message, TimeSpan? duration = null)
	{
		_hideTimer?.Stop();
		_messageTextBlock.Text = message;
		_border.IsVisible = true;

		_hideTimer = new DispatcherTimer { Interval = duration ?? TimeSpan.FromSeconds(5) };
		_hideTimer.Tick += (_, _) =>
		{
			_hideTimer.Stop();
			_border.IsVisible = false;
		};
		_hideTimer.Start();
	}

	public void Hide()
	{
		_hideTimer?.Stop();
		_border.IsVisible = false;
	}
}
