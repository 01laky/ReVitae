using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Material.Icons;
using ReVitae.Ui;

namespace ReVitae.Controls;

public partial class ExpandableSection : UserControl
{
	public static readonly StyledProperty<string> TitleProperty =
		AvaloniaProperty.Register<ExpandableSection, string>(nameof(Title));

	public static readonly StyledProperty<bool> IsExpandedProperty =
		AvaloniaProperty.Register<ExpandableSection, bool>(nameof(IsExpanded), defaultValue: true);

	public static readonly StyledProperty<string> ExpandToolTipProperty =
		AvaloniaProperty.Register<ExpandableSection, string>(nameof(ExpandToolTip), "Expand section");

	public static readonly StyledProperty<string> CollapseToolTipProperty =
		AvaloniaProperty.Register<ExpandableSection, string>(nameof(CollapseToolTip), "Collapse section");

	public event EventHandler? ExpandStateChanged;

	public ExpandableSection()
	{
		InitializeComponent();
		UpdateExpandedState();
	}

	public string Title
	{
		get => GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public bool IsExpanded
	{
		get => GetValue(IsExpandedProperty);
		set => SetValue(IsExpandedProperty, value);
	}

	public string ExpandToolTip
	{
		get => GetValue(ExpandToolTipProperty);
		set => SetValue(ExpandToolTipProperty, value);
	}

	public string CollapseToolTip
	{
		get => GetValue(CollapseToolTipProperty);
		set => SetValue(CollapseToolTipProperty, value);
	}

	public Control? SectionContent
	{
		get => BodyPresenter.Content as Control;
		set => BodyPresenter.Content = value;
	}

	public Control? HeaderActions
	{
		get => HeaderActionsPresenter.Content as Control;
		set => HeaderActionsPresenter.Content = value;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == TitleProperty)
		{
			TitleTextBlock.Text = Title;
		}
		else if (change.Property == IsExpandedProperty)
		{
			UpdateExpandedState();
			ExpandStateChanged?.Invoke(this, EventArgs.Empty);
		}
		else if (change.Property == ExpandToolTipProperty
			|| change.Property == CollapseToolTipProperty)
		{
			UpdateTogglePresentation();
		}
	}

	private void OnToggleClicked(object? sender, RoutedEventArgs e)
	{
		IsExpanded = !IsExpanded;
	}

	private void UpdateExpandedState()
	{
		BodyPresenter.IsVisible = IsExpanded;
		UpdateTogglePresentation();
		TitleTextBlock.Text = Title;
	}

	private void UpdateTogglePresentation()
	{
		ToggleButton.Content = MaterialIconFactory.Create(
			IsExpanded ? MaterialIconKind.ChevronDown : MaterialIconKind.ChevronRight,
			22);
		ToolTip.SetTip(ToggleButton, IsExpanded ? CollapseToolTip : ExpandToolTip);
	}
}
