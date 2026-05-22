using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;
using ReVitae.Ui;

namespace ReVitae.Ui.Quality;

public static class QualityHintFlyoutHelper
{
    public static void Show(
        Control anchor,
        AppLocalizer localizer,
        string sectionTitle,
        IReadOnlyList<CvQualityHint> hints,
        Func<CvQualityHint, bool>? navigateToHint = null,
        Action<CvQualityHint>? dismissHint = null,
        Action? flyoutOpened = null)
    {
        var content = new StackPanel
        {
            Spacing = 10,
            MaxWidth = 380,
            Margin = new Thickness(12)
        };

        content.Children.Add(new TextBlock
        {
            Text = localizer.Format(TranslationKeys.QualityHintFlyoutTitle, sectionTitle),
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        foreach (var hint in hints)
        {
            content.Children.Add(BuildHintRow(localizer, hint, navigateToHint, dismissHint, anchor));
        }

        var flyout = new Flyout
        {
            Content = new ScrollViewer
            {
                MaxHeight = 420,
                Content = content
            }
        };

        FlyoutBase.SetAttachedFlyout(anchor, flyout);
        FlyoutBase.ShowAttachedFlyout(anchor);
        flyoutOpened?.Invoke();
    }

    private static Control BuildHintRow(
        AppLocalizer localizer,
        CvQualityHint hint,
        Func<CvQualityHint, bool>? navigateToHint,
        Action<CvQualityHint>? dismissHint,
        Control anchor)
    {
        var row = new StackPanel { Spacing = 6 };

        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        header.Children.Add(MaterialIconFactory.Create(
            hint.Severity == CvQualityHintSeverity.Suggestion
                ? MaterialIconKind.LightbulbOutline
                : MaterialIconKind.InformationOutline,
            14));
        header.Children.Add(new TextBlock
        {
            Text = localizer.Get(hint.MessageKey),
            TextWrapping = TextWrapping.Wrap
        });
        row.Children.Add(header);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(20, 0, 0, 0)
        };

        if (!string.IsNullOrEmpty(hint.FieldKey) && navigateToHint is not null)
        {
            var goButton = new Button { Content = localizer.Get(TranslationKeys.QualityHintGoToField) };
            goButton.Classes.Add(UiClasses.SecondaryButton);
            goButton.Click += (_, _) =>
            {
                navigateToHint(hint);
                if (FlyoutBase.GetAttachedFlyout(anchor) is Flyout attachedFlyout)
                {
                    attachedFlyout.Hide();
                }
            };
            actions.Children.Add(goButton);
        }

        if (dismissHint is not null)
        {
            var dismissButton = new Button { Content = localizer.Get(TranslationKeys.QualityHintDismiss) };
            dismissButton.Classes.Add(UiClasses.SecondaryButton);
            dismissButton.Click += (_, _) =>
            {
                dismissHint(hint);
                if (FlyoutBase.GetAttachedFlyout(anchor) is Flyout attachedFlyout)
                {
                    attachedFlyout.Hide();
                }
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
