using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ReVitae.Controls;
using ReVitae.Core.Cv.AdditionalInformation;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;
using ReVitae.Core.Validation;
using ReVitae.Core.Validation.Presentation;
using ReVitae.Ui;
using ReVitae.Ui.Quality;
using ReVitae.Ui.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReVitae.AdditionalInformation;

public sealed class AdditionalInformationSectionView : UserControl, IValidationNavigableSection, IQualityHintSection
{
	private const string ValidationErrorsKey = TranslationKeys.AdditionalInformationValidationErrors;

	private readonly ExpandableSection _section;
	private readonly SectionHeaderBadges _headerBadges;
	private readonly TextBlock _emptyHintTextBlock;
	private readonly TextBox _contentTextBox;
	private readonly TextBlock _contentCounterTextBlock;
	private readonly TextBlock _contentLabel;
	private readonly ValidationFieldRegistry _fieldRegistry = new();
	private readonly ValidationTouchTracker _touchTracker = new();
	private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
	private readonly AdditionalInformationContent _content = new();
	private bool _suppressContentChanged;

	public AdditionalInformationSectionView()
	{
		_headerBadges = new SectionHeaderBadges();

		_emptyHintTextBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
		_emptyHintTextBlock.Classes.Add(UiClasses.SecondaryText);

		_contentTextBox = new TextBox();
		_contentTextBox.Classes.Add(UiClasses.MultilineTextBox);
		_contentTextBox.TextChanged += OnContentChanged;

		_contentCounterTextBlock = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right };
		_contentCounterTextBlock.Classes.Add(UiClasses.CounterText);

		_contentLabel = new TextBlock();

		var contentErrorTextBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, IsVisible = false };
		contentErrorTextBlock.Classes.Add(UiClasses.ErrorText);
		var contentBinding = new ValidationFieldBinding(
			AdditionalInformationFieldKeys.Content,
			_contentTextBox,
			contentErrorTextBlock);
		contentBinding.WireTouchTracking(_touchTracker);
		_fieldRegistry.Register(contentBinding);

		var contentField = new StackPanel
		{
			Spacing = 6,
			Children =
			{
				_contentLabel,
				_contentTextBox,
				_contentCounterTextBlock,
				contentErrorTextBlock
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
			IsExpanded = true,
			HeaderActions = _headerBadges.Root
		};
		_section.ExpandStateChanged += (_, _) => ExpandStateChanged?.Invoke(this, EventArgs.Empty);

		Content = _section;
	}

	public event EventHandler? ContentChanged;

	public event EventHandler? ExpandStateChanged;

	public AdditionalInformationContent ContentModel => _content;

	public ValidationTouchTracker TouchTracker => _touchTracker;

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

	public void ApplyQualityHints(
		IReadOnlyList<CvQualityHint> sectionHints,
		AppLocalizer localizer,
		Func<CvQualityHint, bool>? navigateToHint,
		Action<CvQualityHint>? dismissHint,
		Action? flyoutOpened)
	{
		QualityHintsService.UpdateSectionQualityBadge(
			_headerBadges.QualityBadgePanel,
			_headerBadges.QualityBadgeTextBlock,
			sectionHints,
			localizer,
			_section.Title ?? string.Empty,
			navigateToHint,
			dismissHint,
			flyoutOpened);
	}

	public void UpdateValidation(FieldValidationResult validationResult) =>
		UpdateValidation(validationResult, _touchTracker);

	public void UpdateValidation(FieldValidationResult validationResult, ValidationTouchTracker touchTracker)
	{
		var errors = validationResult.Errors
			.Where(error => error.FieldKey == AdditionalInformationFieldKeys.Content)
			.ToArray();

		_fieldRegistry.ApplyErrors(errors, _localizer, touchTracker);

		FormValidationService.UpdateSectionErrorBadge(
			_headerBadges.ErrorBadgePanel,
			_headerBadges.ErrorBadgeTextBlock,
			errors.Length,
			!_section.IsExpanded,
			_localizer,
			ValidationErrorsKey,
			() => _section.IsExpanded = true);
	}

	public bool ExpandAndRevealField(string fieldKey)
	{
		if (fieldKey != AdditionalInformationFieldKeys.Content)
		{
			return false;
		}

		_section.IsExpanded = true;
		var control = FindControlForFieldKey(fieldKey);
		control?.Focus();
		return control is not null;
	}

	public Control? FindControlForFieldKey(string fieldKey) =>
		_fieldRegistry.FindControlForFieldKey(fieldKey);

	public IReadOnlyList<string> GetOrderedFieldKeys() =>
		[AdditionalInformationFieldKeys.Content];

	public void SetContent(string content, bool expandSection = true)
	{
		_suppressContentChanged = true;
		try
		{
			_content.Content = content;
			_contentTextBox.Text = content;
			_section.IsExpanded = expandSection;
			UpdateCharacterCounter();
		}
		finally
		{
			_suppressContentChanged = false;
		}

		ContentChanged?.Invoke(this, EventArgs.Empty);
	}

	public void SetSectionExpanded(bool isExpanded) => _section.IsExpanded = isExpanded;

	public bool IsSectionExpanded => _section.IsExpanded;

	public void ApplyImportConfidence(IReadOnlyList<ImportedFieldConfidence> confidences)
	{
		var contentConfidence = confidences.FirstOrDefault(
			confidence => confidence.FieldKey == AdditionalInformationFieldKeys.Content);
		if (contentConfidence is not null)
		{
			ImportConfidenceHelper.Apply(_contentTextBox, contentConfidence.Confidence);
		}
	}

	private void OnContentChanged(object? sender, TextChangedEventArgs e)
	{
		_contentTextBox.Classes.Remove(UiClasses.ImportHint);
		_content.Content = _contentTextBox.Text ?? string.Empty;
		UpdateCharacterCounter();
		if (!_suppressContentChanged)
		{
			ContentChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private void UpdateCharacterCounter()
	{
		_contentCounterTextBlock.Text =
			$"{(_contentTextBox.Text ?? string.Empty).Length} / {AdditionalInformationSchema.ContentMaxLength}";
	}
}
