using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Controls;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Core.Validation.Presentation;
using ReVitae.Ui;
using ReVitae.Ui.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReVitae.Certificates;

public sealed class CertificatesSectionView : UserControl, IValidationNavigableSection
{
    private static readonly string[] EntryFieldOrder =
    [
        CertificatesFieldKeys.Name,
        CertificatesFieldKeys.Issuer,
        CertificatesFieldKeys.IssueMonth,
        CertificatesFieldKeys.IssueYear,
        CertificatesFieldKeys.DateRange,
        CertificatesFieldKeys.ExpirationMonth,
        CertificatesFieldKeys.ExpirationYear,
        CertificatesFieldKeys.CredentialId,
        CertificatesFieldKeys.CredentialUrl,
        CertificatesFieldKeys.Description
    ];

    private readonly ExpandableSection _section;
    private readonly StackPanel _sectionErrorBadgePanel;
    private readonly TextBlock _sectionErrorBadgeTextBlock;
    private readonly StackPanel _contentPanel;
    private readonly StackPanel _entriesPanel;
    private readonly TextBlock _emptyHintTextBlock;
    private readonly Button _addButton;
    private readonly Button _sortButton;
    private readonly ValidationTouchTracker _touchTracker = new();
    private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
    private readonly List<CertificateEntry> _entries = [];
    private readonly Dictionary<string, CertificateEntryCard> _cardsById = new(StringComparer.Ordinal);
    private string? _dragSourceEntryId;
    private bool _suppressEntriesChanged;
    private int? _pendingDropIndex;
    private IPointer? _capturedPointer;

