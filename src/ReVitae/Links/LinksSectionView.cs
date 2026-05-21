using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Controls;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Ui;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReVitae.Links;

public sealed class LinksSectionView : UserControl
{
    private readonly ExpandableSection _section;
    private readonly StackPanel _contentPanel;
    private readonly StackPanel _entriesPanel;
    private readonly TextBlock _emptyHintTextBlock;
    private readonly Button _addButton;
    private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
    private readonly List<LinkEntry> _entries = [];
    private readonly Dictionary<string, LinkEntryCard> _cardsById = new(StringComparer.Ordinal);
    private string? _dragSourceEntryId;
    private int? _pendingDropIndex;
    private IPointer? _capturedPointer;

    public LinksSectionView()
    {
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
            IsExpanded = true
        };

        Content = _section;
        _entriesPanel.AddHandler(InputElement.PointerMovedEvent, OnEntriesPanelPointerMoved, RoutingStrategies.Tunnel);
        _entriesPanel.AddHandler(InputElement.PointerReleasedEvent, OnEntriesPanelPointerReleased, RoutingStrategies.Tunnel);
    }

    public event EventHandler? EntriesChanged;

    public IReadOnlyList<LinkEntry> Entries => _entries;

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

    public void UpdateValidation(FieldValidationResult validationResult)
    {
        foreach (var (entryId, card) in _cardsById)
        {
            var errors = validationResult.Errors
                .Where(error => LinksFieldKeys.TryParseEntryId(error.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.UpdateValidation(errors);
        }
    }

    public void AddEntry(LinkEntry? entry = null, int? insertIndex = null)
    {
        var newEntry = entry ?? new LinkEntry();
        var index = insertIndex ?? 0;
        index = Math.Clamp(index, 0, _entries.Count);
        _entries.Insert(index, newEntry);
        RebuildEntryCards();
        EntriesChanged?.Invoke(this, EventArgs.Empty);
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
            var card = new LinkEntryCard(this, entry, _localizer);
            card.Changed += (_, _) => EntriesChanged?.Invoke(this, EventArgs.Empty);
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
                EntriesChanged?.Invoke(this, EventArgs.Empty);
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
        EntriesChanged?.Invoke(this, EventArgs.Empty);
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
        private AppLocalizer _localizer;
        private readonly ExpandableSection _expandableSection;
        private readonly StackPanel _errorBadgePanel;
        private readonly TextBlock _errorBadgeTextBlock;
        private readonly AutoCompleteBox _labelAutoComplete;
        private readonly TextBox _urlTextBox;
        private readonly TextBox _noteTextBox;
        private readonly TextBlock _noteCounterTextBlock;
        private readonly Dictionary<string, TextBlock> _errorTextBlocks = new(StringComparer.Ordinal);
        private readonly Border _dragArea;

        public LinkEntryCard(LinksSectionView sectionView, LinkEntry entry, AppLocalizer localizer)
        {
            _sectionView = sectionView;
            _entry = entry;
            _localizer = localizer;

            _errorBadgeTextBlock = new TextBlock
            {
                IsVisible = false,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            _errorBadgeTextBlock.Classes.Add(UiClasses.ErrorText);

            _errorBadgePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                IsVisible = false,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    MaterialIconFactory.Create(MaterialIconKind.AlertCircle, 16),
                    _errorBadgeTextBlock
                }
            };

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

            var body = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    CreateField(_labelAutoComplete, TranslationKeys.CustomLinksLabel, LinksFieldKeys.Label),
                    CreateField(_urlTextBox, TranslationKeys.CustomLinksUrl, LinksFieldKeys.Url),
                    CreateField(_noteTextBox, TranslationKeys.CustomLinksNote, LinksFieldKeys.Note, _noteCounterTextBlock),
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

        public void UpdateValidation(IReadOnlyList<FieldValidationError> errors)
        {
            foreach (var (fieldName, textBlock) in _errorTextBlocks)
            {
                var fieldErrors = errors
                    .Where(error => error.FieldKey.EndsWith("." + fieldName, StringComparison.Ordinal))
                    .Select(error => _localizer.Get(error.Message))
                    .Distinct()
                    .ToArray();
                textBlock.Text = string.Join(Environment.NewLine, fieldErrors);
            }

            var errorCount = errors.Count;
            var showBadge = errorCount > 0 && !_expandableSection.IsExpanded;
            _errorBadgePanel.IsVisible = showBadge;
            _errorBadgeTextBlock.IsVisible = showBadge;
            _errorBadgeTextBlock.Text = showBadge
                ? _localizer.Format(TranslationKeys.CustomLinksValidationErrors, errorCount)
                : string.Empty;
        }

        private StackPanel CreateField(Control input, string labelKey, string fieldName, TextBlock? counter = null)
        {
            var label = new TextBlock { Text = _localizer.Get(labelKey) };
            var error = new TextBlock { TextWrapping = TextWrapping.Wrap };
            error.Classes.Add(UiClasses.ErrorText);
            _errorTextBlocks[fieldName] = error;

            var panel = new StackPanel { Spacing = 6 };
            panel.Classes.Add(UiClasses.FormField);
            panel.Children.Add(label);
            panel.Children.Add(input);
            panel.Children.Add(error);
            if (counter is not null)
            {
                panel.Children.Add(counter);
            }

            return panel;
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
