using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Controls;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Core.Validation.Presentation;
using ReVitae.Ui;
using ReVitae.Ui.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReVitae.Education;

public sealed class EducationSectionView : UserControl, IValidationNavigableSection
{
    private readonly ExpandableSection _section;
    private readonly StackPanel _contentPanel;
    private readonly StackPanel _entriesPanel;
    private readonly TextBlock _emptyHintTextBlock;
    private readonly Button _addButton;
    private readonly Button _sortButton;
    private readonly StackPanel _sectionErrorBadgePanel;
    private readonly TextBlock _sectionErrorBadgeTextBlock;
    private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
    private readonly List<EducationEntry> _entries = [];
    private readonly Dictionary<string, EducationEntryCard> _cardsById = new(StringComparer.Ordinal);
    private ValidationTouchTracker _touchTracker = new();
    private string? _dragSourceEntryId;
    private bool _suppressEntriesChanged;

    public EducationSectionView()
    {
        _entriesPanel = new StackPanel { Spacing = 12 };
        _emptyHintTextBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };
        _emptyHintTextBlock.Classes.Add(UiClasses.SecondaryText);

        _addButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _addButton.Classes.Add(UiClasses.PrimaryButton);
        _addButton.Click += (_, _) => AddEntry();

        _sortButton = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        _sortButton.Classes.Add(UiClasses.SecondaryButton);
        _sortButton.Click += OnSortByDateClicked;

