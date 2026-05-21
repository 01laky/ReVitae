using Avalonia.Controls;
using Avalonia.Media;
using ReVitae.Ui;

namespace ReVitae.Ui.Validation;

public static class ValidationUiFactory
{
    public static readonly IBrush ErrorForeground = new SolidColorBrush(Color.Parse("#EF5350"));

    public static TextBlock CreateErrorTextBlock()
    {
        var error = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            IsVisible = false,
            Foreground = ErrorForeground
        };
        error.Classes.Add(UiClasses.ErrorText);
        return error;
    }
}
