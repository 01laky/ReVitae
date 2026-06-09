using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Material.Icons;
using Material.Icons.Avalonia;
using ReVitae.Ui.Validation;

namespace ReVitae.Ui.Quality;

public sealed class SectionHeaderBadges
{
	private Action? _advisorClick;

	public SectionHeaderBadges()
	{
		(ErrorBadgePanel, ErrorBadgeTextBlock) = ValidationErrorBadgeFactory.Create();
		(QualityBadgePanel, QualityBadgeTextBlock) = QualityHintBadgeFactory.Create();

		// 045 A.2 — per-section "Ask AI for tips" action; hidden until a backend is active.
		AdvisorButton = new Button
		{
			IsVisible = false,
			VerticalAlignment = VerticalAlignment.Center,
			Content = new MaterialIcon { Kind = MaterialIconKind.LightbulbOnOutline, Width = 18, Height = 18 },
		};
		AdvisorButton.Classes.Add("re-vitae-expand-toggle");
		AdvisorButton.Click += (_, _) => _advisorClick?.Invoke();

		Root = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			VerticalAlignment = VerticalAlignment.Center,
			Children = { ErrorBadgePanel, QualityBadgePanel, AdvisorButton }
		};
	}

	public StackPanel Root { get; }

	public StackPanel ErrorBadgePanel { get; }

	public TextBlock ErrorBadgeTextBlock { get; }

	public StackPanel QualityBadgePanel { get; }

	public TextBlock QualityBadgeTextBlock { get; }

	public Button AdvisorButton { get; }

	/// <summary>Wires the advisor action and tooltip (045 A.2); call once per section.</summary>
	public void ConfigureAdvisor(Action onClick, string tooltip)
	{
		_advisorClick = onClick;
		ToolTip.SetTip(AdvisorButton, tooltip);
		AutomationProperties.SetName(AdvisorButton, tooltip);
	}

	public void SetAdvisorVisible(bool visible) => AdvisorButton.IsVisible = visible;
}