        (_sectionErrorBadgePanel, _sectionErrorBadgeTextBlock) = ValidationErrorBadgeFactory.Create();

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
            IsExpanded = true,
            HeaderActions = _sectionErrorBadgePanel
        };

        Content = _section;
    }

    public event EventHandler? EntriesChanged;

    public IReadOnlyList<EducationEntry> Entries => _entries;

    public void SetLocalizer(AppLocalizer localizer)
    {
        _localizer = localizer;
        _section.Title = _localizer.Get(TranslationKeys.Education);
        _section.ExpandToolTip = _localizer.Get(TranslationKeys.ExpandSection);
        _section.CollapseToolTip = _localizer.Get(TranslationKeys.CollapseSection);
        _emptyHintTextBlock.Text = _localizer.Get(TranslationKeys.EducationEmptyHint);
        _addButton.Content = _localizer.Get(TranslationKeys.EducationAdd);
        _sortButton.Content = _localizer.Get(TranslationKeys.EducationSortByDate);

        foreach (var card in _cardsById.Values)
        {
            card.ApplyLocalization(_localizer);
        }
    }

    public void UpdateValidation(FieldValidationResult validationResult, ValidationTouchTracker touchTracker)
    {
        _touchTracker = touchTracker;

        var sectionErrors = validationResult.Errors
            .Where(error => EducationFieldKeys.TryParseEntryId(error.FieldKey, out _, out _))
            .ToArray();

        FormValidationService.UpdateSectionErrorBadge(
            _sectionErrorBadgePanel,
            _sectionErrorBadgeTextBlock,
            sectionErrors.Length,
            !_section.IsExpanded,
            _localizer,
            TranslationKeys.EducationValidationErrors,
            () => _section.IsExpanded = true);

        foreach (var (entryId, card) in _cardsById)
        {
            var errors = sectionErrors
                .Where(error => EducationFieldKeys.TryParseEntryId(error.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.UpdateValidation(errors, touchTracker);
        }
    }

    public IReadOnlyList<string> GetOrderedFieldKeys()
    {
        var keys = new List<string>();
        foreach (var entry in _entries)
        {
            var entryId = entry.Id;
            keys.Add(EducationFieldKeys.Build(entryId, EducationFieldKeys.Institution));
            keys.Add(EducationFieldKeys.Build(entryId, EducationFieldKeys.Degree));
            keys.Add(EducationFieldKeys.Build(entryId, EducationFieldKeys.FieldOfStudy));
            keys.Add(EducationFieldKeys.Build(entryId, EducationFieldKeys.Location));
            keys.Add(EducationFieldKeys.Build(entryId, EducationFieldKeys.DegreeType));
            keys.Add(EducationFieldKeys.Build(entryId, EducationFieldKeys.StartMonth));
            keys.Add(EducationFieldKeys.Build(entryId, EducationFieldKeys.StartYear));
            keys.Add(EducationFieldKeys.Build(entryId, EducationFieldKeys.DateRange));
            keys.Add(EducationFieldKeys.Build(entryId, EducationFieldKeys.EndMonth));
            keys.Add(EducationFieldKeys.Build(entryId, EducationFieldKeys.EndYear));
            keys.Add(EducationFieldKeys.Build(entryId, EducationFieldKeys.Grade));
            keys.Add(EducationFieldKeys.Build(entryId, EducationFieldKeys.Description));
            keys.Add(EducationFieldKeys.Build(entryId, EducationFieldKeys.InstitutionUrl));
        }

        return keys;
    }

    public bool ExpandAndRevealField(string fieldKey)
    {
        if (!EducationFieldKeys.TryParseEntryId(fieldKey, out var entryId, out _))
        {
            return false;
        }

        if (!_cardsById.TryGetValue(entryId, out var card))
        {
            return false;
        }

        _section.IsExpanded = true;
        card.SetExpanded(true);
        return true;
    }

    public Control? FindControlForFieldKey(string fieldKey)
    {
        if (!EducationFieldKeys.TryParseEntryId(fieldKey, out var entryId, out _))
        {
            return null;
        }

        return _cardsById.TryGetValue(entryId, out var card)
            ? card.FindControlForFieldKey(fieldKey)
            : null;
    }

    public void ReplaceEntries(IReadOnlyList<EducationEntry> entries, bool expandSection = true)
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
                .Where(confidence => EducationFieldKeys.TryParseEntryId(confidence.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.ApplyImportConfidence(entryConfidences);
        }
    }

    public void AddEntry(EducationEntry? entry = null, int? insertIndex = null)
    {
        var newEntry = entry ?? new EducationEntry();
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

    private void RebuildEntryCards()
    {
        _entriesPanel.Children.Clear();
        _cardsById.Clear();

        for (var index = 0; index < _entries.Count; index++)
        {
            var entry = _entries[index];
            var card = new EducationEntryCard(entry, _localizer, index, _touchTracker);
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
            card.DragStarted += (_, entryId) => _dragSourceEntryId = entryId;
            card.DragEnded += (_, _) =>
            {
                _dragSourceEntryId = null;
                card.RootBorder.Opacity = 1;
            };
            card.DropTargetEntered += (_, targetIndex) => MoveEntryToIndex(_dragSourceEntryId, targetIndex);

            _cardsById[entry.Id] = card;
            _entriesPanel.Children.Add(card.RootBorder);
        }

        _emptyHintTextBlock.IsVisible = _entries.Count == 0;
    }

    private void OnSortByDateClicked(object? sender, RoutedEventArgs e)
    {
        var sorted = EducationSorter.SortByDateNewestFirst(_entries);
        _entries.Clear();
        _entries.AddRange(sorted);
        RebuildEntryCards();
        NotifyEntriesChanged();
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

    private sealed class EducationEntryCard
    {
        private readonly EducationEntry _entry;
        private readonly int _index;
        private readonly ValidationFieldRegistry _validationRegistry = new();
        private AppLocalizer _localizer;
        private readonly ExpandableSection _expandableSection;
        private readonly StackPanel _errorBadgePanel;
        private readonly TextBlock _errorBadgeTextBlock;
        private readonly TextBox _institutionTextBox;
        private readonly TextBox _degreeTextBox;
        private readonly TextBox _fieldOfStudyTextBox;
        private readonly TextBox _locationTextBox;
        private readonly ComboBox _degreeTypeComboBox;
        private readonly DatePicker _startDatePicker;
        private readonly DatePicker _endDatePicker;
        private readonly CheckBox _currentlyStudyingCheckBox;
        private readonly TextBox _gradeTextBox;
        private readonly TextBox _descriptionTextBox;
        private readonly TextBox _institutionUrlTextBox;
        private readonly TextBlock _descriptionCounterTextBlock;
        private readonly Dictionary<string, TextBox> _importConfidenceFields = new(StringComparer.Ordinal);
        private readonly Border _dragArea;

        public EducationEntryCard(
            EducationEntry entry,
            AppLocalizer localizer,
            int index,
            ValidationTouchTracker touchTracker)
        {
            _entry = entry;
            _localizer = localizer;
            _index = index;

            (_errorBadgePanel, _errorBadgeTextBlock) = ValidationErrorBadgeFactory.Create();

            _dragArea = new Border();
            _dragArea.Classes.Add(UiClasses.DragHandle);
            var dragIcon = MaterialIconFactory.Create(MaterialIconKind.DragVertical, 18);
            dragIcon.HorizontalAlignment = HorizontalAlignment.Center;
            dragIcon.VerticalAlignment = VerticalAlignment.Center;
            _dragArea.Child = dragIcon;
            _dragArea.PointerPressed += OnDragAreaPointerPressed;
            _dragArea.PointerReleased += (_, _) => DragEnded?.Invoke(this, EventArgs.Empty);

            _institutionTextBox = CreateTextBox(OnFieldChanged);
            _degreeTextBox = CreateTextBox(OnFieldChanged);
            _fieldOfStudyTextBox = CreateTextBox(OnFieldChanged);
            _locationTextBox = CreateTextBox(OnFieldChanged);
            _degreeTypeComboBox = CreateDegreeTypeComboBox();
            _startDatePicker = MonthYearDateHelper.CreatePicker(OnDateChanged);
            _endDatePicker = MonthYearDateHelper.CreatePicker(OnDateChanged);
            _currentlyStudyingCheckBox = new CheckBox();
            _currentlyStudyingCheckBox.IsCheckedChanged += OnCurrentlyStudyingChanged;
            _gradeTextBox = CreateTextBox(OnFieldChanged);
            _descriptionTextBox = CreateMultilineTextBox(OnFieldChanged);
            _institutionUrlTextBox = CreateTextBox(OnFieldChanged);
            _descriptionCounterTextBlock = CreateCounterTextBlock();

            RegisterImportConfidenceField(EducationFieldKeys.Institution, _institutionTextBox);
            RegisterImportConfidenceField(EducationFieldKeys.Degree, _degreeTextBox);
            RegisterImportConfidenceField(EducationFieldKeys.FieldOfStudy, _fieldOfStudyTextBox);
            RegisterImportConfidenceField(EducationFieldKeys.Location, _locationTextBox);
            RegisterImportConfidenceField(EducationFieldKeys.Grade, _gradeTextBox);
            RegisterImportConfidenceField(EducationFieldKeys.Description, _descriptionTextBox);
            RegisterImportConfidenceField(EducationFieldKeys.InstitutionUrl, _institutionUrlTextBox);

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
                        _localizer.Get(TranslationKeys.EducationInstitution),
                        _institutionTextBox,
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.Institution),
                        _validationRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.EducationDegree),
                        _degreeTextBox,
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.Degree),
                        _validationRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.EducationFieldOfStudy),
                        _fieldOfStudyTextBox,
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.FieldOfStudy),
                        _validationRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.EducationLocation),
                        _locationTextBox,
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.Location),
                        _validationRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.EducationDegreeType),
                        _degreeTypeComboBox,
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.DegreeType),
                        _validationRegistry,
                        touchTracker),
                    ValidatedDateRangeBinding.CreatePanel(
                        _localizer.Get(TranslationKeys.EducationStartDate),
                        _startDatePicker,
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.StartMonth),
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.StartMonth),
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.StartYear),
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.DateRange),
                        _validationRegistry,
                        touchTracker),
                    _currentlyStudyingCheckBox,
                    ValidatedDateRangeBinding.CreatePanel(
                        _localizer.Get(TranslationKeys.EducationEndDate),
                        _endDatePicker,
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.EndMonth),
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.EndMonth),
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.EndYear),
                        EducationFieldKeys.Build(entryId, "_endDateRange"),
                        _validationRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.EducationGrade),
                        _gradeTextBox,
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.Grade),
                        _validationRegistry,
                        touchTracker),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.EducationDescription),
                        _descriptionTextBox,
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.Description),
                        _validationRegistry,
                        touchTracker,
                        _descriptionCounterTextBlock),
                    ValidationFieldRegistry.CreateFieldPanel(
                        _localizer.Get(TranslationKeys.EducationInstitutionUrl),
                        _institutionUrlTextBox,
                        EducationFieldKeys.Build(entryId, EducationFieldKeys.InstitutionUrl),
                        _validationRegistry,
                        touchTracker),
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

            RootBorder = new Border
            {
                Child = _expandableSection
            };
            RootBorder.Classes.Add(UiClasses.EntryCard);

            RootBorder.PointerEntered += (_, _) => DropTargetEntered?.Invoke(this, _index);

            duplicateButton.Content = _localizer.Get(TranslationKeys.EducationDuplicate);
            removeButton.Content = _localizer.Get(TranslationKeys.EducationRemove);

            LoadFromEntry();
            ApplyLocalization(_localizer);
            UpdateEndDateVisibility();
            UpdateCharacterCounters();
        }

        public event EventHandler? Changed;

        public event EventHandler<EducationEntry>? DuplicateRequested;

        public event EventHandler<EducationEntry>? RemoveRequested;

        public event EventHandler<string>? DragStarted;

        public event EventHandler? DragEnded;

        public event EventHandler<int>? DropTargetEntered;

        public Border RootBorder { get; }

        public void SetExpanded(bool isExpanded) => _expandableSection.IsExpanded = isExpanded;

        public Control? FindControlForFieldKey(string fieldKey) =>
            _validationRegistry.FindControlForFieldKey(fieldKey);

        public void ApplyImportConfidence(IReadOnlyList<ImportedFieldConfidence> confidences)
        {
            ImportConfidenceHelper.ApplyToFields(_importConfidenceFields, confidences);
        }

        public void ApplyLocalization(AppLocalizer localizer)
        {
            _localizer = localizer;
            _expandableSection.Title = _entry.BuildHeaderSummary(_localizer.Get(TranslationKeys.EducationPresent));
            _expandableSection.ExpandToolTip = _localizer.Get(TranslationKeys.EducationExpand);
            _expandableSection.CollapseToolTip = _localizer.Get(TranslationKeys.EducationCollapse);
            ToolTip.SetTip(_dragArea, _localizer.Get(TranslationKeys.EducationDragToReorder));
            _currentlyStudyingCheckBox.Content = _localizer.Get(TranslationKeys.EducationCurrentlyStudying);
            RefreshDegreeTypeItems();
            UpdateCharacterCounters();
        }

        public void UpdateValidation(IReadOnlyList<FieldValidationError> errors, ValidationTouchTracker touchTracker)
        {
            _validationRegistry.ApplyErrors(errors, _localizer, touchTracker);

            ValidationErrorBadgeFactory.Update(
                _errorBadgePanel,
                _errorBadgeTextBlock,
                errors.Count,
                !_expandableSection.IsExpanded,
                _localizer.Format(TranslationKeys.EducationValidationErrors, errors.Count),
                () => _expandableSection.IsExpanded = true);
        }

        private ComboBox CreateDegreeTypeComboBox()
        {
            var comboBox = new ComboBox();
            comboBox.SelectionChanged += OnSelectionChanged;
            return comboBox;
        }

        private void RefreshDegreeTypeItems()
        {
            var selected = _degreeTypeComboBox.SelectedItem as DegreeType? ?? _entry.DegreeType;
            _degreeTypeComboBox.ItemsSource = DegreeTypeExtensions.SupportedValues
                .Select(type => new ComboBoxItem
                {
                    Content = _localizer.Get(type.ToTranslationKey()),
                    Tag = type
                })
                .ToArray();
            _degreeTypeComboBox.SelectedItem = _degreeTypeComboBox.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag is DegreeType type && type == selected);
        }

        private void LoadFromEntry()
        {
            _institutionTextBox.Text = _entry.Institution;
            _degreeTextBox.Text = _entry.Degree;
            _fieldOfStudyTextBox.Text = _entry.FieldOfStudy;
            _locationTextBox.Text = _entry.Location;
            _startDatePicker.SelectedDate = MonthYearDateHelper.ToSelectedDate(_entry.StartMonth, _entry.StartYear);
            _endDatePicker.SelectedDate = MonthYearDateHelper.ToSelectedDate(_entry.EndMonth, _entry.EndYear);
            _currentlyStudyingCheckBox.IsChecked = _entry.IsCurrentlyStudying;
            _gradeTextBox.Text = _entry.Grade;
            _descriptionTextBox.Text = _entry.Description;
            _institutionUrlTextBox.Text = _entry.InstitutionUrl;
            RefreshDegreeTypeItems();
        }

        private void SyncToEntry()
        {
            _entry.Institution = _institutionTextBox.Text ?? string.Empty;
            _entry.Degree = _degreeTextBox.Text ?? string.Empty;
            _entry.FieldOfStudy = _fieldOfStudyTextBox.Text ?? string.Empty;
            _entry.Location = _locationTextBox.Text ?? string.Empty;
            _entry.DegreeType = _degreeTypeComboBox.SelectedItem is ComboBoxItem { Tag: DegreeType degreeType }
                ? degreeType
                : DegreeType.Bachelor;
            (_entry.StartMonth, _entry.StartYear) = MonthYearDateHelper.FromSelectedDate(_startDatePicker.SelectedDate);
            (_entry.EndMonth, _entry.EndYear) = MonthYearDateHelper.FromSelectedDate(_endDatePicker.SelectedDate);
            _entry.IsCurrentlyStudying = _currentlyStudyingCheckBox.IsChecked == true;
            _entry.Grade = _gradeTextBox.Text ?? string.Empty;
            _entry.Description = _descriptionTextBox.Text ?? string.Empty;
            _entry.InstitutionUrl = _institutionUrlTextBox.Text ?? string.Empty;
        }

        private void OnFieldChanged(object? sender, EventArgs e)
        {
            SyncToEntry();
            _expandableSection.Title = _entry.BuildHeaderSummary(_localizer.Get(TranslationKeys.EducationPresent));
            UpdateEndDateVisibility();
            UpdateCharacterCounters();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => OnFieldChanged(sender, e);

        private void OnDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e) => OnFieldChanged(sender, EventArgs.Empty);

        private void OnCurrentlyStudyingChanged(object? sender, RoutedEventArgs e) => OnFieldChanged(sender, e);

        private void OnDragAreaPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(_dragArea).Properties.IsLeftButtonPressed)
            {
                return;
            }

            RootBorder.Opacity = 0.75;
            DragStarted?.Invoke(this, _entry.Id);
            e.Pointer.Capture(_dragArea);
        }

        private void UpdateEndDateVisibility()
        {
            var isCurrent = _currentlyStudyingCheckBox.IsChecked == true;
            _endDatePicker.IsEnabled = !isCurrent;
        }

        private void UpdateCharacterCounters()
        {
            _descriptionCounterTextBlock.Text = $"{(_descriptionTextBox.Text ?? string.Empty).Length} / {EducationSchema.DescriptionMaxLength}";
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
            var counter = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Right
            };
            counter.Classes.Add(UiClasses.CounterText);
            return counter;
        }

        private void RegisterImportConfidenceField(string fieldName, TextBox textBox)
        {
            _importConfidenceFields[EducationFieldKeys.Build(_entry.Id, fieldName)] = textBox;
            textBox.TextChanged += (_, _) => textBox.Classes.Remove(UiClasses.ImportHint);
        }

    }
}
