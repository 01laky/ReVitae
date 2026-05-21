using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Controls;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Ui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ReVitae.Projects;

public sealed class ProjectsSectionView : UserControl
{
    private readonly ExpandableSection _section;
    private readonly StackPanel _contentPanel;
    private readonly StackPanel _entriesPanel;
    private readonly TextBlock _emptyHintTextBlock;
    private readonly Button _addButton;
    private readonly Button _sortButton;
    private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
    private readonly List<ProjectEntry> _entries = [];
    private readonly Dictionary<string, ProjectEntryCard> _cardsById = new(StringComparer.Ordinal);
    private string? _dragSourceEntryId;
    private bool _suppressEntriesChanged;
    private int? _pendingDropIndex;
    private IPointer? _capturedPointer;

    public ProjectsSectionView()
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

    public IReadOnlyList<ProjectEntry> Entries => _entries;

    public void SetLocalizer(AppLocalizer localizer)
    {
        _localizer = localizer;
        _section.Title = _localizer.Get(TranslationKeys.Projects);
        _section.ExpandToolTip = _localizer.Get(TranslationKeys.ExpandSection);
        _section.CollapseToolTip = _localizer.Get(TranslationKeys.CollapseSection);
        _emptyHintTextBlock.Text = _localizer.Get(TranslationKeys.ProjectsEmptyHint);
        _addButton.Content = _localizer.Get(TranslationKeys.ProjectsAdd);
        _sortButton.Content = _localizer.Get(TranslationKeys.ProjectsSortByDate);

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
                .Where(error => ProjectsFieldKeys.TryParseEntryId(error.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.UpdateValidation(errors);
        }
    }

    public void ReplaceEntries(IReadOnlyList<ProjectEntry> entries, bool expandSection = true)
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
                .Where(confidence => ProjectsFieldKeys.TryParseEntryId(confidence.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.ApplyImportConfidence(entryConfidences);
        }
    }

    public void AddEntry(ProjectEntry? entry = null, int? insertIndex = null)
    {
        var newEntry = entry ?? new ProjectEntry();
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
            var card = new ProjectEntryCard(this, entry, _localizer);
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
        var sorted = ProjectSorter.SortByDateNewestFirst(_entries);
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

    private sealed class ProjectEntryCard
    {
        private readonly ProjectsSectionView _sectionView;
        private readonly ProjectEntry _entry;
        private AppLocalizer _localizer;
        private readonly ExpandableSection _expandableSection;
        private readonly StackPanel _errorBadgePanel;
        private readonly TextBlock _errorBadgeTextBlock;
        private readonly TextBox _nameTextBox;
        private readonly TextBox _roleTextBox;
        private readonly TextBox _organizationTextBox;
        private readonly ComboBox _startMonthComboBox;
        private readonly TextBox _startYearTextBox;
        private readonly ComboBox _endMonthComboBox;
        private readonly TextBox _endYearTextBox;
        private readonly CheckBox _currentlyActiveCheckBox;
        private readonly TextBox _projectUrlTextBox;
        private readonly AutoCompleteBox _technologyAutoComplete;
        private readonly TextBox _bulkTechnologiesTextBox;
        private readonly TextBlock _bulkCounterTextBlock;
        private readonly WrapPanel _technologiesPanel;
        private readonly TextBox _highlightsTextBox;
        private readonly TextBox _descriptionTextBox;
        private readonly TextBlock _highlightsCounterTextBlock;
        private readonly TextBlock _descriptionCounterTextBlock;
        private readonly Dictionary<string, TextBlock> _errorTextBlocks = new(StringComparer.Ordinal);
        private readonly Dictionary<string, TextBox> _importConfidenceFields = new(StringComparer.Ordinal);
        private readonly Border _dragArea;

        public ProjectEntryCard(ProjectsSectionView sectionView, ProjectEntry entry, AppLocalizer localizer)
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
            _roleTextBox = CreateTextBox(OnFieldChanged);
            _organizationTextBox = CreateTextBox(OnFieldChanged);
            _startMonthComboBox = CreateMonthComboBox(OnSelectionChanged);
            _startYearTextBox = CreateTextBox(OnFieldChanged);
            _endMonthComboBox = CreateMonthComboBox(OnSelectionChanged);
            _endYearTextBox = CreateTextBox(OnFieldChanged);
            _currentlyActiveCheckBox = new CheckBox();
            _currentlyActiveCheckBox.IsCheckedChanged += OnCurrentlyActiveChanged;
            _projectUrlTextBox = CreateTextBox(OnFieldChanged);
            _technologyAutoComplete = CreateTechnologyAutoComplete();
            _bulkTechnologiesTextBox = CreateMultilineTextBox(OnFieldChanged);
            _bulkCounterTextBlock = CreateCounterTextBlock();
            _technologiesPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                MinHeight = 48
            };
            _technologiesPanel.Classes.Add(UiClasses.SkillChipPanel);
            _highlightsTextBox = CreateMultilineTextBox(OnFieldChanged);
            _descriptionTextBox = CreateMultilineTextBox(OnFieldChanged);
            _highlightsCounterTextBlock = CreateCounterTextBlock();
            _descriptionCounterTextBlock = CreateCounterTextBlock();

            RegisterImportConfidenceField(ProjectsFieldKeys.Name, _nameTextBox);
            RegisterImportConfidenceField(ProjectsFieldKeys.Role, _roleTextBox);
            RegisterImportConfidenceField(ProjectsFieldKeys.Organization, _organizationTextBox);
            RegisterImportConfidenceField(ProjectsFieldKeys.StartYear, _startYearTextBox);
            RegisterImportConfidenceField(ProjectsFieldKeys.EndYear, _endYearTextBox);
            RegisterImportConfidenceField(ProjectsFieldKeys.ProjectUrl, _projectUrlTextBox);
            RegisterImportConfidenceField(ProjectsFieldKeys.BulkTechnologies, _bulkTechnologiesTextBox);
            RegisterImportConfidenceField(ProjectsFieldKeys.Highlights, _highlightsTextBox);
            RegisterImportConfidenceField(ProjectsFieldKeys.Description, _descriptionTextBox);

            var addTechnologyButton = new Button();
            addTechnologyButton.Classes.Add(UiClasses.SecondaryButton);
            addTechnologyButton.Click += (_, _) => AddTechnologyFromInput();

            var addFromListButton = new Button();
            addFromListButton.Classes.Add(UiClasses.SecondaryButton);
            addFromListButton.Click += (_, _) => AddTechnologiesFromBulkText();

            var duplicateButton = new Button();
            duplicateButton.Classes.Add(UiClasses.SecondaryButton);
            duplicateButton.Click += (_, _) => DuplicateRequested?.Invoke(this, _entry);
            var removeButton = new Button();
            removeButton.Classes.Add(UiClasses.SecondaryButton);
            removeButton.Click += (_, _) => RemoveRequested?.Invoke(this, _entry);

            var addTechnologyRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                ColumnSpacing = 8
            };
            addTechnologyRow.Children.Add(_technologyAutoComplete);
            Grid.SetColumn(addTechnologyButton, 1);
            addTechnologyRow.Children.Add(addTechnologyButton);

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
                    CreateField(_nameTextBox, TranslationKeys.ProjectsName, ProjectsFieldKeys.Name),
                    CreateField(_roleTextBox, TranslationKeys.ProjectsRole, ProjectsFieldKeys.Role),
                    CreateField(_organizationTextBox, TranslationKeys.ProjectsOrganization, ProjectsFieldKeys.Organization),
                    CreateDateField(
                        _startMonthComboBox,
                        _startYearTextBox,
                        TranslationKeys.ProjectsStartDate,
                        ProjectsFieldKeys.StartMonth,
                        ProjectsFieldKeys.StartYear),
                    _currentlyActiveCheckBox,
                    CreateDateField(
                        _endMonthComboBox,
                        _endYearTextBox,
                        TranslationKeys.ProjectsEndDate,
                        ProjectsFieldKeys.EndMonth,
                        ProjectsFieldKeys.EndYear),
                    CreateField(_projectUrlTextBox, TranslationKeys.ProjectsProjectUrl, ProjectsFieldKeys.ProjectUrl),
                    CreateField(addTechnologyRow, TranslationKeys.ProjectsTechnologyName, ProjectsFieldKeys.TechnologyName),
                    CreateField(_bulkTechnologiesTextBox, TranslationKeys.ProjectsBulkTechnologies, ProjectsFieldKeys.BulkTechnologies),
                    _bulkCounterTextBlock,
                    addFromListButton,
                    _technologiesPanel,
                    CreateMultilineField(
                        _highlightsTextBox,
                        _highlightsCounterTextBlock,
                        TranslationKeys.ProjectsHighlights,
                        ProjectsFieldKeys.Highlights),
                    CreateMultilineField(
                        _descriptionTextBox,
                        _descriptionCounterTextBlock,
                        TranslationKeys.ProjectsDescription,
                        ProjectsFieldKeys.Description),
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

            duplicateButton.Content = _localizer.Get(TranslationKeys.ProjectsDuplicate);
            removeButton.Content = _localizer.Get(TranslationKeys.ProjectsRemove);
            addTechnologyButton.Content = _localizer.Get(TranslationKeys.ProjectsAddTechnology);
            addFromListButton.Content = _localizer.Get(TranslationKeys.ProjectsAddFromList);

            LoadFromEntry();
            ApplyLocalization(_localizer);
            UpdateEndDateVisibility();
            RebuildTechnologyChips();
            UpdateCharacterCounters();
        }

