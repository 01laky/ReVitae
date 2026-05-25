using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Controls;
using ReVitae.Core.Cv.Languages;
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
using System.Globalization;
using System.Linq;

namespace ReVitae.Languages;

public sealed class LanguagesSectionView : UserControl, IValidationNavigableSection, IQualityHintSection
{
    private static readonly string[] EntryFieldOrder =
    [
        LanguagesFieldKeys.Language,
        LanguagesFieldKeys.Proficiency,
        LanguagesFieldKeys.CefrLevel,
        LanguagesFieldKeys.Certificate,
        LanguagesFieldKeys.Reading,
        LanguagesFieldKeys.Writing,
        LanguagesFieldKeys.Speaking,
        LanguagesFieldKeys.Listening
    ];

    private readonly ExpandableSection _section;
    private readonly SectionHeaderBadges _headerBadges;
    private readonly StackPanel _contentPanel;
    private readonly StackPanel _entriesPanel;
    private readonly TextBlock _emptyHintTextBlock;
    private readonly Button _addButton;
    private readonly ValidationTouchTracker _touchTracker = new();
    private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
    private readonly List<LanguageEntry> _entries = [];
    private readonly Dictionary<string, LanguageEntryCard> _cardsById = new(StringComparer.Ordinal);
    private string? _dragSourceEntryId;
    private bool _suppressEntriesChanged;
    private int? _pendingDropIndex;
    private IPointer? _capturedPointer;

    public LanguagesSectionView()
    {
        _headerBadges = new SectionHeaderBadges();

        _entriesPanel = new StackPanel { Spacing = 12 };
        _emptyHintTextBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
        _emptyHintTextBlock.Classes.Add(UiClasses.SecondaryText);

        _addButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _addButton.Classes.Add(UiClasses.PrimaryButton);
        _addButton.Click += (_, _) => AddEntry();

        _contentPanel = new StackPanel
        {
            Spacing = 12,
            Children = { _emptyHintTextBlock, _addButton, _entriesPanel }
        };

        _section = new ExpandableSection
        {
            SectionContent = _contentPanel,
            IsExpanded = true,
            HeaderActions = _headerBadges.Root
        };
        _section.ExpandStateChanged += (_, _) => ExpandStateChanged?.Invoke(this, EventArgs.Empty);

        Content = _section;
        _entriesPanel.AddHandler(InputElement.PointerMovedEvent, OnEntriesPanelPointerMoved, RoutingStrategies.Tunnel);
        _entriesPanel.AddHandler(InputElement.PointerReleasedEvent, OnEntriesPanelPointerReleased, RoutingStrategies.Tunnel);
    }

    public event EventHandler? EntriesChanged;

    public event EventHandler? ExpandStateChanged;

    public IReadOnlyList<LanguageEntry> Entries => _entries;

    public ValidationTouchTracker TouchTracker => _touchTracker;

