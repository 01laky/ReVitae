using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Controls;
using ReVitae.Core.Cv.Links;
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

namespace ReVitae.Links;

public sealed class LinksSectionView : UserControl, IValidationNavigableSection, IQualityHintSection
{
    private static readonly string[] EntryFieldOrder =
    [
        LinksFieldKeys.Label,
        LinksFieldKeys.Url,
        LinksFieldKeys.Note
    ];

    private readonly ExpandableSection _section;
    private readonly SectionHeaderBadges _headerBadges;
    private readonly StackPanel _contentPanel;
    private readonly StackPanel _entriesPanel;
    private readonly TextBlock _emptyHintTextBlock;
    private readonly Button _addButton;
    private readonly ValidationTouchTracker _touchTracker = new();
    private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
    private readonly List<LinkEntry> _entries = [];
    private readonly Dictionary<string, LinkEntryCard> _cardsById = new(StringComparer.Ordinal);
    private string? _dragSourceEntryId;
    private bool _suppressEntriesChanged;
    private int? _pendingDropIndex;
    private IPointer? _capturedPointer;

    public LinksSectionView()
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

    public IReadOnlyList<LinkEntry> Entries => _entries;

    public ValidationTouchTracker TouchTracker => _touchTracker;

    public void SetLocalizer(AppLocalizer localizer)
    {
        _localizer = localizer;
        _section.Title = _localizer.Get(TranslationKeys.CustomLinks);
        _section.ExpandToolTip = _localizer.Get(TranslationKeys.ExpandSection);
        _section.CollapseToolTip = _localizer.Get(TranslationKeys.CollapseSection);
        _emptyHintTextBlock.Text = _localizer.Get(TranslationKeys.CustomLinksEmptyHint);
        _addButton.Content = _localizer.Get(TranslationKeys.CustomLinksAdd);

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
            .Where(error => LinksFieldKeys.TryParseEntryId(error.FieldKey, out _, out _))
            .ToArray();

        FormValidationService.UpdateSectionErrorBadge(
            _headerBadges.ErrorBadgePanel,
            _headerBadges.ErrorBadgeTextBlock,
            sectionErrors.Length,
            !_section.IsExpanded,
            _localizer,
            TranslationKeys.CustomLinksValidationErrors,
            () => _section.IsExpanded = true);

        foreach (var (entryId, card) in _cardsById)
        {
            var errors = sectionErrors
                .Where(error => LinksFieldKeys.TryParseEntryId(error.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.UpdateValidation(errors, touchTracker);
        }
    }

    public bool ExpandAndRevealField(string fieldKey)
    {
        if (!LinksFieldKeys.TryParseEntryId(fieldKey, out var entryId, out _))
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
        if (!LinksFieldKeys.TryParseEntryId(fieldKey, out var entryId, out _))
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
                keys.Add(LinksFieldKeys.Build(entry.Id, fieldName));
            }
        }

        return keys;
    }

    public void ReplaceEntries(IReadOnlyList<LinkEntry> entries, bool expandSection = true)
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
                .Where(confidence => LinksFieldKeys.TryParseEntryId(confidence.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.ApplyImportConfidence(entryConfidences);
        }
    }

