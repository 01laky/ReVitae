using System;
using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;

namespace ReVitae.Ui.Quality;

public static class QualityHintsService
{
	public static void UpdateSectionQualityBadge(
		StackPanel badgePanel,
		TextBlock badgeTextBlock,
		IReadOnlyList<CvQualityHint> sectionHints,
		AppLocalizer localizer,
		string sectionTitle,
		Func<CvQualityHint, bool>? navigateToHint = null,
		Action<CvQualityHint>? dismissHint = null,
		Action? flyoutOpened = null)
	{
		var count = sectionHints.Count;
		var show = count > 0;
		badgePanel.IsVisible = show;
		badgeTextBlock.IsVisible = show;
		badgeTextBlock.Text = show ? localizer.Format(TranslationKeys.QualityHintBadgeCount, count) : string.Empty;

		if (show)
		{
			AutomationProperties.SetName(
				badgePanel,
				localizer.Format(TranslationKeys.QualityHintBadgeAccessibility, count));
		}

		badgePanel.PointerPressed -= OnPointerPressed;
		badgePanel.KeyDown -= OnKeyDown;

		if (!show)
		{
			return;
		}

		badgePanel.PointerPressed += OnPointerPressed;
		badgePanel.KeyDown += OnKeyDown;

		void OpenFlyout()
		{
			QualityHintFlyoutHelper.Show(
				badgePanel,
				localizer,
				sectionTitle,
				sectionHints,
				navigateToHint,
				dismissHint,
				flyoutOpened,
				QualityHintFlyoutHelper.AiOptions);
		}

		void OnPointerPressed(object? sender, PointerPressedEventArgs e)
		{
			OpenFlyout();
			e.Handled = true;
		}

		void OnKeyDown(object? sender, KeyEventArgs e)
		{
			if (e.Key is Key.Enter or Key.Space)
			{
				OpenFlyout();
				e.Handled = true;
			}
		}
	}
}
