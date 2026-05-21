using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ReVitae.Core.Export;
using ReVitae.Core.Localization;
using ReVitae.Preview;

namespace ReVitae;

public partial class MainWindow
{
    private readonly List<TemplateCardUi> _templateCards = [];
    private bool _templateCardsInitialized;

    private sealed class TemplateCardUi
    {
        public required Button Button { get; init; }

        public required TextBlock SelectedLabel { get; init; }

        public required CvExportTemplateId TemplateId { get; init; }
    }

    private void EnsureTemplateCardsInitialized()
    {
        if (_templateCardsInitialized)
        {
            return;
        }

        TemplateCardsWrapPanel.Children.Clear();
        _templateCards.Clear();

        foreach (var template in CvExportTemplateCatalog.All)
        {
            var selectedLabel = new TextBlock
            {
                Classes = { "re-vitae-accent" },
                Text = _localizer.Get(TranslationKeys.Selected),
                IsVisible = false
            };

            var card = new StackPanel { Spacing = 10 };
            card.Children.Add(
                new Border
                {
                    Classes = { "re-vitae-template-thumbnail" },
                    Child = CvExportTemplateThumbnailFactory.Create(template.Id)
                });
            card.Children.Add(new TextBlock
            {
                Text = _localizer.Get(template.NameKey),
                FontWeight = FontWeight.SemiBold
            });
            card.Children.Add(new TextBlock
            {
                Classes = { "re-vitae-secondary" },
                Text = _localizer.Get(template.DescriptionKey),
                TextWrapping = TextWrapping.Wrap
            });
            card.Children.Add(selectedLabel);

            var button = new Button
            {
                Classes = { "re-vitae-template-card" },
                Content = card,
                Tag = template.Id
            };
            button.Click += OnTemplateCardClicked;

            TemplateCardsWrapPanel.Children.Add(button);
            _templateCards.Add(new TemplateCardUi
            {
                Button = button,
                SelectedLabel = selectedLabel,
                TemplateId = template.Id
            });
        }

        _templateCardsInitialized = true;
    }

    private void RefreshTemplateCardLabels()
    {
        EnsureTemplateCardsInitialized();

        foreach (var card in _templateCards)
        {
            var descriptor = CvExportTemplateCatalog.Get(card.TemplateId);
            var stack = (StackPanel)card.Button.Content!;
            ((TextBlock)stack.Children[1]).Text = _localizer.Get(descriptor.NameKey);
            ((TextBlock)stack.Children[2]).Text = _localizer.Get(descriptor.DescriptionKey);
            card.SelectedLabel.Text = _localizer.Get(TranslationKeys.Selected);
        }
    }

    private void OnTemplateCardClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CvExportTemplateId templateId })
        {
            SelectTemplate(templateId);
        }
    }

    private void UpdateTemplateSelectionState()
    {
        EnsureTemplateCardsInitialized();

        foreach (var card in _templateCards)
        {
            var isSelected = _selectedTemplate == card.TemplateId;
            card.SelectedLabel.IsVisible = isSelected;
            card.Button.Classes.Set("selected", isSelected);
        }
    }
}
