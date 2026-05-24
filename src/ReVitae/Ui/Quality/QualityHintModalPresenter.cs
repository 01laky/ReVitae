using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;
using ReVitae.Ui;

namespace ReVitae.Ui.Quality;

public sealed class QualityHintModalPresenter
{
    private readonly Grid _overlay;
    private readonly StackPanel _contentPanel;
    private readonly TextBlock _titleTextBlock;

    public QualityHintModalPresenter(
        Grid overlay,
        TextBlock titleTextBlock,
        StackPanel contentPanel)
    {
        _overlay = overlay;
        _titleTextBlock = titleTextBlock;
        _contentPanel = contentPanel;
    }

    public void Show(
        AppLocalizer localizer,
        string sectionTitle,
        IReadOnlyList<CvQualityHint> hints,
        Func<CvQualityHint, bool>? navigateToHint = null,
        Action<CvQualityHint>? dismissHint = null)
    {
        _titleTextBlock.Text = localizer.Format(TranslationKeys.QualityHintFlyoutTitle, sectionTitle);
        _contentPanel.Children.Clear();

        foreach (var hint in hints)
        {
            _contentPanel.Children.Add(BuildHintRow(localizer, hint, navigateToHint, dismissHint, Hide));
        }

        _overlay.IsVisible = true;
    }

    public void Hide() => _overlay.IsVisible = false;

    private static Control BuildHintRow(
        AppLocalizer localizer,
        CvQualityHint hint,
        Func<CvQualityHint, bool>? navigateToHint,
        Action<CvQualityHint>? dismissHint,
        Action hideModal)
    {
        var row = new StackPanel { Spacing = 12 };

        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12
        };
        header.Children.Add(MaterialIconFactory.Create(
            hint.Severity == CvQualityHintSeverity.Suggestion
                ? MaterialIconKind.LightbulbOutline
                : MaterialIconKind.InformationOutline,
            28));
        header.Children.Add(new TextBlock
        {
            Text = localizer.Get(hint.MessageKey),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 16,
            LineHeight = 24
        });
        row.Children.Add(header);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(40, 0, 0, 0)
        };

        if (!string.IsNullOrEmpty(hint.FieldKey) && navigateToHint is not null)
        {
            var goButton = new Button
            {
                Content = localizer.Get(TranslationKeys.QualityHintGoToField),
                MinHeight = 40,
                Padding = new Thickness(16, 8)
            };
            goButton.Classes.Add(UiClasses.SecondaryButton);
            goButton.Click += (_, _) =>
            {
                navigateToHint(hint);
                hideModal();
            };
            actions.Children.Add(goButton);
        }

        if (dismissHint is not null)
        {
            var dismissButton = new Button
            {
                Content = localizer.Get(TranslationKeys.QualityHintDismiss),
                MinHeight = 40,
                Padding = new Thickness(16, 8)
            };
            dismissButton.Classes.Add(UiClasses.SecondaryButton);
            dismissButton.Click += (_, _) =>
            {
                dismissHint(hint);
                hideModal();
            };
            actions.Children.Add(dismissButton);
        }

        if (actions.Children.Count > 0)
        {
            row.Children.Add(actions);
        }

        return row;
    }
}
