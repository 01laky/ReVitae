using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Controls;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Ui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ReVitae.Skills;

public sealed class SkillsSectionView : UserControl
{
    private readonly ExpandableSection _section;
    private readonly StackPanel _contentPanel;
    private readonly StackPanel _entriesPanel;
    private readonly TextBlock _emptyHintTextBlock;
    private readonly Button _addButton;
    private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
    private readonly List<SkillsGroupEntry> _entries = [];
    private readonly Dictionary<string, SkillsGroupCard> _cardsById = new(StringComparer.Ordinal);
    private string? _dragSourceGroupId;
    private string? _dragSourceSkillGroupId;
    private string? _dragSourceSkillId;
    private int? _pendingGroupDropIndex;
    private string? _pendingSkillDropGroupId;
    private int? _pendingSkillDropIndex;
    private IPointer? _capturedPointer;

    public SkillsSectionView()
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

        _contentPanel = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                _emptyHintTextBlock,
                _addButton,
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

    public IReadOnlyList<SkillsGroupEntry> Entries => _entries;

    public void SetLocalizer(AppLocalizer localizer)
    {
        _localizer = localizer;
        _section.Title = _localizer.Get(TranslationKeys.Skills);
        _section.ExpandToolTip = _localizer.Get(TranslationKeys.ExpandSection);
        _section.CollapseToolTip = _localizer.Get(TranslationKeys.CollapseSection);
        _emptyHintTextBlock.Text = _localizer.Get(TranslationKeys.SkillsEmptyHint);
        _addButton.Content = _localizer.Get(TranslationKeys.SkillsAdd);

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
                .Where(error => SkillsFieldKeys.TryParseGroupId(error.FieldKey, out var parsedId, out _)
                    && parsedId == entryId)
                .ToArray();
            card.UpdateValidation(errors);
        }
    }

    public void AddEntry(SkillsGroupEntry? entry = null, int? insertIndex = null)
    {
        var newEntry = entry ?? new SkillsGroupEntry();
        var index = insertIndex ?? 0;
        index = Math.Clamp(index, 0, _entries.Count);
        _entries.Insert(index, newEntry);
        RebuildEntryCards();
        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void BeginSkillDrag(string groupId, string skillId)
    {
        _dragSourceSkillGroupId = groupId;
        _dragSourceSkillId = skillId;
        _pendingSkillDropGroupId = groupId;
        _pendingSkillDropIndex = null;
    }

    internal void BeginGroupDrag(string groupId)
    {
        _dragSourceGroupId = groupId;
        _pendingGroupDropIndex = FindGroupIndexById(groupId);
    }

    internal void CaptureDragPointer(IPointer pointer)
    {
        _capturedPointer = pointer;
        pointer.Capture(_entriesPanel);
    }

    internal void EndSkillDrag()
    {
        _dragSourceSkillGroupId = null;
        _dragSourceSkillId = null;
        _pendingSkillDropGroupId = null;
        _pendingSkillDropIndex = null;
        foreach (var card in _cardsById.Values)
        {
            card.ClearSkillDragVisuals();
            card.ClearGroupDragVisual();
        }
    }

    private void EndGroupDrag()
    {
        _dragSourceGroupId = null;
        _pendingGroupDropIndex = null;
        foreach (var card in _cardsById.Values)
        {
            card.ClearGroupDragVisual();
        }
    }

    private void EndAllDrags()
    {
        EndSkillDrag();
        EndGroupDrag();
        _capturedPointer?.Capture(null);
        _capturedPointer = null;
    }

    internal void MoveSkillToGroup(string targetGroupId, int? targetSkillIndex)
    {
        if (string.IsNullOrWhiteSpace(_dragSourceSkillGroupId)
            || string.IsNullOrWhiteSpace(_dragSourceSkillId))
        {
            return;
        }

        var sourceGroup = _entries.FirstOrDefault(entry => entry.Id == _dragSourceSkillGroupId);
        var targetGroup = _entries.FirstOrDefault(entry => entry.Id == targetGroupId);
        var skill = sourceGroup?.Skills.FirstOrDefault(item => item.Id == _dragSourceSkillId);
        if (sourceGroup is null || targetGroup is null || skill is null)
        {
            return;
        }

        var sourceSkillIndex = sourceGroup.Skills.FindIndex(item => item.Id == _dragSourceSkillId);
        sourceGroup.Skills.Remove(skill);
        var insertIndex = targetSkillIndex ?? targetGroup.Skills.Count;
        if (ReferenceEquals(sourceGroup, targetGroup) && insertIndex > sourceSkillIndex)
        {
            insertIndex--;
        }

        insertIndex = Math.Clamp(insertIndex, 0, targetGroup.Skills.Count);
        targetGroup.Skills.Insert(insertIndex, skill);

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
            var card = new SkillsGroupCard(this, entry, _localizer);
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

    private void MoveGroupToIndex(string? sourceGroupId, int targetIndex)
    {
        if (string.IsNullOrWhiteSpace(sourceGroupId))
        {
            return;
        }

        var sourceIndex = _entries.FindIndex(entry => entry.Id == sourceGroupId);
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

    private void OnEntriesPanelPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragSourceGroupId is not null)
        {
            _pendingGroupDropIndex = FindGroupDropIndex(e.GetPosition(_entriesPanel));
            return;
        }

        if (_dragSourceSkillId is not null)
        {
            var target = FindSkillDropTarget(e.GetPosition(_entriesPanel));
            _pendingSkillDropGroupId = target.GroupId;
            _pendingSkillDropIndex = target.SkillIndex;
        }
    }

    private void OnEntriesPanelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragSourceGroupId is not null && _pendingGroupDropIndex is int groupTargetIndex)
        {
            MoveGroupToIndex(_dragSourceGroupId, groupTargetIndex);
        }
        else if (_dragSourceSkillGroupId is not null
            && _dragSourceSkillId is not null
            && _pendingSkillDropGroupId is not null)
        {
            MoveSkillToGroup(_pendingSkillDropGroupId, _pendingSkillDropIndex);
        }

        EndAllDrags();
    }

    private int? FindGroupDropIndex(Point position)
    {
        for (var index = 0; index < _entriesPanel.Children.Count; index++)
        {
            if (_entriesPanel.Children[index] is not Visual visual)
            {
                continue;
            }

            if (GetBoundsRelativeTo(visual, _entriesPanel).Contains(position))
            {
                return index;
            }
        }

        return null;
    }

    private int? FindGroupIndexById(string groupId)
    {
        for (var index = 0; index < _entries.Count; index++)
        {
            if (_entries[index].Id == groupId)
            {
                return index;
            }
        }

        return null;
    }

    private (string? GroupId, int? SkillIndex) FindSkillDropTarget(Point position)
    {
        foreach (var (groupId, card) in _cardsById)
        {
            var panelBounds = GetBoundsRelativeTo(card.SkillsDropPanel, _entriesPanel);
            if (!panelBounds.Contains(position))
            {
                continue;
            }

            for (var index = 0; index < card.SkillsDropPanel.Children.Count; index++)
            {
                if (card.SkillsDropPanel.Children[index] is not Visual chipVisual)
                {
                    continue;
                }

                if (GetBoundsRelativeTo(chipVisual, _entriesPanel).Contains(position)
                    && card.TryGetSkillIdFromChip(chipVisual, out var skillId)
                    && skillId != _dragSourceSkillId)
                {
                    var group = _entries.First(entry => entry.Id == groupId);
                    var skillIndex = group.Skills.FindIndex(skill => skill.Id == skillId);
                    return (groupId, skillIndex >= 0 ? skillIndex : null);
                }
            }

            var groupEntry = _entries.First(entry => entry.Id == groupId);
            return (groupId, groupEntry.Skills.Count);
        }

        return (null, null);
    }

    private static Rect GetBoundsRelativeTo(Visual visual, Visual relativeTo)
    {
        var topLeft = visual.TranslatePoint(new Point(0, 0), relativeTo);
        if (topLeft is null)
        {
            return default;
        }

        return new Rect(topLeft.Value, visual.Bounds.Size);
    }

    private sealed class SkillsGroupCard
    {
        private readonly SkillsSectionView _sectionView;
        private readonly SkillsGroupEntry _entry;
        private AppLocalizer _localizer;
        private readonly ExpandableSection _expandableSection;
        private readonly StackPanel _errorBadgePanel;
        private readonly TextBlock _errorBadgeTextBlock;
        private readonly TextBox _categoryTextBox;
        private readonly AutoCompleteBox _skillNameAutoComplete;
        private readonly ComboBox _proficiencyComboBox;
        private readonly TextBox _yearsTextBox;
        private readonly TextBox _bulkSkillsTextBox;
        private readonly TextBlock _bulkCounterTextBlock;
        private readonly WrapPanel _skillsPanel;
        private readonly Dictionary<string, TextBlock> _errorTextBlocks = new(StringComparer.Ordinal);
        private readonly Dictionary<Visual, string> _skillIdsByChip = new();
        private readonly Border _dragArea;

        public SkillsGroupCard(SkillsSectionView sectionView, SkillsGroupEntry entry, AppLocalizer localizer)
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
            _dragArea.PointerPressed += OnGroupDragPointerPressed;

            _categoryTextBox = CreateTextBox(OnFieldChanged);
            _skillNameAutoComplete = CreateSkillAutoComplete();
            _proficiencyComboBox = CreateProficiencyComboBox();
            _yearsTextBox = CreateTextBox(OnFieldChanged);
            _bulkSkillsTextBox = CreateMultilineTextBox(OnFieldChanged);
            _bulkCounterTextBlock = CreateCounterTextBlock();
            _skillsPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                MinHeight = 48
            };
            _skillsPanel.Classes.Add(UiClasses.SkillChipPanel);

            var addSkillButton = new Button();
            addSkillButton.Classes.Add(UiClasses.SecondaryButton);
            addSkillButton.Click += (_, _) => AddSkillFromInputs();

            var addFromListButton = new Button();
            addFromListButton.Classes.Add(UiClasses.SecondaryButton);
            addFromListButton.Click += (_, _) => AddSkillsFromBulkText();

            var duplicateButton = new Button();
            duplicateButton.Classes.Add(UiClasses.SecondaryButton);
            duplicateButton.Click += (_, _) => DuplicateRequested?.Invoke(this, _entry);
            var removeButton = new Button();
            removeButton.Classes.Add(UiClasses.SecondaryButton);
            removeButton.Click += (_, _) => RemoveRequested?.Invoke(this, _entry);

            var addSkillRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto"),
                ColumnSpacing = 8
            };
            addSkillRow.Children.Add(_skillNameAutoComplete);
            Grid.SetColumn(_proficiencyComboBox, 1);
            addSkillRow.Children.Add(_proficiencyComboBox);
            Grid.SetColumn(_yearsTextBox, 2);
            addSkillRow.Children.Add(_yearsTextBox);
            Grid.SetColumn(addSkillButton, 3);
            addSkillRow.Children.Add(addSkillButton);

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
                    CreateField(_categoryTextBox, TranslationKeys.SkillsCategory, SkillsFieldKeys.Category),
                    CreateField(addSkillRow, TranslationKeys.SkillsSkillName, SkillsFieldKeys.SkillName),
                    CreateField(_bulkSkillsTextBox, TranslationKeys.SkillsBulkSkills, SkillsFieldKeys.SkillsCollection),
                    _bulkCounterTextBlock,
                    addFromListButton,
                    _skillsPanel,
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

            duplicateButton.Content = _localizer.Get(TranslationKeys.SkillsDuplicate);
            removeButton.Content = _localizer.Get(TranslationKeys.SkillsRemove);
            addSkillButton.Content = _localizer.Get(TranslationKeys.SkillsAddSkill);
            addFromListButton.Content = _localizer.Get(TranslationKeys.SkillsAddFromList);

            LoadFromEntry();
            ApplyLocalization(_localizer);
            RebuildSkillChips();
            UpdateBulkCounter();
        }

        public event EventHandler? Changed;

        public event EventHandler<SkillsGroupEntry>? DuplicateRequested;

        public event EventHandler<SkillsGroupEntry>? RemoveRequested;

        public Border RootBorder { get; }

        public WrapPanel SkillsDropPanel => _skillsPanel;

        public bool TryGetSkillIdFromChip(Visual chipVisual, out string skillId)
        {
            return _skillIdsByChip.TryGetValue(chipVisual, out skillId!);
        }

        public void ClearGroupDragVisual()
        {
            RootBorder.Opacity = 1;
        }

        public void ApplyLocalization(AppLocalizer localizer)
        {
            _localizer = localizer;
            _expandableSection.Title = _entry.BuildHeaderSummary();
            _expandableSection.ExpandToolTip = _localizer.Get(TranslationKeys.SkillsExpand);
            _expandableSection.CollapseToolTip = _localizer.Get(TranslationKeys.SkillsCollapse);
            ToolTip.SetTip(_dragArea, _localizer.Get(TranslationKeys.SkillsDragToReorder));
            _bulkSkillsTextBox.PlaceholderText = _localizer.Get(TranslationKeys.SkillsBulkSkillsPlaceholder);
            RefreshProficiencyItems();
            RebuildSkillChips();
            UpdateBulkCounter();
        }

        public void ClearSkillDragVisuals()
        {
            foreach (var child in _skillsPanel.Children.OfType<Border>())
            {
                child.Opacity = 1;
            }
        }

        public void UpdateValidation(IReadOnlyList<FieldValidationError> errors)
        {
            foreach (var (fieldName, textBlock) in _errorTextBlocks)
            {
                var fieldErrors = errors
                    .Where(error =>
                    {
                        if (!SkillsFieldKeys.TryParseSkillField(error.FieldKey, out _, out _, out var skillField))
                        {
                            return error.FieldKey.EndsWith("." + fieldName, StringComparison.Ordinal);
                        }

                        return skillField == fieldName;
                    })
                    .Select(error => _localizer.Get(error.Message))
                    .Distinct()
                    .ToArray();
                textBlock.Text = string.Join(Environment.NewLine, fieldErrors);
            }

            var collectionError = errors.FirstOrDefault(error =>
                error.FieldKey.EndsWith("." + SkillsFieldKeys.SkillsCollection, StringComparison.Ordinal));
            if (collectionError is not null
                && _errorTextBlocks.TryGetValue(SkillsFieldKeys.SkillsCollection, out var collectionErrorBlock))
            {
                collectionErrorBlock.Text = _localizer.Get(collectionError.Message);
            }

            var errorCount = errors.Count;
            var showBadge = errorCount > 0 && !_expandableSection.IsExpanded;
            _errorBadgePanel.IsVisible = showBadge;
            _errorBadgeTextBlock.IsVisible = showBadge;
            _errorBadgeTextBlock.Text = showBadge
                ? _localizer.Format(TranslationKeys.SkillsValidationErrors, errorCount)
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

        private AutoCompleteBox CreateSkillAutoComplete()
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
                    AddSkillFromInputs();
                    e.Handled = true;
                }
            };
            return autoComplete;
        }

        private ComboBox CreateProficiencyComboBox()
        {
            var comboBox = new ComboBox { MinWidth = 140 };
            comboBox.SelectionChanged += OnFieldChanged;
            return comboBox;
        }

        private void RefreshProficiencyItems()
        {
            var selected = _proficiencyComboBox.SelectedItem as ProficiencyLevel? ?? ProficiencyLevel.Intermediate;
            _proficiencyComboBox.ItemsSource = ProficiencyLevelExtensions.SupportedValues
                .Select(level => new ComboBoxItem
                {
                    Content = _localizer.Get(level.ToTranslationKey()),
                    Tag = level
                })
                .ToArray();
            _proficiencyComboBox.SelectedItem = _proficiencyComboBox.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag is ProficiencyLevel level && level == selected);
        }

        private void LoadFromEntry()
        {
            _categoryTextBox.Text = _entry.Category;
            RefreshProficiencyItems();
        }

        private void SyncCategoryToEntry()
        {
            _entry.Category = _categoryTextBox.Text ?? string.Empty;
        }

        private void OnFieldChanged(object? sender, RoutedEventArgs e)
        {
            SyncCategoryToEntry();
            _expandableSection.Title = _entry.BuildHeaderSummary();
            UpdateBulkCounter();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void AddSkillFromInputs()
        {
            var name = _skillNameAutoComplete.Text?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                return;
            }

            var proficiency = _proficiencyComboBox.SelectedItem is ComboBoxItem { Tag: ProficiencyLevel level }
                ? level
                : ProficiencyLevel.Intermediate;

            _entry.Skills.Add(new SkillItem
            {
                Name = name,
                Proficiency = proficiency,
                YearsOfExperience = ParseYears(_yearsTextBox.Text)
            });

            _skillNameAutoComplete.Text = string.Empty;
            _yearsTextBox.Text = string.Empty;
            _expandableSection.Title = _entry.BuildHeaderSummary();
            RebuildSkillChips();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void AddSkillsFromBulkText()
        {
            var text = _bulkSkillsTextBox.Text ?? string.Empty;
            if (text.Length > SkillsSchema.BulkSkillsMaxLength)
            {
                return;
            }

            var proficiency = _proficiencyComboBox.SelectedItem is ComboBoxItem { Tag: ProficiencyLevel level }
                ? level
                : ProficiencyLevel.Intermediate;
            var years = ParseYears(_yearsTextBox.Text);

            foreach (var name in SkillsTextParser.ParseSkillNames(text))
            {
                _entry.Skills.Add(new SkillItem
                {
                    Name = name,
                    Proficiency = proficiency,
                    YearsOfExperience = years
                });
            }

            _bulkSkillsTextBox.Text = string.Empty;
            _expandableSection.Title = _entry.BuildHeaderSummary();
            RebuildSkillChips();
            UpdateBulkCounter();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void RebuildSkillChips()
        {
            _skillsPanel.Children.Clear();
            _skillIdsByChip.Clear();

            for (var index = 0; index < _entry.Skills.Count; index++)
            {
                var skill = _entry.Skills[index];
                if (!skill.HasUserInput())
                {
                    continue;
                }

                var chip = CreateSkillChip(skill);
                _skillIdsByChip[chip] = skill.Id;
                _skillsPanel.Children.Add(chip);
            }
        }

        private Border CreateSkillChip(SkillItem skill)
        {
            var label = BuildSkillChipLabel(skill);
            var textBlock = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center
            };

            var dragHandle = new Border { Padding = new Thickness(4, 0) };
            dragHandle.Classes.Add(UiClasses.DragHandle);
            dragHandle.Child = MaterialIconFactory.Create(MaterialIconKind.DragVertical, 14);
            ToolTip.SetTip(dragHandle, _localizer.Get(TranslationKeys.SkillsDragSkillToMove));

            var removeButton = new Button
            {
                Padding = new Thickness(4, 0),
                Content = MaterialIconFactory.Create(MaterialIconKind.Close, 14)
            };
            removeButton.Classes.Add(UiClasses.IconButton);
            ToolTip.SetTip(removeButton, _localizer.Get(TranslationKeys.SkillsRemoveSkill));
            removeButton.Click += (_, _) =>
            {
                _entry.Skills.RemoveAll(item => item.Id == skill.Id);
                RebuildSkillChips();
                _expandableSection.Title = _entry.BuildHeaderSummary();
                Changed?.Invoke(this, EventArgs.Empty);
            };

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { dragHandle, textBlock, removeButton }
            };

            var chip = new Border
            {
                Child = row,
                Margin = new Thickness(0, 0, 8, 8)
            };
            chip.Classes.Add(UiClasses.SkillChip);

            dragHandle.PointerPressed += (_, e) =>
            {
                if (!e.GetCurrentPoint(dragHandle).Properties.IsLeftButtonPressed)
                {
                    return;
                }

                chip.Opacity = 0.75;
                _sectionView.BeginSkillDrag(_entry.Id, skill.Id);
                _sectionView.CaptureDragPointer(e.Pointer);
            };

            return chip;
        }

        private string BuildSkillChipLabel(SkillItem skill)
        {
            var parts = new List<string> { skill.Name.Trim(), _localizer.Get(skill.Proficiency.ToTranslationKey()) };
            if (skill.YearsOfExperience is not null)
            {
                parts.Add($"{skill.YearsOfExperience} {_localizer.Get(TranslationKeys.PreviewYearsSuffix)}");
            }

            return string.Join(" · ", parts);
        }

        private void UpdateBulkCounter()
        {
            _bulkCounterTextBlock.Text =
                $"{(_bulkSkillsTextBox.Text ?? string.Empty).Length} / {SkillsSchema.BulkSkillsMaxLength}";
        }

        private void OnGroupDragPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(_dragArea).Properties.IsLeftButtonPressed)
            {
                return;
            }

            RootBorder.Opacity = 0.75;
            _sectionView.BeginGroupDrag(_entry.Id);
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
            textBox.MinHeight = 72;
            return textBox;
        }

        private static TextBlock CreateCounterTextBlock()
        {
            var counter = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right };
            counter.Classes.Add(UiClasses.CounterText);
            return counter;
        }

        private static int? ParseYears(string? text)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var years) ? years : null;
        }
    }
}