    public void SetLocalizer(AppLocalizer localizer)
    {
        _localizer = localizer;
        _section.Title = _localizer.Get(TranslationKeys.Languages);
        _section.ExpandToolTip = _localizer.Get(TranslationKeys.ExpandSection);
        _section.CollapseToolTip = _localizer.Get(TranslationKeys.CollapseSection);
        _emptyHintTextBlock.Text = _localizer.Get(TranslationKeys.LanguagesEmptyHint);
        _addButton.Content = _localizer.Get(TranslationKeys.LanguagesAdd);

        foreach (var card in _cardsById.Values)
        {
            card.ApplyLocalization(_localizer);
        }
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
        var sectionErrors = validationResult.Errors
            .Where(error => LanguagesFieldKeys.TryParseEntryId(error.FieldKey, out _, out _))
            .ToArray();

        FormValidationService.UpdateSectionErrorBadge(
            _headerBadges.ErrorBadgePanel,
            _headerBadges.ErrorBadgeTextBlock,
            sectionErrors.Length,
            !_section.IsExpanded,
            _localizer,
            TranslationKeys.LanguagesValidationErrors,
            () => _section.IsExpanded = true);

        foreach (var (entryId, card) in _cardsById)
        {
            var errors = sectionErrors
                .Where(error => LanguagesFieldKeys.TryParseEntryId(error.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.UpdateValidation(errors, touchTracker);
        }
    }

    public bool ExpandAndRevealField(string fieldKey)
    {
        if (!LanguagesFieldKeys.TryParseEntryId(fieldKey, out var entryId, out _))
        {
            return false;
        }

        _section.IsExpanded = true;

        if (!_cardsById.TryGetValue(entryId, out var card))
        {
            return false;
        }

        card.SetExpanded(true);
        var control = FindControlForFieldKey(fieldKey);
        control?.Focus();
        return control is not null;
    }

    public Control? FindControlForFieldKey(string fieldKey)
    {
        if (!LanguagesFieldKeys.TryParseEntryId(fieldKey, out var entryId, out _))
        {
            return null;
        }

        return _cardsById.TryGetValue(entryId, out var card)
            ? card.FindControlForFieldKey(fieldKey)
            : null;
    }

    public IReadOnlyList<string> GetOrderedFieldKeys()
    {
        var keys = new List<string>(_entries.Count * EntryFieldOrder.Length);
        foreach (var entry in _entries)
        {
            foreach (var fieldName in EntryFieldOrder)
            {
                keys.Add(LanguagesFieldKeys.Build(entry.Id, fieldName));
            }
        }

        return keys;
    }

    public void ReplaceEntries(IReadOnlyList<LanguageEntry> entries, bool expandSection = true)
    {
        _suppressEntriesChanged = true;
        try
        {
            _entries.Clear();
            _entries.AddRange(entries);
            _section.IsExpanded = expandSection;
            RebuildEntryCards();
            foreach (var entry in _entries)
            {
                if (_cardsById.TryGetValue(entry.Id, out var card))
                {
                    card.SetExpanded(entry.HasUserInput());
                }
            }
        }
        finally
        {
            _suppressEntriesChanged = false;
        }

        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetSectionExpanded(bool isExpanded) => _section.IsExpanded = isExpanded;

    public bool IsSectionExpanded => _section.IsExpanded;

    public void ApplyImportConfidence(IReadOnlyList<ImportedFieldConfidence> confidences)
    {
        foreach (var (entryId, card) in _cardsById)
        {
            var entryConfidences = confidences
                .Where(confidence => LanguagesFieldKeys.TryParseEntryId(confidence.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.ApplyImportConfidence(entryConfidences);
        }
    }

    public void AddEntry(LanguageEntry? entry = null, int? insertIndex = null)
    {
        var newEntry = entry ?? new LanguageEntry();
        var index = insertIndex ?? 0;
        index = Math.Clamp(index, 0, _entries.Count);
        _entries.Insert(index, newEntry);
        RebuildEntryCards();
        NotifyEntriesChanged();
    }

    private void NotifyEntriesChanged()
    {
        if (!_suppressEntriesChanged)
        {
            EntriesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal void BeginEntryDrag(string entryId)
    {
        _dragSourceEntryId = entryId;
        _pendingDropIndex = FindEntryIndexById(entryId);
    }

    internal void CaptureDragPointer(IPointer pointer)
    {
        _capturedPointer = pointer;
        pointer.Capture(_entriesPanel);
    }

    private void EndDrag()
    {
        _dragSourceEntryId = null;
        _pendingDropIndex = null;
        _capturedPointer?.Capture(null);
        _capturedPointer = null;
        foreach (var card in _cardsById.Values)
        {
            card.ClearDragVisual();
        }
    }

    private void RebuildEntryCards()
    {
        _entriesPanel.Children.Clear();
        _cardsById.Clear();

        foreach (var entry in _entries)
        {
            var card = new LanguageEntryCard(this, entry, _localizer, _touchTracker);
            card.Changed += (_, _) => NotifyEntriesChanged();
            card.DuplicateRequested += (_, sourceEntry) =>
            {
                var sourceIndex = _entries.FindIndex(item => item.Id == sourceEntry.Id);
                if (sourceIndex >= 0)
                {
                    AddEntry(sourceEntry.Duplicate(), sourceIndex + 1);
                }
            };
            card.RemoveRequested += (_, sourceEntry) =>
            {
                _entries.RemoveAll(item => item.Id == sourceEntry.Id);
                RebuildEntryCards();
                NotifyEntriesChanged();
            };

            _cardsById[entry.Id] = card;
            _entriesPanel.Children.Add(card.RootBorder);
        }

        _emptyHintTextBlock.IsVisible = _entries.Count == 0;
    }

    private void OnEntriesPanelPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragSourceEntryId is null)
        {
            return;
        }

        _pendingDropIndex = FindDropIndex(e.GetPosition(_entriesPanel));
    }

    private void OnEntriesPanelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragSourceEntryId is not null && _pendingDropIndex is int targetIndex)
        {
            MoveEntryToIndex(_dragSourceEntryId, targetIndex);
        }

        EndDrag();
    }

    private void MoveEntryToIndex(string? sourceEntryId, int targetIndex)
    {
        if (string.IsNullOrWhiteSpace(sourceEntryId))
        {
            return;
        }

        var sourceIndex = _entries.FindIndex(entry => entry.Id == sourceEntryId);
        if (sourceIndex < 0 || sourceIndex == targetIndex)
        {
            return;
        }

        var entry = _entries[sourceIndex];
        _entries.RemoveAt(sourceIndex);
        if (targetIndex > sourceIndex)
        {
            targetIndex--;
        }

        _entries.Insert(Math.Clamp(targetIndex, 0, _entries.Count), entry);
        RebuildEntryCards();
        NotifyEntriesChanged();
    }

    private int? FindDropIndex(Point position)
    {
        for (var index = 0; index < _entriesPanel.Children.Count; index++)
        {
            if (_entriesPanel.Children[index] is Visual visual
                && GetBoundsRelativeTo(visual, _entriesPanel).Contains(position))
            {
                return index;
            }
        }

        return null;
    }

    private int? FindEntryIndexById(string entryId)
    {
        for (var index = 0; index < _entries.Count; index++)
        {
            if (_entries[index].Id == entryId)
            {
                return index;
            }
        }

        return null;
    }

    private static Rect GetBoundsRelativeTo(Visual visual, Visual relativeTo)
    {
        var topLeft = visual.TranslatePoint(new Point(0, 0), relativeTo);
        return topLeft is null ? default : new Rect(topLeft.Value, visual.Bounds.Size);
    }

    private sealed class LanguageEntryCard
    {
        private readonly LanguagesSectionView _sectionView;
        private readonly LanguageEntry _entry;
        private readonly ValidationFieldRegistry _fieldRegistry = new();
        private AppLocalizer _localizer;
        private readonly ExpandableSection _expandableSection;
        private readonly StackPanel _errorBadgePanel;
        private readonly TextBlock _errorBadgeTextBlock;
        private readonly TextBlock _flagTextBlock;
        private readonly AutoCompleteBox _languageAutoComplete;
        private readonly ComboBox _proficiencyComboBox;
        private readonly ComboBox _cefrComboBox;
        private readonly TextBox _certificateTextBox;
        private readonly ComboBox _readingComboBox;
        private readonly ComboBox _writingComboBox;
        private readonly ComboBox _speakingComboBox;
        private readonly ComboBox _listeningComboBox;
        private readonly Dictionary<string, TextBox> _importConfidenceFields = new(StringComparer.Ordinal);
        private readonly Border _dragArea;

        public LanguageEntryCard(
            LanguagesSectionView sectionView,
            LanguageEntry entry,
            AppLocalizer localizer,
            ValidationTouchTracker touchTracker)
        {
            _sectionView = sectionView;
            _entry = entry;
            _localizer = localizer;

            (_errorBadgePanel, _errorBadgeTextBlock) = ValidationErrorBadgeFactory.Create();

            _dragArea = new Border();
            _dragArea.Classes.Add(UiClasses.DragHandle);
            var dragIcon = MaterialIconFactory.Create(MaterialIconKind.DragVertical, 18);
            dragIcon.HorizontalAlignment = HorizontalAlignment.Center;
            dragIcon.VerticalAlignment = VerticalAlignment.Center;
            _dragArea.Child = dragIcon;
            _dragArea.PointerPressed += OnDragPointerPressed;

            _flagTextBlock = new TextBlock
            {
                FontSize = 28,
                VerticalAlignment = VerticalAlignment.Center
            };

            _languageAutoComplete = CreateLanguageAutoComplete();
            _proficiencyComboBox = CreateProficiencyComboBox();
            _cefrComboBox = CreateCefrComboBox();
            _certificateTextBox = CreateTextBox(OnFieldChanged);
            _readingComboBox = CreateOptionalProficiencyComboBox();
            _writingComboBox = CreateOptionalProficiencyComboBox();
            _speakingComboBox = CreateOptionalProficiencyComboBox();
            _listeningComboBox = CreateOptionalProficiencyComboBox();

            RegisterImportConfidenceField(LanguagesFieldKeys.Certificate, _certificateTextBox);

            var duplicateButton = new Button();
            duplicateButton.Classes.Add(UiClasses.SecondaryButton);
            duplicateButton.Click += (_, _) => DuplicateRequested?.Invoke(this, _entry);
            var removeButton = new Button();
            removeButton.Classes.Add(UiClasses.SecondaryButton);
            removeButton.Click += (_, _) => RemoveRequested?.Invoke(this, _entry);

            var languageRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                ColumnSpacing = 10
            };
            languageRow.Children.Add(_flagTextBlock);
            languageRow.Children.Add(_languageAutoComplete);
            Grid.SetColumn(_languageAutoComplete, 1);

            var entryId = _entry.Id;
            var readingPanel = WrapSubSkill(
                _readingComboBox,
                TranslationKeys.LanguagesReading,
                LanguagesFieldKeys.Reading,
                entryId,
                touchTracker);
            var writingPanel = WrapSubSkill(
                _writingComboBox,
                TranslationKeys.LanguagesWriting,
                LanguagesFieldKeys.Writing,
                entryId,
                touchTracker);
            var speakingPanel = WrapSubSkill(
                _speakingComboBox,
                TranslationKeys.LanguagesSpeaking,
                LanguagesFieldKeys.Speaking,
                entryId,
                touchTracker);
            var listeningPanel = WrapSubSkill(
                _listeningComboBox,
                TranslationKeys.LanguagesListening,
                LanguagesFieldKeys.Listening,
                entryId,
                touchTracker);

            var subSkillsGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                RowDefinitions = new RowDefinitions("Auto,Auto"),
                ColumnSpacing = 8,
                RowSpacing = 8
            };
            subSkillsGrid.Children.Add(readingPanel);
            subSkillsGrid.Children.Add(writingPanel);
            Grid.SetColumn(writingPanel, 1);
            subSkillsGrid.Children.Add(speakingPanel);
            Grid.SetRow(speakingPanel, 1);
            subSkillsGrid.Children.Add(listeningPanel);
            Grid.SetColumn(listeningPanel, 1);
            Grid.SetRow(listeningPanel, 1);

            var headerActions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children = { _dragArea, _errorBadgePanel }
            };

            var body = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.LanguagesLanguage),
                        languageRow,
                        LanguagesFieldKeys.Build(entryId, LanguagesFieldKeys.Language),
                        _fieldRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.LanguagesProficiency),
                        _proficiencyComboBox,
                        LanguagesFieldKeys.Build(entryId, LanguagesFieldKeys.Proficiency),
                        _fieldRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.LanguagesCefrLevel),
                        _cefrComboBox,
                        LanguagesFieldKeys.Build(entryId, LanguagesFieldKeys.CefrLevel),
                        _fieldRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.LanguagesCertificate),
                        _certificateTextBox,
                        LanguagesFieldKeys.Build(entryId, LanguagesFieldKeys.Certificate),
                        _fieldRegistry,
                        touchTracker),
                    new TextBlock { Text = _localizer.Get(TranslationKeys.LanguagesSubSkills) },
                    subSkillsGrid,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Children = { duplicateButton, removeButton }
                    }
                }
            };

            _expandableSection = new ExpandableSection
            {
                SectionContent = body,
                IsExpanded = true,
                HeaderActions = headerActions
            };
            _expandableSection.ExpandStateChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);

            RootBorder = new Border { Child = _expandableSection };
            RootBorder.Classes.Add(UiClasses.EntryCard);

            duplicateButton.Content = _localizer.Get(TranslationKeys.LanguagesDuplicate);
            removeButton.Content = _localizer.Get(TranslationKeys.LanguagesRemove);

            LoadFromEntry();
            ApplyLocalization(_localizer);
        }

        public event EventHandler? Changed;

        public event EventHandler<LanguageEntry>? DuplicateRequested;

        public event EventHandler<LanguageEntry>? RemoveRequested;

        public Border RootBorder { get; }

        public void SetExpanded(bool isExpanded) => _expandableSection.IsExpanded = isExpanded;

        public void ApplyImportConfidence(IReadOnlyList<ImportedFieldConfidence> confidences)
        {
            ImportConfidenceHelper.ApplyToFields(_importConfidenceFields, confidences);
        }

        public void ApplyLocalization(AppLocalizer localizer)
        {
            _localizer = localizer;
            UpdateHeaderTitle();
            _expandableSection.ExpandToolTip = _localizer.Get(TranslationKeys.LanguagesExpand);
            _expandableSection.CollapseToolTip = _localizer.Get(TranslationKeys.LanguagesCollapse);
            ToolTip.SetTip(_dragArea, _localizer.Get(TranslationKeys.LanguagesDragToReorder));
            RefreshProficiencyItems();
            RefreshCefrItems();
            RefreshOptionalProficiencyItems(_readingComboBox, _entry.ReadingProficiency);
            RefreshOptionalProficiencyItems(_writingComboBox, _entry.WritingProficiency);
            RefreshOptionalProficiencyItems(_speakingComboBox, _entry.SpeakingProficiency);
            RefreshOptionalProficiencyItems(_listeningComboBox, _entry.ListeningProficiency);
        }

        public void ClearDragVisual() => RootBorder.Opacity = 1;

        public Control? FindControlForFieldKey(string fieldKey) =>
            _fieldRegistry.FindControlForFieldKey(fieldKey);

        public void UpdateValidation(IReadOnlyList<FieldValidationError> errors, ValidationTouchTracker touchTracker)
        {
            _fieldRegistry.ApplyErrors(errors, _localizer, touchTracker);

            ValidationErrorBadgeFactory.Update(
                _errorBadgePanel,
                _errorBadgeTextBlock,
                errors.Count,
                !_expandableSection.IsExpanded,
                _localizer.Format(TranslationKeys.LanguagesValidationErrors, errors.Count),
                () => _expandableSection.IsExpanded = true);
        }

        private StackPanel WrapSubSkill(
            ComboBox comboBox,
            string labelKey,
            string fieldName,
            string entryId,
            ValidationTouchTracker touchTracker)
        {
            return ValidationFieldRegistry.CreateFieldPanel(
                _localizer.Get(labelKey),
                comboBox,
                LanguagesFieldKeys.Build(entryId, fieldName),
                _fieldRegistry,
                touchTracker);
        }

        private AutoCompleteBox CreateLanguageAutoComplete()
        {
            var autoComplete = new AutoCompleteBox
            {
                FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
                ItemsSource = LanguageSuggestions.All,
                MinimumPrefixLength = 0,
                MaxDropDownHeight = 200
            };
            autoComplete.TextChanged += OnFieldChanged;
            return autoComplete;
        }

        private ComboBox CreateProficiencyComboBox()
        {
            var comboBox = new ComboBox();
            comboBox.SelectionChanged += OnSelectionChanged;
            return comboBox;
        }

        private ComboBox CreateCefrComboBox()
        {
            var comboBox = new ComboBox();
            comboBox.SelectionChanged += OnSelectionChanged;
            return comboBox;
        }

        private ComboBox CreateOptionalProficiencyComboBox()
        {
            var comboBox = new ComboBox();
            comboBox.SelectionChanged += OnSelectionChanged;
            return comboBox;
        }

        private void RefreshProficiencyItems()
        {
            var selected = _entry.Proficiency;
            _proficiencyComboBox.ItemsSource = LanguageProficiencyExtensions.SupportedValues
                .Select(level => new ComboBoxItem
                {
                    Content = _localizer.Get(level.ToTranslationKey()),
                    Tag = level
                })
                .ToArray();
            _proficiencyComboBox.SelectedItem = _proficiencyComboBox.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag is LanguageProficiency level && level == selected);
        }

        private void RefreshCefrItems()
        {
            var items = new List<ComboBoxItem>
            {
                new() { Content = _localizer.Get(TranslationKeys.LanguagesCefrNone), Tag = null }
            };
            items.AddRange(CefrLevelExtensions.SupportedValues.Select(level => new ComboBoxItem
            {
                Content = _localizer.Get(level.ToTranslationKey()),
                Tag = level
            }));
            _cefrComboBox.ItemsSource = items;
            _cefrComboBox.SelectedItem = items.FirstOrDefault(item =>
                item.Tag is CefrLevel level && level == _entry.CefrLevel)
                ?? items[0];
        }

        private void RefreshOptionalProficiencyItems(ComboBox comboBox, LanguageProficiency? selected)
        {
            var items = new List<ComboBoxItem>
            {
                new() { Content = _localizer.Get(TranslationKeys.LanguagesProficiencyNone), Tag = null }
            };
            items.AddRange(LanguageProficiencyExtensions.SupportedValues.Select(level => new ComboBoxItem
            {
                Content = _localizer.Get(level.ToTranslationKey()),
                Tag = level
            }));
            comboBox.ItemsSource = items;
            comboBox.SelectedItem = items.FirstOrDefault(item =>
                item.Tag is LanguageProficiency level && level == selected)
                ?? items[0];
        }

        private void LoadFromEntry()
        {
            _languageAutoComplete.Text = _entry.Language;
            _certificateTextBox.Text = _entry.Certificate;
            UpdateFlagDisplay();
            RefreshProficiencyItems();
            RefreshCefrItems();
            RefreshOptionalProficiencyItems(_readingComboBox, _entry.ReadingProficiency);
            RefreshOptionalProficiencyItems(_writingComboBox, _entry.WritingProficiency);
            RefreshOptionalProficiencyItems(_speakingComboBox, _entry.SpeakingProficiency);
            RefreshOptionalProficiencyItems(_listeningComboBox, _entry.ListeningProficiency);
        }

        private void SyncToEntry()
        {
            _entry.Language = _languageAutoComplete.Text ?? string.Empty;
            _entry.Proficiency = _proficiencyComboBox.SelectedItem is ComboBoxItem { Tag: LanguageProficiency proficiency }
                ? proficiency
                : LanguageProficiency.Intermediate;
            _entry.CefrLevel = _cefrComboBox.SelectedItem is ComboBoxItem { Tag: CefrLevel cefr } ? cefr : null;
            _entry.Certificate = _certificateTextBox.Text ?? string.Empty;
            _entry.ReadingProficiency = GetOptionalProficiency(_readingComboBox);
            _entry.WritingProficiency = GetOptionalProficiency(_writingComboBox);
            _entry.SpeakingProficiency = GetOptionalProficiency(_speakingComboBox);
            _entry.ListeningProficiency = GetOptionalProficiency(_listeningComboBox);
        }

        private static LanguageProficiency? GetOptionalProficiency(ComboBox comboBox)
        {
            return comboBox.SelectedItem is ComboBoxItem { Tag: LanguageProficiency proficiency } ? proficiency : null;
        }

        private void OnFieldChanged(object? sender, RoutedEventArgs e)
        {
            SyncToEntry();
            UpdateFlagDisplay();
            UpdateHeaderTitle();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => OnFieldChanged(sender, e);

        private void UpdateFlagDisplay()
        {
            _flagTextBlock.Text = LanguageFlagResolver.ResolveFlagEmoji(_entry.Language);
        }

        private void UpdateHeaderTitle()
        {
            var cefrLabel = _entry.CefrLevel is null
                ? null
                : _localizer.Get(_entry.CefrLevel.Value.ToTranslationKey());
            _expandableSection.Title = _entry.BuildHeaderSummary(
                LanguageFlagResolver.ResolveFlagEmoji(_entry.Language),
                _localizer.Get(_entry.Proficiency.ToTranslationKey()),
                cefrLabel);
        }

        private void OnDragPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(_dragArea).Properties.IsLeftButtonPressed)
            {
                return;
            }

            RootBorder.Opacity = 0.75;
            _sectionView.BeginEntryDrag(_entry.Id);
            _sectionView.CaptureDragPointer(e.Pointer);
        }

        private void RegisterImportConfidenceField(string fieldName, TextBox textBox)
        {
            _importConfidenceFields[LanguagesFieldKeys.Build(_entry.Id, fieldName)] = textBox;
            textBox.TextChanged += (_, _) => textBox.Classes.Remove(UiClasses.ImportHint);
        }

        private static TextBox CreateTextBox(EventHandler<RoutedEventArgs> onChanged)
        {
            var textBox = new TextBox();
            textBox.TextChanged += onChanged;
            return textBox;
        }
    }
}
