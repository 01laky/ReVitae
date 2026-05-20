using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Controls;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Ui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ReVitae.Education;

public sealed class EducationSectionView : UserControl
{
    private readonly ExpandableSection _section;
    private readonly StackPanel _contentPanel;
    private readonly StackPanel _entriesPanel;
    private readonly TextBlock _emptyHintTextBlock;
    private readonly Button _addButton;
    private readonly Button _sortButton;
    private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
    private readonly List<EducationEntry> _entries = [];
    private readonly Dictionary<string, EducationEntryCard> _cardsById = new(StringComparer.Ordinal);
    private string? _dragSourceEntryId;

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

    public void UpdateValidation(FieldValidationResult validationResult)
    {
        foreach (var (entryId, card) in _cardsById)
        {
            var errors = validationResult.Errors
                .Where(error => EducationFieldKeys.TryParseEntryId(error.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.UpdateValidation(errors);
        }
    }

    public void AddEntry(EducationEntry? entry = null, int? insertIndex = null)
    {
        var newEntry = entry ?? new EducationEntry();
        var index = insertIndex ?? 0;
        index = Math.Clamp(index, 0, _entries.Count);
        _entries.Insert(index, newEntry);
        RebuildEntryCards();
        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RebuildEntryCards()
    {
        _entriesPanel.Children.Clear();
        _cardsById.Clear();

        for (var index = 0; index < _entries.Count; index++)
        {
            var entry = _entries[index];
            var card = new EducationEntryCard(entry, _localizer, index);
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
        EntriesChanged?.Invoke(this, EventArgs.Empty);
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

    private sealed class EducationEntryCard
    {
        private readonly EducationEntry _entry;
        private readonly int _index;
        private AppLocalizer _localizer;
        private readonly ExpandableSection _expandableSection;
        private readonly StackPanel _errorBadgePanel;
        private readonly TextBlock _errorBadgeTextBlock;
        private readonly TextBox _institutionTextBox;
        private readonly TextBox _degreeTextBox;
        private readonly TextBox _fieldOfStudyTextBox;
        private readonly TextBox _locationTextBox;
        private readonly ComboBox _degreeTypeComboBox;
        private readonly ComboBox _startMonthComboBox;
        private readonly TextBox _startYearTextBox;
        private readonly ComboBox _endMonthComboBox;
        private readonly TextBox _endYearTextBox;
        private readonly CheckBox _currentlyStudyingCheckBox;
        private readonly TextBox _gradeTextBox;
        private readonly TextBox _descriptionTextBox;
        private readonly TextBox _institutionUrlTextBox;
        private readonly TextBlock _descriptionCounterTextBlock;
        private readonly Dictionary<string, TextBlock> _errorTextBlocks = new(StringComparer.Ordinal);
        private readonly Border _dragArea;

        public EducationEntryCard(EducationEntry entry, AppLocalizer localizer, int index)
        {
            _entry = entry;
            _localizer = localizer;
            _index = index;

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
            _dragArea.PointerPressed += OnDragAreaPointerPressed;
            _dragArea.PointerReleased += (_, _) => DragEnded?.Invoke(this, EventArgs.Empty);

            _institutionTextBox = CreateTextBox(OnFieldChanged);
            _degreeTextBox = CreateTextBox(OnFieldChanged);
            _fieldOfStudyTextBox = CreateTextBox(OnFieldChanged);
            _locationTextBox = CreateTextBox(OnFieldChanged);
            _degreeTypeComboBox = CreateDegreeTypeComboBox();
            _startMonthComboBox = CreateMonthComboBox(OnSelectionChanged);
            _endMonthComboBox = CreateMonthComboBox(OnSelectionChanged);
            _startYearTextBox = CreateTextBox(OnFieldChanged);
            _endYearTextBox = CreateTextBox(OnFieldChanged);
            _currentlyStudyingCheckBox = new CheckBox();
            _currentlyStudyingCheckBox.IsCheckedChanged += OnCurrentlyStudyingChanged;
            _gradeTextBox = CreateTextBox(OnFieldChanged);
            _descriptionTextBox = CreateMultilineTextBox(OnFieldChanged);
            _institutionUrlTextBox = CreateTextBox(OnFieldChanged);
            _descriptionCounterTextBlock = CreateCounterTextBlock();

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
                    CreateField(_institutionTextBox, TranslationKeys.EducationInstitution, EducationFieldKeys.Institution),
                    CreateField(_degreeTextBox, TranslationKeys.EducationDegree, EducationFieldKeys.Degree),
                    CreateField(_fieldOfStudyTextBox, TranslationKeys.EducationFieldOfStudy, EducationFieldKeys.FieldOfStudy),
                    CreateField(_locationTextBox, TranslationKeys.EducationLocation, EducationFieldKeys.Location),
                    CreateField(_degreeTypeComboBox, TranslationKeys.EducationDegreeType, EducationFieldKeys.DegreeType),
                    CreateDateField(_startMonthComboBox, _startYearTextBox, TranslationKeys.EducationStartDate, EducationFieldKeys.StartMonth, EducationFieldKeys.StartYear),
                    _currentlyStudyingCheckBox,
                    CreateDateField(_endMonthComboBox, _endYearTextBox, TranslationKeys.EducationEndDate, EducationFieldKeys.EndMonth, EducationFieldKeys.EndYear),
                    CreateField(_gradeTextBox, TranslationKeys.EducationGrade, EducationFieldKeys.Grade),
                    CreateMultilineField(_descriptionTextBox, _descriptionCounterTextBlock, TranslationKeys.EducationDescription, EducationFieldKeys.Description),
                    CreateField(_institutionUrlTextBox, TranslationKeys.EducationInstitutionUrl, EducationFieldKeys.InstitutionUrl),
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

        public void UpdateValidation(IReadOnlyList<FieldValidationError> errors)
        {
            foreach (var (fieldName, textBlock) in _errorTextBlocks)
            {
                var fieldErrors = errors
                    .Where(error => error.FieldKey.EndsWith("." + fieldName, StringComparison.Ordinal))
                    .Select(error => _localizer.Get(error.Message))
                    .ToArray();
                textBlock.Text = string.Join(Environment.NewLine, fieldErrors);
            }

            var dateRangeError = errors.FirstOrDefault(error =>
                error.FieldKey.EndsWith("." + EducationFieldKeys.DateRange, StringComparison.Ordinal));
            if (dateRangeError is not null
                && _errorTextBlocks.TryGetValue(EducationFieldKeys.StartMonth, out var startErrorBlock))
            {
                var combined = string.Join(
                    Environment.NewLine,
                    new[] { startErrorBlock.Text, _localizer.Get(dateRangeError.Message) }
                        .Where(text => !string.IsNullOrWhiteSpace(text)));
                startErrorBlock.Text = combined;
            }

            var errorCount = errors.Count;
            var showBadge = errorCount > 0 && !_expandableSection.IsExpanded;
            _errorBadgePanel.IsVisible = showBadge;
            _errorBadgeTextBlock.IsVisible = showBadge;
            _errorBadgeTextBlock.Text = showBadge
                ? _localizer.Format(TranslationKeys.EducationValidationErrors, errorCount)
                : string.Empty;
        }

        private StackPanel CreateField(Control input, string labelKey, string fieldName)
        {
            var label = new TextBlock { Text = _localizer.Get(labelKey) };
            var error = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap
            };
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
            _startMonthComboBox.SelectedItem = _entry.StartMonth;
            _startYearTextBox.Text = _entry.StartYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            _endMonthComboBox.SelectedItem = _entry.EndMonth;
            _endYearTextBox.Text = _entry.EndYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
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
            _entry.StartMonth = _startMonthComboBox.SelectedItem as int?;
            _entry.StartYear = ParseYear(_startYearTextBox.Text);
            _entry.EndMonth = _endMonthComboBox.SelectedItem as int?;
            _entry.EndYear = ParseYear(_endYearTextBox.Text);
            _entry.IsCurrentlyStudying = _currentlyStudyingCheckBox.IsChecked == true;
            _entry.Grade = _gradeTextBox.Text ?? string.Empty;
            _entry.Description = _descriptionTextBox.Text ?? string.Empty;
            _entry.InstitutionUrl = _institutionUrlTextBox.Text ?? string.Empty;
        }

        private void OnFieldChanged(object? sender, RoutedEventArgs e)
        {
            SyncToEntry();
            _expandableSection.Title = _entry.BuildHeaderSummary(_localizer.Get(TranslationKeys.EducationPresent));
            UpdateEndDateVisibility();
            UpdateCharacterCounters();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => OnFieldChanged(sender, e);

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
            _endMonthComboBox.IsEnabled = !isCurrent;
            _endYearTextBox.IsEnabled = !isCurrent;
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

        private static int? ParseYear(string? text)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year) ? year : null;
        }
    }
}