        public event EventHandler? Changed;

        public event EventHandler<ProjectEntry>? DuplicateRequested;

        public event EventHandler<ProjectEntry>? RemoveRequested;

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
            _expandableSection.ExpandToolTip = _localizer.Get(TranslationKeys.ProjectsExpand);
            _expandableSection.CollapseToolTip = _localizer.Get(TranslationKeys.ProjectsCollapse);
            ToolTip.SetTip(_dragArea, _localizer.Get(TranslationKeys.ProjectsDragToReorder));
            _currentlyActiveCheckBox.Content = _localizer.Get(TranslationKeys.ProjectsCurrentlyActive);
            _bulkTechnologiesTextBox.PlaceholderText = _localizer.Get(TranslationKeys.ProjectsBulkTechnologiesPlaceholder);
            RebuildTechnologyChips();
            UpdateCharacterCounters();
        }

        public void ClearDragVisual() => RootBorder.Opacity = 1;

        public void UpdateValidation(IReadOnlyList<FieldValidationError> errors)
        {
            foreach (var (fieldName, textBlock) in _errorTextBlocks)
            {
                var fieldErrors = errors
                    .Where(error =>
                    {
                        if (ProjectsFieldKeys.TryParseTechnologyField(error.FieldKey, out _, out _, out var technologyField))
                        {
                            return technologyField == fieldName;
                        }

                        return error.FieldKey.EndsWith("." + fieldName, StringComparison.Ordinal);
                    })
                    .Select(error => _localizer.Get(error.Message))
                    .Distinct()
                    .ToArray();
                textBlock.Text = string.Join(Environment.NewLine, fieldErrors);
            }

            var dateRangeError = errors.FirstOrDefault(error =>
                error.FieldKey.EndsWith("." + ProjectsFieldKeys.DateRange, StringComparison.Ordinal));
            if (dateRangeError is not null
                && _errorTextBlocks.TryGetValue(ProjectsFieldKeys.StartMonth, out var startErrorBlock))
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
                ? _localizer.Format(TranslationKeys.ProjectsValidationErrors, errorCount)
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

        private AutoCompleteBox CreateTechnologyAutoComplete()
        {
            var autoComplete = new AutoCompleteBox
            {
                FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
                ItemsSource = SkillsSuggestions.All,
                MinimumPrefixLength = 0,
                MaxDropDownHeight = 200
            };
            autoComplete.TextChanged += OnFieldChanged;
            autoComplete.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    AddTechnologyFromInput();
                    e.Handled = true;
                }
            };
            return autoComplete;
        }

        private void LoadFromEntry()
        {
            _nameTextBox.Text = _entry.Name;
            _roleTextBox.Text = _entry.Role;
            _organizationTextBox.Text = _entry.Organization;
            _startMonthComboBox.SelectedItem = _entry.StartMonth;
            _startYearTextBox.Text = _entry.StartYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            _endMonthComboBox.SelectedItem = _entry.EndMonth;
            _endYearTextBox.Text = _entry.EndYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            _currentlyActiveCheckBox.IsChecked = _entry.IsCurrentlyActive;
            _projectUrlTextBox.Text = _entry.ProjectUrl;
            _highlightsTextBox.Text = _entry.Highlights;
            _descriptionTextBox.Text = _entry.Description;
        }

        private void SyncToEntry()
        {
            _entry.Name = _nameTextBox.Text ?? string.Empty;
            _entry.Role = _roleTextBox.Text ?? string.Empty;
            _entry.Organization = _organizationTextBox.Text ?? string.Empty;
            _entry.StartMonth = _startMonthComboBox.SelectedItem as int?;
            _entry.StartYear = ParseYear(_startYearTextBox.Text);
            _entry.EndMonth = _endMonthComboBox.SelectedItem as int?;
            _entry.EndYear = ParseYear(_endYearTextBox.Text);
            _entry.IsCurrentlyActive = _currentlyActiveCheckBox.IsChecked == true;
            _entry.ProjectUrl = _projectUrlTextBox.Text ?? string.Empty;
            _entry.Highlights = _highlightsTextBox.Text ?? string.Empty;
            _entry.Description = _descriptionTextBox.Text ?? string.Empty;
        }

        private void OnFieldChanged(object? sender, RoutedEventArgs e)
        {
            SyncToEntry();
            UpdateHeaderTitle();
            UpdateEndDateVisibility();
            UpdateCharacterCounters();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => OnFieldChanged(sender, e);

        private void OnCurrentlyActiveChanged(object? sender, RoutedEventArgs e) => OnFieldChanged(sender, e);

        private void AddTechnologyFromInput()
        {
            var name = _technologyAutoComplete.Text?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                return;
            }

            _entry.Technologies.Add(new ProjectTechnologyItem { Name = name });
            _technologyAutoComplete.Text = string.Empty;
            UpdateHeaderTitle();
            RebuildTechnologyChips();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void AddTechnologiesFromBulkText()
        {
            var text = _bulkTechnologiesTextBox.Text ?? string.Empty;
            if (text.Length > ProjectsSchema.BulkTechnologiesMaxLength)
            {
                return;
            }

            foreach (var name in ProjectTechnologiesParser.ParseTechnologyNames(text))
            {
                _entry.Technologies.Add(new ProjectTechnologyItem { Name = name });
            }

            _bulkTechnologiesTextBox.Text = string.Empty;
            UpdateHeaderTitle();
            RebuildTechnologyChips();
            UpdateCharacterCounters();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void RebuildTechnologyChips()
        {
            _technologiesPanel.Children.Clear();

            foreach (var technology in _entry.Technologies)
            {
                if (!technology.HasUserInput())
                {
                    continue;
                }

                _technologiesPanel.Children.Add(CreateTechnologyChip(technology));
            }
        }

        private Border CreateTechnologyChip(ProjectTechnologyItem technology)
        {
            var textBlock = new TextBlock
            {
                Text = technology.Name.Trim(),
                VerticalAlignment = VerticalAlignment.Center
            };

            var removeButton = new Button
            {
                Padding = new Thickness(4, 0),
                Content = MaterialIconFactory.Create(MaterialIconKind.Close, 14)
            };
            removeButton.Classes.Add(UiClasses.IconButton);
            ToolTip.SetTip(removeButton, _localizer.Get(TranslationKeys.ProjectsRemoveTechnology));
            removeButton.Click += (_, _) =>
            {
                _entry.Technologies.RemoveAll(item => item.Id == technology.Id);
                RebuildTechnologyChips();
                UpdateHeaderTitle();
                Changed?.Invoke(this, EventArgs.Empty);
            };

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { textBlock, removeButton }
            };

            var chip = new Border
            {
                Child = row,
                Margin = new Thickness(0, 0, 8, 8)
            };
            chip.Classes.Add(UiClasses.SkillChip);
            return chip;
        }

        private void UpdateHeaderTitle()
        {
            _expandableSection.Title = _entry.BuildHeaderSummary(_localizer.Get(TranslationKeys.ProjectsPresent));
        }

        private void UpdateEndDateVisibility()
        {
            var isActive = _currentlyActiveCheckBox.IsChecked == true;
            _endMonthComboBox.IsEnabled = !isActive;
            _endYearTextBox.IsEnabled = !isActive;
        }

        private void UpdateCharacterCounters()
        {
            _bulkCounterTextBlock.Text =
                $"{(_bulkTechnologiesTextBox.Text ?? string.Empty).Length} / {ProjectsSchema.BulkTechnologiesMaxLength}";
            _highlightsCounterTextBlock.Text =
                $"{(_highlightsTextBox.Text ?? string.Empty).Length} / {ProjectsSchema.HighlightsMaxLength}";
            _descriptionCounterTextBlock.Text =
                $"{(_descriptionTextBox.Text ?? string.Empty).Length} / {ProjectsSchema.DescriptionMaxLength}";
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
            _importConfidenceFields[ProjectsFieldKeys.Build(_entry.Id, fieldName)] = textBox;
            textBox.TextChanged += (_, _) => textBox.Classes.Remove(UiClasses.ImportHint);
        }

        private static int? ParseYear(string? text)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year) ? year : null;
        }
    }
}
