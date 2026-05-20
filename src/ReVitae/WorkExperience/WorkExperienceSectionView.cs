using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Controls;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Ui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ReVitae.WorkExperience;

public sealed class WorkExperienceSectionView : UserControl
{
    private readonly ExpandableSection _section;
    private readonly StackPanel _contentPanel;
    private readonly StackPanel _entriesPanel;
    private readonly TextBlock _emptyHintTextBlock;
    private readonly Button _addButton;
    private readonly Button _sortButton;
    private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
    private readonly List<WorkExperienceEntry> _entries = [];
    private readonly Dictionary<string, WorkExperienceEntryCard> _cardsById = new(StringComparer.Ordinal);
    private string? _dragSourceEntryId;

    public WorkExperienceSectionView()
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

    public IReadOnlyList<WorkExperienceEntry> Entries => _entries;

    public void SetLocalizer(AppLocalizer localizer)
    {
        _localizer = localizer;
        _section.Title = _localizer.Get(TranslationKeys.WorkExperience);
        _section.ExpandToolTip = _localizer.Get(TranslationKeys.ExpandSection);
        _section.CollapseToolTip = _localizer.Get(TranslationKeys.CollapseSection);
        _emptyHintTextBlock.Text = _localizer.Get(TranslationKeys.WorkExperienceEmptyHint);
        _addButton.Content = _localizer.Get(TranslationKeys.WorkExperienceAdd);
        _sortButton.Content = _localizer.Get(TranslationKeys.WorkExperienceSortByDate);

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
                .Where(error => WorkExperienceFieldKeys.TryParseEntryId(error.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.UpdateValidation(errors);
        }
    }

    public void AddEntry(WorkExperienceEntry? entry = null, int? insertIndex = null)
    {
        var newEntry = entry ?? new WorkExperienceEntry();
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
            var card = new WorkExperienceEntryCard(entry, _localizer, index);
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
        var sorted = WorkExperienceSorter.SortByDateNewestFirst(_entries);
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

    private sealed class WorkExperienceEntryCard
    {
        private readonly WorkExperienceEntry _entry;
        private readonly int _index;
        private AppLocalizer _localizer;
        private readonly ExpandableSection _expandableSection;
        private readonly StackPanel _errorBadgePanel;
        private readonly TextBlock _errorBadgeTextBlock;
        private readonly TextBox _jobTitleTextBox;
        private readonly TextBox _companyTextBox;
        private readonly TextBox _locationTextBox;
        private readonly ComboBox _employmentTypeComboBox;
        private readonly ComboBox _startMonthComboBox;
        private readonly TextBox _startYearTextBox;
        private readonly ComboBox _endMonthComboBox;
        private readonly TextBox _endYearTextBox;
        private readonly CheckBox _currentlyWorkingCheckBox;
        private readonly TextBox _descriptionTextBox;
        private readonly TextBox _achievementsTextBox;
        private readonly TextBox _technologiesTextBox;
        private readonly TextBox _companyUrlTextBox;
        private readonly TextBlock _descriptionCounterTextBlock;
        private readonly TextBlock _achievementsCounterTextBlock;
        private readonly Dictionary<string, TextBlock> _errorTextBlocks = new(StringComparer.Ordinal);
        private readonly Border _dragArea;

        public WorkExperienceEntryCard(WorkExperienceEntry entry, AppLocalizer localizer, int index)
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

            _jobTitleTextBox = CreateTextBox(OnFieldChanged);
            _companyTextBox = CreateTextBox(OnFieldChanged);
            _locationTextBox = CreateTextBox(OnFieldChanged);
            _employmentTypeComboBox = CreateEmploymentTypeComboBox();
            _startMonthComboBox = CreateMonthComboBox(OnSelectionChanged);
            _endMonthComboBox = CreateMonthComboBox(OnSelectionChanged);
            _startYearTextBox = CreateTextBox(OnFieldChanged);
            _endYearTextBox = CreateTextBox(OnFieldChanged);
            _currentlyWorkingCheckBox = new CheckBox();
            _currentlyWorkingCheckBox.IsCheckedChanged += OnCurrentlyWorkingChanged;
            _descriptionTextBox = CreateMultilineTextBox(OnFieldChanged);
            _achievementsTextBox = CreateMultilineTextBox(OnFieldChanged);
            _technologiesTextBox = CreateTextBox(OnFieldChanged);
            _companyUrlTextBox = CreateTextBox(OnFieldChanged);
            _descriptionCounterTextBlock = CreateCounterTextBlock();
            _achievementsCounterTextBlock = CreateCounterTextBlock();

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
                    CreateField(_jobTitleTextBox, TranslationKeys.WorkExperienceJobTitle, WorkExperienceFieldKeys.JobTitle),
                    CreateField(_companyTextBox, TranslationKeys.WorkExperienceCompany, WorkExperienceFieldKeys.Company),
                    CreateField(_locationTextBox, TranslationKeys.WorkExperienceLocation, WorkExperienceFieldKeys.Location),
                    CreateField(_employmentTypeComboBox, TranslationKeys.WorkExperienceEmploymentType, WorkExperienceFieldKeys.EmploymentType),
                    CreateDateField(_startMonthComboBox, _startYearTextBox, TranslationKeys.WorkExperienceStartDate, WorkExperienceFieldKeys.StartMonth, WorkExperienceFieldKeys.StartYear),
                    _currentlyWorkingCheckBox,
                    CreateDateField(_endMonthComboBox, _endYearTextBox, TranslationKeys.WorkExperienceEndDate, WorkExperienceFieldKeys.EndMonth, WorkExperienceFieldKeys.EndYear),
                    CreateMultilineField(_descriptionTextBox, _descriptionCounterTextBlock, TranslationKeys.WorkExperienceDescription, WorkExperienceFieldKeys.Description),
                    CreateMultilineField(_achievementsTextBox, _achievementsCounterTextBlock, TranslationKeys.WorkExperienceAchievements, WorkExperienceFieldKeys.Achievements),
                    CreateField(_technologiesTextBox, TranslationKeys.WorkExperienceTechnologies, WorkExperienceFieldKeys.Technologies),
                    CreateField(_companyUrlTextBox, TranslationKeys.WorkExperienceCompanyUrl, WorkExperienceFieldKeys.CompanyUrl),
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

            duplicateButton.Content = _localizer.Get(TranslationKeys.WorkExperienceDuplicate);
            removeButton.Content = _localizer.Get(TranslationKeys.WorkExperienceRemove);

            LoadFromEntry();
            ApplyLocalization(_localizer);
            UpdateEndDateVisibility();
            UpdateCharacterCounters();
        }

        public event EventHandler? Changed;

        public event EventHandler<WorkExperienceEntry>? DuplicateRequested;

        public event EventHandler<WorkExperienceEntry>? RemoveRequested;

        public event EventHandler<string>? DragStarted;

        public event EventHandler? DragEnded;

        public event EventHandler<int>? DropTargetEntered;

        public Border RootBorder { get; }

        public void ApplyLocalization(AppLocalizer localizer)
        {
            _localizer = localizer;
            _expandableSection.Title = _entry.BuildHeaderSummary(_localizer.Get(TranslationKeys.WorkExperiencePresent));
            _expandableSection.ExpandToolTip = _localizer.Get(TranslationKeys.WorkExperienceExpand);
            _expandableSection.CollapseToolTip = _localizer.Get(TranslationKeys.WorkExperienceCollapse);
            ToolTip.SetTip(_dragArea, _localizer.Get(TranslationKeys.WorkExperienceDragToReorder));
            _currentlyWorkingCheckBox.Content = _localizer.Get(TranslationKeys.WorkExperienceCurrentlyWorking);
            RefreshEmploymentTypeItems();
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
                error.FieldKey.EndsWith("." + WorkExperienceFieldKeys.DateRange, StringComparison.Ordinal));
            if (dateRangeError is not null
                && _errorTextBlocks.TryGetValue(WorkExperienceFieldKeys.StartMonth, out var startErrorBlock))
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
                ? _localizer.Format(TranslationKeys.WorkExperienceValidationErrors, errorCount)
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

        private ComboBox CreateEmploymentTypeComboBox()
        {
            var comboBox = new ComboBox();
            comboBox.SelectionChanged += OnSelectionChanged;
            return comboBox;
        }

        private void RefreshEmploymentTypeItems()
        {
            var selected = _employmentTypeComboBox.SelectedItem as EmploymentType? ?? _entry.EmploymentType;
            _employmentTypeComboBox.ItemsSource = EmploymentTypeExtensions.SupportedValues
                .Select(type => new ComboBoxItem
                {
                    Content = _localizer.Get(type.ToTranslationKey()),
                    Tag = type
                })
                .ToArray();
            _employmentTypeComboBox.SelectedItem = _employmentTypeComboBox.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag is EmploymentType type && type == selected);
        }

        private void LoadFromEntry()
        {
            _jobTitleTextBox.Text = _entry.JobTitle;
            _companyTextBox.Text = _entry.Company;
            _locationTextBox.Text = _entry.Location;
            _startMonthComboBox.SelectedItem = _entry.StartMonth;
            _startYearTextBox.Text = _entry.StartYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            _endMonthComboBox.SelectedItem = _entry.EndMonth;
            _endYearTextBox.Text = _entry.EndYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            _currentlyWorkingCheckBox.IsChecked = _entry.IsCurrentlyWorking;
            _descriptionTextBox.Text = _entry.Description;
            _achievementsTextBox.Text = _entry.Achievements;
            _technologiesTextBox.Text = _entry.Technologies;
            _companyUrlTextBox.Text = _entry.CompanyUrl;
            RefreshEmploymentTypeItems();
        }

        private void SyncToEntry()
        {
            _entry.JobTitle = _jobTitleTextBox.Text ?? string.Empty;
            _entry.Company = _companyTextBox.Text ?? string.Empty;
            _entry.Location = _locationTextBox.Text ?? string.Empty;
            _entry.EmploymentType = _employmentTypeComboBox.SelectedItem is ComboBoxItem { Tag: EmploymentType employmentType }
                ? employmentType
                : EmploymentType.FullTime;
            _entry.StartMonth = _startMonthComboBox.SelectedItem as int?;
            _entry.StartYear = ParseYear(_startYearTextBox.Text);
            _entry.EndMonth = _endMonthComboBox.SelectedItem as int?;
            _entry.EndYear = ParseYear(_endYearTextBox.Text);
            _entry.IsCurrentlyWorking = _currentlyWorkingCheckBox.IsChecked == true;
            _entry.Description = _descriptionTextBox.Text ?? string.Empty;
            _entry.Achievements = _achievementsTextBox.Text ?? string.Empty;
            _entry.Technologies = _technologiesTextBox.Text ?? string.Empty;
            _entry.CompanyUrl = _companyUrlTextBox.Text ?? string.Empty;
        }

        private void OnFieldChanged(object? sender, RoutedEventArgs e)
        {
            SyncToEntry();
            _expandableSection.Title = _entry.BuildHeaderSummary(_localizer.Get(TranslationKeys.WorkExperiencePresent));
            UpdateEndDateVisibility();
            UpdateCharacterCounters();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => OnFieldChanged(sender, e);

        private void OnCurrentlyWorkingChanged(object? sender, RoutedEventArgs e) => OnFieldChanged(sender, e);

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
            var isCurrent = _currentlyWorkingCheckBox.IsChecked == true;
            _endMonthComboBox.IsEnabled = !isCurrent;
            _endYearTextBox.IsEnabled = !isCurrent;
        }

        private void UpdateCharacterCounters()
        {
            _descriptionCounterTextBlock.Text = $"{(_descriptionTextBox.Text ?? string.Empty).Length} / {WorkExperienceSchema.DescriptionMaxLength}";
            _achievementsCounterTextBlock.Text = $"{(_achievementsTextBox.Text ?? string.Empty).Length} / {WorkExperienceSchema.AchievementsMaxLength}";
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
