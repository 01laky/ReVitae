using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ReVitae.Controls;
using ReVitae.Core.Cv.AdditionalInformation;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Ui;
using System;
using System.Linq;

namespace ReVitae.AdditionalInformation;

public sealed class AdditionalInformationSectionView : UserControl
{
    private readonly ExpandableSection _section;
    private readonly TextBlock _emptyHintTextBlock;
    private readonly TextBox _contentTextBox;
    private readonly TextBlock _contentCounterTextBlock;
    private readonly TextBlock _contentErrorTextBlock;
    private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
    private readonly AdditionalInformationContent _content = new();

    public AdditionalInformationSectionView()
    {
        _emptyHintTextBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
        _emptyHintTextBlock.Classes.Add(UiClasses.SecondaryText);

        _contentTextBox = new TextBox();
        _contentTextBox.Classes.Add(UiClasses.MultilineTextBox);
        _contentTextBox.TextChanged += OnContentChanged;

        _contentCounterTextBlock = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right };
        _contentCounterTextBlock.Classes.Add(UiClasses.CounterText);

        _contentErrorTextBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
        _contentErrorTextBlock.Classes.Add(UiClasses.ErrorText);

        var contentLabel = new TextBlock();
        contentLabel.SetValue(TextBlock.NameProperty, "ContentLabel");

        var contentField = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                contentLabel,
                _contentTextBox,
                _contentCounterTextBlock,
                _contentErrorTextBlock
            }
        };
        contentField.Classes.Add(UiClasses.FormField);

        var panel = new StackPanel
        {
            Spacing = 12,
            Children = { _emptyHintTextBlock, contentField }
        };

        _section = new ExpandableSection
        {
            SectionContent = panel,
            IsExpanded = true
        };

        Content = _section;
        _contentLabel = contentLabel;
    }

    private readonly TextBlock _contentLabel;

    public event EventHandler? ContentChanged;

    public AdditionalInformationContent ContentModel => _content;

    public void SetLocalizer(AppLocalizer localizer)
    {
        _localizer = localizer;
        _section.Title = _localizer.Get(TranslationKeys.AdditionalInformation);
        _section.ExpandToolTip = _localizer.Get(TranslationKeys.ExpandSection);
        _section.CollapseToolTip = _localizer.Get(TranslationKeys.CollapseSection);
        _emptyHintTextBlock.Text = _localizer.Get(TranslationKeys.AdditionalInformationEmptyHint);
        _contentLabel.Text = _localizer.Get(TranslationKeys.AdditionalInformationContent);
        UpdateCharacterCounter();
    }

    public void UpdateValidation(FieldValidationResult validationResult)
    {
        var errors = validationResult.Errors
            .Where(error => error.FieldKey == AdditionalInformationFieldKeys.Content)
            .Select(error => _localizer.Get(error.Message))
            .Distinct()
            .ToArray();
        _contentErrorTextBlock.Text = string.Join(Environment.NewLine, errors);
    }

    private void OnContentChanged(object? sender, TextChangedEventArgs e)
    {
        _content.Content = _contentTextBox.Text ?? string.Empty;
        UpdateCharacterCounter();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateCharacterCounter()
    {
        _contentCounterTextBlock.Text =
            $"{(_contentTextBox.Text ?? string.Empty).Length} / {AdditionalInformationSchema.ContentMaxLength}";
    }
}
