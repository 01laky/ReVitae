using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace ReVitae.Ui.Quality;

/// <summary>
/// Opens quality hints in a large in-window modal (registered from MainWindow).
/// </summary>
public static class QualityHintFlyoutHelper
{
	private static QualityHintModalPresenter? _presenter;

	public static QualityHintAiOptions? AiOptions { get; set; }

	public static void RegisterPresenter(QualityHintModalPresenter presenter) =>
		_presenter = presenter;

	public static void Show(
		Control anchor,
		Core.Localization.AppLocalizer localizer,
		string sectionTitle,
		IReadOnlyList<Core.Quality.CvQualityHint> hints,
		Func<Core.Quality.CvQualityHint, bool>? navigateToHint = null,
		Action<Core.Quality.CvQualityHint>? dismissHint = null,
		Action? flyoutOpened = null,
		QualityHintAiOptions? aiOptions = null)
	{
		if (_presenter is null)
		{
			return;
		}

		_presenter.Show(localizer, sectionTitle, hints, navigateToHint, dismissHint, aiOptions);
		flyoutOpened?.Invoke();
	}

	public static void HideActive() => _presenter?.Hide();
}