    public CertificatesSectionView()
    {
        _entriesPanel = new StackPanel { Spacing = 12 };
        _emptyHintTextBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
        _emptyHintTextBlock.Classes.Add(UiClasses.SecondaryText);

        _addButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _addButton.Classes.Add(UiClasses.PrimaryButton);
        _addButton.Click += (_, _) => AddEntry();

        _sortButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _sortButton.Classes.Add(UiClasses.SecondaryButton);
        _sortButton.Click += OnSortByDateClicked;

        _contentPanel = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                _emptyHintTextBlock,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children = { _addButton, _sortButton }
                },
                _entriesPanel
            }
        };

        (_sectionErrorBadgePanel, _sectionErrorBadgeTextBlock) = ValidationErrorBadgeFactory.Create();

        _section = new ExpandableSection
        {
            SectionContent = _contentPanel,
            IsExpanded = true,
            HeaderActions = _sectionErrorBadgePanel
        };
        _section.ExpandStateChanged += (_, _) => ExpandStateChanged?.Invoke(this, EventArgs.Empty);

        Content = _section;
        _entriesPanel.AddHandler(InputElement.PointerMovedEvent, OnEntriesPanelPointerMoved, RoutingStrategies.Tunnel);
        _entriesPanel.AddHandler(InputElement.PointerReleasedEvent, OnEntriesPanelPointerReleased, RoutingStrategies.Tunnel);
    }

    public event EventHandler? EntriesChanged;

    public event EventHandler? ExpandStateChanged;

    public IReadOnlyList<CertificateEntry> Entries => _entries;

    public ValidationTouchTracker TouchTracker => _touchTracker;

    public void SetLocalizer(AppLocalizer localizer)
    {
        _localizer = localizer;
        _section.Title = _localizer.Get(TranslationKeys.Certificates);
        _section.ExpandToolTip = _localizer.Get(TranslationKeys.ExpandSection);
        _section.CollapseToolTip = _localizer.Get(TranslationKeys.CollapseSection);
        _emptyHintTextBlock.Text = _localizer.Get(TranslationKeys.CertificatesEmptyHint);
        _addButton.Content = _localizer.Get(TranslationKeys.CertificatesAdd);
        _sortButton.Content = _localizer.Get(TranslationKeys.CertificatesSortByDate);

        foreach (var card in _cardsById.Values)
        {
            card.ApplyLocalization(_localizer);
        }
    }

    public void UpdateValidation(FieldValidationResult validationResult) =>
        UpdateValidation(validationResult, _touchTracker);

    public void UpdateValidation(FieldValidationResult validationResult, ValidationTouchTracker touchTracker)
    {
        var sectionErrors = validationResult.Errors
            .Where(error => CertificatesFieldKeys.TryParseEntryId(error.FieldKey, out _, out _))
            .ToArray();

        FormValidationService.UpdateSectionErrorBadge(
            _sectionErrorBadgePanel,
            _sectionErrorBadgeTextBlock,
            sectionErrors.Length,
            !_section.IsExpanded,
            _localizer,
            TranslationKeys.CertificatesValidationErrors,
            () => _section.IsExpanded = true);

        foreach (var (entryId, card) in _cardsById)
        {
            var errors = validationResult.Errors
                .Where(error => CertificatesFieldKeys.TryParseEntryId(error.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.UpdateValidation(errors, touchTracker);
        }
    }

    public bool ExpandAndRevealField(string fieldKey)
    {
        if (!CertificatesFieldKeys.TryParseEntryId(fieldKey, out var entryId, out _))
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
        if (!CertificatesFieldKeys.TryParseEntryId(fieldKey, out var entryId, out _))
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
                keys.Add(CertificatesFieldKeys.Build(entry.Id, fieldName));
            }
        }

        return keys;
    }

    public void ReplaceEntries(IReadOnlyList<CertificateEntry> entries, bool expandSection = true)
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

    public void ApplyImportConfidence(IReadOnlyList<ImportedFieldConfidence> confidences)
    {
        foreach (var (entryId, card) in _cardsById)
        {
            var entryConfidences = confidences
                .Where(confidence => CertificatesFieldKeys.TryParseEntryId(confidence.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.ApplyImportConfidence(entryConfidences);
        }
    }

    public void AddEntry(CertificateEntry? entry = null, int? insertIndex = null)
    {
        var newEntry = entry ?? new CertificateEntry();
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
            var card = new CertificateEntryCard(this, entry, _localizer, _touchTracker);
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

    private void OnSortByDateClicked(object? sender, RoutedEventArgs e)
    {
        var sorted = CertificateSorter.SortByDateNewestFirst(_entries);
        _entries.Clear();
        _entries.AddRange(sorted);
        RebuildEntryCards();
        NotifyEntriesChanged();
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

    private sealed class CertificateEntryCard
    {
        private readonly CertificatesSectionView _sectionView;
        private readonly CertificateEntry _entry;
        private readonly ValidationFieldRegistry _fieldRegistry = new();
        private AppLocalizer _localizer;
        private bool _isLoadingFromEntry;
        private readonly ExpandableSection _expandableSection;
        private readonly StackPanel _errorBadgePanel;
        private readonly TextBlock _errorBadgeTextBlock;
        private readonly TextBox _nameTextBox;
        private readonly AutoCompleteBox _issuerAutoComplete;
        private readonly DatePicker _issueDatePicker;
        private readonly DatePicker _expirationDatePicker;
        private readonly TextBox _credentialIdTextBox;
        private readonly TextBox _credentialUrlTextBox;
        private readonly TextBox _descriptionTextBox;
        private readonly TextBlock _descriptionCounterTextBlock;
        private readonly Dictionary<string, TextBox> _importConfidenceFields = new(StringComparer.Ordinal);
        private readonly Border _dragArea;

        public CertificateEntryCard(
            CertificatesSectionView sectionView,
            CertificateEntry entry,
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

            _nameTextBox = CreateTextBox(OnFieldChanged);
            _issuerAutoComplete = CreateIssuerAutoComplete();
            _issueDatePicker = MonthYearDateHelper.CreatePicker(OnDateChanged);
            _expirationDatePicker = MonthYearDateHelper.CreatePicker(OnDateChanged);
            _credentialIdTextBox = CreateTextBox(OnFieldChanged);
            _credentialUrlTextBox = CreateTextBox(OnFieldChanged);
            _descriptionTextBox = CreateMultilineTextBox(OnFieldChanged);
            _descriptionCounterTextBlock = CreateCounterTextBlock();

            RegisterImportConfidenceField(CertificatesFieldKeys.Name, _nameTextBox);
            RegisterImportConfidenceField(CertificatesFieldKeys.CredentialId, _credentialIdTextBox);
            RegisterImportConfidenceField(CertificatesFieldKeys.CredentialUrl, _credentialUrlTextBox);
            RegisterImportConfidenceField(CertificatesFieldKeys.Description, _descriptionTextBox);

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
                        _localizer.Get(TranslationKeys.CertificatesName),
                        _nameTextBox,
                        CertificatesFieldKeys.Build(entryId, CertificatesFieldKeys.Name),
                        _fieldRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.CertificatesIssuer),
                        _issuerAutoComplete,
                        CertificatesFieldKeys.Build(entryId, CertificatesFieldKeys.Issuer),
                        _fieldRegistry,
                        touchTracker),
                    ValidatedDateRangeBinding.CreatePanel(
                        _localizer.Get(TranslationKeys.CertificatesIssueDate),
                        _issueDatePicker,
                        CertificatesFieldKeys.Build(entryId, CertificatesFieldKeys.IssueMonth),
                        CertificatesFieldKeys.Build(entryId, CertificatesFieldKeys.IssueMonth),
                        CertificatesFieldKeys.Build(entryId, CertificatesFieldKeys.IssueYear),
                        CertificatesFieldKeys.Build(entryId, CertificatesFieldKeys.DateRange),
                        _fieldRegistry,
                        touchTracker),
                    ValidatedDateRangeBinding.CreatePanel(
                        _localizer.Get(TranslationKeys.CertificatesExpirationDate),
                        _expirationDatePicker,
                        CertificatesFieldKeys.Build(entryId, CertificatesFieldKeys.ExpirationMonth),
                        CertificatesFieldKeys.Build(entryId, CertificatesFieldKeys.ExpirationMonth),
                        CertificatesFieldKeys.Build(entryId, CertificatesFieldKeys.ExpirationYear),
                        CertificatesFieldKeys.Build(entryId, CertificatesFieldKeys.DateRange),
                        _fieldRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.CertificatesCredentialId),
                        _credentialIdTextBox,
                        CertificatesFieldKeys.Build(entryId, CertificatesFieldKeys.CredentialId),
                        _fieldRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.CertificatesCredentialUrl),
                        _credentialUrlTextBox,
                        CertificatesFieldKeys.Build(entryId, CertificatesFieldKeys.CredentialUrl),
                        _fieldRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.CertificatesDescription),
                        _descriptionTextBox,
                        CertificatesFieldKeys.Build(entryId, CertificatesFieldKeys.Description),
                        _fieldRegistry,
                        touchTracker,
                        _descriptionCounterTextBlock),
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

            duplicateButton.Content = _localizer.Get(TranslationKeys.CertificatesDuplicate);
            removeButton.Content = _localizer.Get(TranslationKeys.CertificatesRemove);

            LoadFromEntry();
            ApplyLocalization(_localizer);
        }

        public event EventHandler? Changed;

        public event EventHandler<CertificateEntry>? DuplicateRequested;

        public event EventHandler<CertificateEntry>? RemoveRequested;

        public Border RootBorder { get; }

        public void SetExpanded(bool isExpanded) => _expandableSection.IsExpanded = isExpanded;

        public Control? FindControlForFieldKey(string fieldKey) =>
            _fieldRegistry.FindControlForFieldKey(fieldKey);

        public void ApplyImportConfidence(IReadOnlyList<ImportedFieldConfidence> confidences)
        {
            ImportConfidenceHelper.ApplyToFields(_importConfidenceFields, confidences);
        }

        public void ApplyLocalization(AppLocalizer localizer)
        {
            _localizer = localizer;
            _expandableSection.Title = _entry.BuildHeaderSummary();
            _expandableSection.ExpandToolTip = _localizer.Get(TranslationKeys.CertificatesExpand);
            _expandableSection.CollapseToolTip = _localizer.Get(TranslationKeys.CertificatesCollapse);
            ToolTip.SetTip(_dragArea, _localizer.Get(TranslationKeys.CertificatesDragToReorder));
            UpdateCharacterCounters();
        }

        public void ClearDragVisual() => RootBorder.Opacity = 1;

        public void UpdateValidation(IReadOnlyList<FieldValidationError> errors, ValidationTouchTracker touchTracker)
        {
            _fieldRegistry.ApplyErrors(errors, _localizer, touchTracker);

            ValidationErrorBadgeFactory.Update(
                _errorBadgePanel,
                _errorBadgeTextBlock,
                errors.Count,
                !_expandableSection.IsExpanded,
                _localizer.Format(TranslationKeys.CertificatesValidationErrors, errors.Count),
                () => _expandableSection.IsExpanded = true);
        }

        private AutoCompleteBox CreateIssuerAutoComplete()
        {
            var autoComplete = new AutoCompleteBox
            {
                FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
                ItemsSource = IssuerSuggestions.All,
                MinimumPrefixLength = 0,
                MaxDropDownHeight = 200
            };
            autoComplete.TextChanged += OnFieldChanged;
            return autoComplete;
        }

        private void LoadFromEntry()
        {
            _isLoadingFromEntry = true;
            try
            {
                _nameTextBox.Text = _entry.Name;
                _issuerAutoComplete.Text = _entry.Issuer;
                _issueDatePicker.SelectedDate = MonthYearDateHelper.ToSelectedDate(_entry.IssueMonth, _entry.IssueYear);
                _expirationDatePicker.SelectedDate = MonthYearDateHelper.ToSelectedDate(_entry.ExpirationMonth, _entry.ExpirationYear);
                _credentialIdTextBox.Text = _entry.CredentialId;
                _credentialUrlTextBox.Text = _entry.CredentialUrl;
                _descriptionTextBox.Text = _entry.Description;
            }
            finally
            {
                _isLoadingFromEntry = false;
            }
        }

        private void SyncToEntry()
        {
            _entry.Name = _nameTextBox.Text ?? string.Empty;
            _entry.Issuer = _issuerAutoComplete.Text ?? string.Empty;
            (_entry.IssueMonth, _entry.IssueYear) = MonthYearDateHelper.FromSelectedDate(_issueDatePicker.SelectedDate);
            (_entry.ExpirationMonth, _entry.ExpirationYear) = MonthYearDateHelper.FromSelectedDate(_expirationDatePicker.SelectedDate);
            _entry.CredentialId = _credentialIdTextBox.Text ?? string.Empty;
            _entry.CredentialUrl = _credentialUrlTextBox.Text ?? string.Empty;
            _entry.Description = _descriptionTextBox.Text ?? string.Empty;
        }

        private void OnFieldChanged(object? sender, EventArgs e)
        {
            if (_isLoadingFromEntry)
            {
                return;
            }

            SyncToEntry();
            _expandableSection.Title = _entry.BuildHeaderSummary();
            UpdateCharacterCounters();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void OnDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e) => OnFieldChanged(sender, EventArgs.Empty);

        private void UpdateCharacterCounters()
        {
            _descriptionCounterTextBlock.Text =
                $"{(_descriptionTextBox.Text ?? string.Empty).Length} / {CertificatesSchema.DescriptionMaxLength}";
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

        private static TextBox CreateMultilineTextBox(EventHandler<RoutedEventArgs> onChanged)
        {
            var textBox = CreateTextBox(onChanged);
            textBox.Classes.Add(UiClasses.MultilineTextBox);
            return textBox;
        }

        private static TextBlock CreateCounterTextBlock()
        {
            var counter = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right };
            counter.Classes.Add(UiClasses.CounterText);
            return counter;
        }

        private void RegisterImportConfidenceField(string fieldName, TextBox textBox)
        {
            _importConfidenceFields[CertificatesFieldKeys.Build(_entry.Id, fieldName)] = textBox;
            textBox.TextChanged += (_, _) => textBox.Classes.Remove(UiClasses.ImportHint);
        }

    }
}
