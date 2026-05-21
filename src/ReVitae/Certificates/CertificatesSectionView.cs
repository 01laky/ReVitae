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
using ReVitae.Ui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ReVitae.Certificates;

public sealed class CertificatesSectionView : UserControl
{
    private readonly ExpandableSection _section;
    private readonly StackPanel _contentPanel;
    private readonly StackPanel _entriesPanel;
    private readonly TextBlock _emptyHintTextBlock;
    private readonly Button _addButton;
    private readonly Button _sortButton;
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

    public IReadOnlyList<CertificateEntry> Entries => _entries;

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

    public void UpdateValidation(FieldValidationResult validationResult)
    {
        foreach (var (entryId, card) in _cardsById)
        {
            var errors = validationResult.Errors
                .Where(error => CertificatesFieldKeys.TryParseEntryId(error.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.UpdateValidation(errors);
        }
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
            var card = new CertificateEntryCard(this, entry, _localizer);
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
        private AppLocalizer _localizer;
        private readonly ExpandableSection _expandableSection;
        private readonly StackPanel _errorBadgePanel;
        private readonly TextBlock _errorBadgeTextBlock;
        private readonly TextBox _nameTextBox;
        private readonly AutoCompleteBox _issuerAutoComplete;
        private readonly ComboBox _issueMonthComboBox;
        private readonly TextBox _issueYearTextBox;
        private readonly ComboBox _expirationMonthComboBox;
        private readonly TextBox _expirationYearTextBox;
        private readonly TextBox _credentialIdTextBox;
        private readonly TextBox _credentialUrlTextBox;
        private readonly TextBox _descriptionTextBox;
        private readonly TextBlock _descriptionCounterTextBlock;
        private readonly Dictionary<string, TextBlock> _errorTextBlocks = new(StringComparer.Ordinal);
        private readonly Dictionary<string, TextBox> _importConfidenceFields = new(StringComparer.Ordinal);
        private readonly Border _dragArea;

        public CertificateEntryCard(CertificatesSectionView sectionView, CertificateEntry entry, AppLocalizer localizer)
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

            _nameTextBox = CreateTextBox(OnFieldChanged);
            _issuerAutoComplete = CreateIssuerAutoComplete();
            _issueMonthComboBox = CreateMonthComboBox(OnSelectionChanged);
            _issueYearTextBox = CreateTextBox(OnFieldChanged);
            _expirationMonthComboBox = CreateMonthComboBox(OnSelectionChanged);
            _expirationYearTextBox = CreateTextBox(OnFieldChanged);
            _credentialIdTextBox = CreateTextBox(OnFieldChanged);
            _credentialUrlTextBox = CreateTextBox(OnFieldChanged);
            _descriptionTextBox = CreateMultilineTextBox(OnFieldChanged);
            _descriptionCounterTextBlock = CreateCounterTextBlock();

            RegisterImportConfidenceField(CertificatesFieldKeys.Name, _nameTextBox);
            RegisterImportConfidenceField(CertificatesFieldKeys.IssueYear, _issueYearTextBox);
            RegisterImportConfidenceField(CertificatesFieldKeys.ExpirationYear, _expirationYearTextBox);
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

            var body = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    CreateField(_nameTextBox, TranslationKeys.CertificatesName, CertificatesFieldKeys.Name),
                    CreateField(_issuerAutoComplete, TranslationKeys.CertificatesIssuer, CertificatesFieldKeys.Issuer),
                    CreateDateField(
                        _issueMonthComboBox,
                        _issueYearTextBox,
                        TranslationKeys.CertificatesIssueDate,
                        CertificatesFieldKeys.IssueMonth,
                        CertificatesFieldKeys.IssueYear),
                    CreateDateField(
                        _expirationMonthComboBox,
                        _expirationYearTextBox,
                        TranslationKeys.CertificatesExpirationDate,
                        CertificatesFieldKeys.ExpirationMonth,
                        CertificatesFieldKeys.ExpirationYear),
                    CreateField(_credentialIdTextBox, TranslationKeys.CertificatesCredentialId, CertificatesFieldKeys.CredentialId),
                    CreateField(_credentialUrlTextBox, TranslationKeys.CertificatesCredentialUrl, CertificatesFieldKeys.CredentialUrl),
                    CreateMultilineField(
                        _descriptionTextBox,
                        _descriptionCounterTextBlock,
                        TranslationKeys.CertificatesDescription,
                        CertificatesFieldKeys.Description),
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

            var dateRangeError = errors.FirstOrDefault(error =>
                error.FieldKey.EndsWith("." + CertificatesFieldKeys.DateRange, StringComparison.Ordinal));
            if (dateRangeError is not null
                && _errorTextBlocks.TryGetValue(CertificatesFieldKeys.IssueMonth, out var issueErrorBlock))
            {
                var combined = string.Join(
                    Environment.NewLine,
                    new[] { issueErrorBlock.Text, _localizer.Get(dateRangeError.Message) }
                        .Where(text => !string.IsNullOrWhiteSpace(text)));
                issueErrorBlock.Text = combined;
            }

            var errorCount = errors.Count;
            var showBadge = errorCount > 0 && !_expandableSection.IsExpanded;
            _errorBadgePanel.IsVisible = showBadge;
            _errorBadgeTextBlock.IsVisible = showBadge;
            _errorBadgeTextBlock.Text = showBadge
                ? _localizer.Format(TranslationKeys.CertificatesValidationErrors, errorCount)
                : string.Empty;
        }

        private StackPanel CreateField(Control input, string labelKey, string fieldName)
        {
            var label = new TextBlock { Text = _localizer.Get(labelKey) };
            var error = new TextBlock { TextWrapping = TextWrapping.Wrap };
            error.Classes.Add(UiClasses.ErrorText);
            _errorTextBlocks[fieldName] = error;

            var panel = new StackPanel
            {
                Spacing = 6,
                Children = { label, input, error }
            };
            panel.Classes.Add(UiClasses.FormField);
            return panel;
        }

        private StackPanel CreateDateField(
            ComboBox monthComboBox,
            TextBox yearTextBox,
            string labelKey,
            string monthFieldName,
            string yearFieldName)
        {
            var label = new TextBlock { Text = _localizer.Get(labelKey) };
            var monthError = new TextBlock { TextWrapping = TextWrapping.Wrap };
            monthError.Classes.Add(UiClasses.ErrorText);
            var yearError = new TextBlock { TextWrapping = TextWrapping.Wrap };
            yearError.Classes.Add(UiClasses.ErrorText);
            _errorTextBlocks[monthFieldName] = monthError;
            _errorTextBlocks[yearFieldName] = yearError;

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                ColumnSpacing = 8
            };
            row.Children.Add(monthComboBox);
            row.Children.Add(yearTextBox);
            Grid.SetColumn(yearTextBox, 1);

            var panel = new StackPanel
            {
                Spacing = 6,
                Children = { label, row, monthError, yearError }
            };
            panel.Classes.Add(UiClasses.FormField);
            return panel;
        }

        private StackPanel CreateMultilineField(TextBox textBox, TextBlock counter, string labelKey, string fieldName)
        {
            var panel = CreateField(textBox, labelKey, fieldName);
            panel.Children.Add(counter);
            return panel;
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
            _nameTextBox.Text = _entry.Name;
            _issuerAutoComplete.Text = _entry.Issuer;
            _issueMonthComboBox.SelectedItem = _entry.IssueMonth;
            _issueYearTextBox.Text = _entry.IssueYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            _expirationMonthComboBox.SelectedItem = _entry.ExpirationMonth;
            _expirationYearTextBox.Text = _entry.ExpirationYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            _credentialIdTextBox.Text = _entry.CredentialId;
            _credentialUrlTextBox.Text = _entry.CredentialUrl;
            _descriptionTextBox.Text = _entry.Description;
        }

        private void SyncToEntry()
        {
            _entry.Name = _nameTextBox.Text ?? string.Empty;
            _entry.Issuer = _issuerAutoComplete.Text ?? string.Empty;
            _entry.IssueMonth = _issueMonthComboBox.SelectedItem as int?;
            _entry.IssueYear = ParseYear(_issueYearTextBox.Text);
            _entry.ExpirationMonth = _expirationMonthComboBox.SelectedItem as int?;
            _entry.ExpirationYear = ParseYear(_expirationYearTextBox.Text);
            _entry.CredentialId = _credentialIdTextBox.Text ?? string.Empty;
            _entry.CredentialUrl = _credentialUrlTextBox.Text ?? string.Empty;
            _entry.Description = _descriptionTextBox.Text ?? string.Empty;
        }

        private void OnFieldChanged(object? sender, RoutedEventArgs e)
        {
            SyncToEntry();
            _expandableSection.Title = _entry.BuildHeaderSummary();
            UpdateCharacterCounters();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => OnFieldChanged(sender, e);

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

        private static ComboBox CreateMonthComboBox(EventHandler<SelectionChangedEventArgs> onChanged)
        {
            var comboBox = new ComboBox
            {
                ItemsSource = Enumerable.Range(1, 12).Cast<int?>().ToArray(),
                PlaceholderText = "MM"
            };
            comboBox.SelectionChanged += onChanged;
            return comboBox;
        }

        private void RegisterImportConfidenceField(string fieldName, TextBox textBox)
        {
            _importConfidenceFields[CertificatesFieldKeys.Build(_entry.Id, fieldName)] = textBox;
            textBox.TextChanged += (_, _) => textBox.Classes.Remove(UiClasses.ImportHint);
        }

        private static int? ParseYear(string? text)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year) ? year : null;
        }
    }
}
