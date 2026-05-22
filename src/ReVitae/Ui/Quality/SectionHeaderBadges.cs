using Avalonia.Controls;
using Avalonia.Layout;
using ReVitae.Ui.Validation;

namespace ReVitae.Ui.Quality;

public sealed class SectionHeaderBadges
{
    public SectionHeaderBadges()
    {
        (ErrorBadgePanel, ErrorBadgeTextBlock) = ValidationErrorBadgeFactory.Create();
        (QualityBadgePanel, QualityBadgeTextBlock) = QualityHintBadgeFactory.Create();
        Root = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { ErrorBadgePanel, QualityBadgePanel }
        };
    }

    public StackPanel Root { get; }

    public StackPanel ErrorBadgePanel { get; }

    public TextBlock ErrorBadgeTextBlock { get; }

    public StackPanel QualityBadgePanel { get; }

    public TextBlock QualityBadgeTextBlock { get; }
}