    public void AddEntry(LinkEntry? entry = null, int? insertIndex = null)
    {
        var newEntry = entry ?? new LinkEntry();
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
            var card = new LinkEntryCard(this, entry, _localizer, _touchTracker);
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

    private sealed class LinkEntryCard
    {
        private readonly LinksSectionView _sectionView;
        private readonly LinkEntry _entry;
        private readonly ValidationFieldRegistry _fieldRegistry = new();
        private AppLocalizer _localizer;
        private readonly ExpandableSection _expandableSection;
        private readonly StackPanel _errorBadgePanel;
        private readonly TextBlock _errorBadgeTextBlock;
        private readonly AutoCompleteBox _labelAutoComplete;
        private readonly TextBox _urlTextBox;
        private readonly TextBox _noteTextBox;
        private readonly TextBlock _noteCounterTextBlock;
        private readonly Dictionary<string, TextBox> _importConfidenceFields = new(StringComparer.Ordinal);
        private readonly Border _dragArea;

        public LinkEntryCard(
            LinksSectionView sectionView,
            LinkEntry entry,
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

            _labelAutoComplete = CreateLabelAutoComplete();
            _urlTextBox = CreateTextBox(OnFieldChanged);
            _noteTextBox = CreateTextBox(OnFieldChanged);
            _noteCounterTextBlock = CreateCounterTextBlock();

            RegisterImportConfidenceField(LinksFieldKeys.Url, _urlTextBox);
            RegisterImportConfidenceField(LinksFieldKeys.Note, _noteTextBox);

            var duplicateButton = new Button();
            duplicateButton.Classes.Add(UiClasses.SecondaryButton);
            duplicateButton.Click += (_, _) => DuplicateRequested?.Invoke(this, _entry);
            var removeButton = new Button();
            removeButton.Classes.Add(UiClasses.SecondaryButton);
            removeButton.Click += (_, _) => RemoveRequested?.Invoke(this, _entry);

            var headerActions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children = { _dragArea, _errorBadgePanel }
            };

            var entryId = _entry.Id;
            var body = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.CustomLinksLabel),
                        _labelAutoComplete,
                        LinksFieldKeys.Build(entryId, LinksFieldKeys.Label),
                        _fieldRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.CustomLinksUrl),
                        _urlTextBox,
                        LinksFieldKeys.Build(entryId, LinksFieldKeys.Url),
                        _fieldRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.CustomLinksNote),
                        _noteTextBox,
                        LinksFieldKeys.Build(entryId, LinksFieldKeys.Note),
                        _fieldRegistry,
                        touchTracker,
                        _noteCounterTextBlock),
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

            duplicateButton.Content = _localizer.Get(TranslationKeys.CustomLinksDuplicate);
            removeButton.Content = _localizer.Get(TranslationKeys.CustomLinksRemove);

            LoadFromEntry();
            ApplyLocalization(_localizer);
        }

        public event EventHandler? Changed;

        public event EventHandler<LinkEntry>? DuplicateRequested;

        public event EventHandler<LinkEntry>? RemoveRequested;

        public Border RootBorder { get; }

        public void SetExpanded(bool isExpanded) => _expandableSection.IsExpanded = isExpanded;

        public void ApplyImportConfidence(IReadOnlyList<ImportedFieldConfidence> confidences)
        {
            ImportConfidenceHelper.ApplyToFields(_importConfidenceFields, confidences);
        }

        public void ApplyLocalization(AppLocalizer localizer)
        {
            _localizer = localizer;
            _expandableSection.Title = _entry.BuildHeaderSummary();
            _expandableSection.ExpandToolTip = _localizer.Get(TranslationKeys.CustomLinksExpand);
            _expandableSection.CollapseToolTip = _localizer.Get(TranslationKeys.CustomLinksCollapse);
            ToolTip.SetTip(_dragArea, _localizer.Get(TranslationKeys.CustomLinksDragToReorder));
            UpdateCharacterCounters();
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
                _localizer.Format(TranslationKeys.CustomLinksValidationErrors, errors.Count),
                () => _expandableSection.IsExpanded = true);
        }

        private AutoCompleteBox CreateLabelAutoComplete()
        {
            var autoComplete = new AutoCompleteBox
            {
                FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
                ItemsSource = LinkLabelSuggestions.All,
                MinimumPrefixLength = 0,
                MaxDropDownHeight = 200
            };
            autoComplete.TextChanged += OnFieldChanged;
            return autoComplete;
        }

        private void LoadFromEntry()
        {
            _labelAutoComplete.Text = _entry.Label;
            _urlTextBox.Text = _entry.Url;
            _noteTextBox.Text = _entry.Note;
        }

        private void SyncToEntry()
        {
            _entry.Label = _labelAutoComplete.Text ?? string.Empty;
            _entry.Url = _urlTextBox.Text ?? string.Empty;
            _entry.Note = _noteTextBox.Text ?? string.Empty;
        }

        private void OnFieldChanged(object? sender, RoutedEventArgs e)
        {
            SyncToEntry();
            _expandableSection.Title = _entry.BuildHeaderSummary();
            UpdateCharacterCounters();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateCharacterCounters()
        {
            _noteCounterTextBlock.Text =
                $"{(_noteTextBox.Text ?? string.Empty).Length} / {LinksSchema.NoteMaxLength}";
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
            _importConfidenceFields[LinksFieldKeys.Build(_entry.Id, fieldName)] = textBox;
            textBox.TextChanged += (_, _) => textBox.Classes.Remove(UiClasses.ImportHint);
        }

        private static TextBox CreateTextBox(EventHandler<RoutedEventArgs> onChanged)
        {
            var textBox = new TextBox();
            textBox.TextChanged += onChanged;
            return textBox;
        }

        private static TextBlock CreateCounterTextBlock()
        {
            var counter = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right };
            counter.Classes.Add(UiClasses.CounterText);
            return counter;
        }
    }
}
