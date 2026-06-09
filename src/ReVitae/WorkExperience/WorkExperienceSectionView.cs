using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;
using ReVitae.Controls;
using ReVitae.Core.Cv.WorkExperience;
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

namespace ReVitae.WorkExperience;

public sealed class WorkExperienceSectionView : UserControl, IValidationNavigableSection, IQualityHintSection, IAiAdvisorSection
{
	private readonly ExpandableSection _section;
	private readonly StackPanel _contentPanel;
	private readonly StackPanel _entriesPanel;
	private readonly TextBlock _emptyHintTextBlock;
	private readonly Button _addButton;
	private readonly Button _sortButton;
	private readonly SectionHeaderBadges _headerBadges;
	private AppLocalizer _localizer = AppLocalizer.FromSystemCulture();
	private readonly List<WorkExperienceEntry> _entries = [];
	private readonly Dictionary<string, WorkExperienceEntryCard> _cardsById = new(StringComparer.Ordinal);
	private ValidationTouchTracker _touchTracker = new();
	private int _sectionErrorCount;
	private string? _dragSourceEntryId;
	private bool _suppressEntriesChanged;

	public WorkExperienceSectionView()
	{
		_headerBadges = new SectionHeaderBadges();

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
			IsExpanded = true,
			HeaderActions = _headerBadges.Root
		};
		_section.ExpandStateChanged += (_, _) => ExpandStateChanged?.Invoke(this, EventArgs.Empty);

		Content = _section;
	}

	public event EventHandler? EntriesChanged;

	public event EventHandler? ExpandStateChanged;

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

		UpdateSectionErrorBadge();
	}

	public void ConfigureAdvisor(Action onClick, string tooltip) =>
		_headerBadges.ConfigureAdvisor(onClick, tooltip);

	public void SetAdvisorVisible(bool visible) =>
		_headerBadges.SetAdvisorVisible(visible);

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

	public void UpdateValidation(FieldValidationResult validationResult, ValidationTouchTracker touchTracker)
	{
		_touchTracker = touchTracker;

		var sectionErrors = validationResult.Errors
			.Where(error => WorkExperienceFieldKeys.TryParseEntryId(error.FieldKey, out _, out _))
			.ToArray();
		_sectionErrorCount = sectionErrors.Length;

		foreach (var (entryId, card) in _cardsById)
		{
			var errors = sectionErrors
				.Where(error => WorkExperienceFieldKeys.TryParseEntryId(error.FieldKey, out var parsedId, out _)
					&& parsedId == entryId)
				.ToArray();
			card.UpdateValidation(errors, touchTracker);
		}

		UpdateSectionErrorBadge();
	}

	public bool ExpandAndRevealField(string fieldKey)
	{
		if (!WorkExperienceFieldKeys.TryParseEntryId(fieldKey, out var entryId, out _))
		{
			return false;
		}

		_section.IsExpanded = true;

		if (!_cardsById.TryGetValue(entryId, out var card))
		{
			return false;
		}

		return card.ExpandAndRevealField(fieldKey);
	}

	public bool TryApplyFieldText(string fieldKey, string text)
	{
		if (!WorkExperienceFieldKeys.TryParseEntryId(fieldKey, out var entryId, out var fieldName))
		{
			return false;
		}

		if (!_cardsById.TryGetValue(entryId, out var card))
		{
			return false;
		}

		return card.TryApplyFieldText(fieldName, text);
	}

	public Control? FindControlForFieldKey(string fieldKey)
	{
		if (!WorkExperienceFieldKeys.TryParseEntryId(fieldKey, out var entryId, out _))
		{
			return null;
		}

		return _cardsById.TryGetValue(entryId, out var card)
			? card.FindControlForFieldKey(fieldKey)
			: null;
	}

	public IReadOnlyList<string> GetOrderedFieldKeys()
	{
		var keys = new List<string>();
		foreach (var entry in _entries)
		{
			keys.AddRange(WorkExperienceEntryCard.GetOrderedFieldKeys(entry.Id));
		}

		return keys;
	}

	public void ReplaceEntries(IReadOnlyList<WorkExperienceEntry> entries, bool expandSection = true)
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
				.Where(confidence => WorkExperienceFieldKeys.TryParseEntryId(confidence.FieldKey, out var parsedId, out _)
					&& parsedId == entryId)
				.ToArray();
			card.ApplyImportConfidence(entryConfidences);
		}
	}

	public void AddEntry(WorkExperienceEntry? entry = null, int? insertIndex = null)
	{
		var newEntry = entry ?? new WorkExperienceEntry();
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

	private void UpdateSectionErrorBadge()
	{
		FormValidationService.UpdateSectionErrorBadge(
			_headerBadges.ErrorBadgePanel,
			_headerBadges.ErrorBadgeTextBlock,
			_sectionErrorCount,
			!_section.IsExpanded,
			_localizer,
			TranslationKeys.WorkExperienceValidationErrors,
			() => _section.IsExpanded = true);
	}

	private void RebuildEntryCards()
	{
		_entriesPanel.Children.Clear();
		_cardsById.Clear();

		for (var index = 0; index < _entries.Count; index++)
		{
			var entry = _entries[index];
			var card = new WorkExperienceEntryCard(entry, _localizer, index, _touchTracker);
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
		var sorted = WorkExperienceSorter.SortByDateNewestFirst(_entries);
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

	private sealed class WorkExperienceEntryCard
	{
		private readonly WorkExperienceEntry _entry;
		private readonly int _index;
		private readonly ValidationFieldRegistry _fieldRegistry = new();
		private ValidationTouchTracker _touchTracker;
		private AppLocalizer _localizer;
		private bool _isLoadingFromEntry;
		private readonly ExpandableSection _expandableSection;
		private readonly StackPanel _errorBadgePanel;
		private readonly TextBlock _errorBadgeTextBlock;
		private readonly TextBox _jobTitleTextBox;
		private readonly TextBox _companyTextBox;
		private readonly TextBox _locationTextBox;
		private readonly ComboBox _employmentTypeComboBox;
		private readonly DatePicker _startDatePicker;
		private readonly DatePicker _endDatePicker;
		private readonly CheckBox _currentlyWorkingCheckBox;
		private readonly TextBox _descriptionTextBox;
		private readonly TextBox _achievementsTextBox;
		private readonly TextBox _technologiesTextBox;
		private readonly TextBox _companyUrlTextBox;
		private readonly TextBlock _descriptionCounterTextBlock;
		private readonly TextBlock _achievementsCounterTextBlock;
		private readonly Dictionary<string, TextBox> _importConfidenceFields = new(StringComparer.Ordinal);
		private readonly Border _dragArea;
		private int _errorCount;

		public WorkExperienceEntryCard(
			WorkExperienceEntry entry,
			AppLocalizer localizer,
			int index,
			ValidationTouchTracker touchTracker)
		{
			_entry = entry;
			_localizer = localizer;
			_index = index;
			_touchTracker = touchTracker;

			(_errorBadgePanel, _errorBadgeTextBlock) = ValidationErrorBadgeFactory.Create();

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
			_startDatePicker = MonthYearDateHelper.CreatePicker(OnDateChanged);
			_endDatePicker = MonthYearDateHelper.CreatePicker(OnDateChanged);
			_currentlyWorkingCheckBox = new CheckBox();
			_currentlyWorkingCheckBox.IsCheckedChanged += OnCurrentlyWorkingChanged;
			_descriptionTextBox = CreateMultilineTextBox(OnFieldChanged);
			_achievementsTextBox = CreateMultilineTextBox(OnFieldChanged);
			_technologiesTextBox = CreateTextBox(OnFieldChanged);
			_companyUrlTextBox = CreateTextBox(OnFieldChanged);
			_descriptionCounterTextBlock = CreateCounterTextBlock();
			_achievementsCounterTextBlock = CreateCounterTextBlock();

			RegisterImportConfidenceField(WorkExperienceFieldKeys.JobTitle, _jobTitleTextBox);
			RegisterImportConfidenceField(WorkExperienceFieldKeys.Company, _companyTextBox);
			RegisterImportConfidenceField(WorkExperienceFieldKeys.Location, _locationTextBox);
			RegisterImportConfidenceField(WorkExperienceFieldKeys.Description, _descriptionTextBox);
			RegisterImportConfidenceField(WorkExperienceFieldKeys.Achievements, _achievementsTextBox);
			RegisterImportConfidenceField(WorkExperienceFieldKeys.Technologies, _technologiesTextBox);
			RegisterImportConfidenceField(WorkExperienceFieldKeys.CompanyUrl, _companyUrlTextBox);

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
			string FieldKey(string fieldName) => WorkExperienceFieldKeys.Build(entryId, fieldName);

			var body = new StackPanel
			{
				Spacing = 10,
				Children =
				{
					ValidationFieldRegistry.CreateFieldPanel(
						_localizer.Get(TranslationKeys.WorkExperienceJobTitle),
						_jobTitleTextBox,
						FieldKey(WorkExperienceFieldKeys.JobTitle),
						_fieldRegistry,
						_touchTracker),
					ValidationFieldRegistry.CreateFieldPanel(
						_localizer.Get(TranslationKeys.WorkExperienceCompany),
						_companyTextBox,
						FieldKey(WorkExperienceFieldKeys.Company),
						_fieldRegistry,
						_touchTracker),
					ValidationFieldRegistry.CreateFieldPanel(
						_localizer.Get(TranslationKeys.WorkExperienceLocation),
						_locationTextBox,
						FieldKey(WorkExperienceFieldKeys.Location),
						_fieldRegistry,
						_touchTracker),
					ValidationFieldRegistry.CreateFieldPanel(
						_localizer.Get(TranslationKeys.WorkExperienceEmploymentType),
						_employmentTypeComboBox,
						FieldKey(WorkExperienceFieldKeys.EmploymentType),
						_fieldRegistry,
						_touchTracker),
					ValidatedDateRangeBinding.CreatePanel(
						_localizer.Get(TranslationKeys.WorkExperienceStartDate),
						_startDatePicker,
						FieldKey(WorkExperienceFieldKeys.StartMonth),
						FieldKey(WorkExperienceFieldKeys.StartMonth),
						FieldKey(WorkExperienceFieldKeys.StartYear),
						FieldKey(WorkExperienceFieldKeys.DateRange),
						_fieldRegistry,
						_touchTracker),
					_currentlyWorkingCheckBox,
					ValidatedDateRangeBinding.CreatePanel(
						_localizer.Get(TranslationKeys.WorkExperienceEndDate),
						_endDatePicker,
						FieldKey(WorkExperienceFieldKeys.EndMonth),
						FieldKey(WorkExperienceFieldKeys.EndMonth),
						FieldKey(WorkExperienceFieldKeys.EndYear),
						FieldKey("_endDateRange"),
						_fieldRegistry,
						_touchTracker),
					ValidationFieldRegistry.CreateFieldPanel(
						_localizer.Get(TranslationKeys.WorkExperienceDescription),
						_descriptionTextBox,
						FieldKey(WorkExperienceFieldKeys.Description),
						_fieldRegistry,
						_touchTracker,
						_descriptionCounterTextBlock),
					ValidationFieldRegistry.CreateFieldPanel(
						_localizer.Get(TranslationKeys.WorkExperienceAchievements),
						_achievementsTextBox,
						FieldKey(WorkExperienceFieldKeys.Achievements),
						_fieldRegistry,
						_touchTracker,
						_achievementsCounterTextBlock),
					ValidationFieldRegistry.CreateFieldPanel(
						_localizer.Get(TranslationKeys.WorkExperienceTechnologies),
						_technologiesTextBox,
						FieldKey(WorkExperienceFieldKeys.Technologies),
						_fieldRegistry,
						_touchTracker),
					ValidationFieldRegistry.CreateFieldPanel(
						_localizer.Get(TranslationKeys.WorkExperienceCompanyUrl),
						_companyUrlTextBox,
						FieldKey(WorkExperienceFieldKeys.CompanyUrl),
						_fieldRegistry,
						_touchTracker),
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

		public static IReadOnlyList<string> GetOrderedFieldKeys(string entryId)
		{
			return
			[
				WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.JobTitle),
				WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.Company),
				WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.Location),
				WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.EmploymentType),
				WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.StartMonth),
				WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.StartYear),
				WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.DateRange),
				WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.EndMonth),
				WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.EndYear),
				WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.Description),
				WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.Achievements),
				WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.Technologies),
				WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.CompanyUrl)
			];
		}

		public void SetExpanded(bool isExpanded) => _expandableSection.IsExpanded = isExpanded;

		public void ApplyImportConfidence(IReadOnlyList<ImportedFieldConfidence> confidences)
		{
			ImportConfidenceHelper.ApplyToFields(_importConfidenceFields, confidences);
		}

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
			UpdateErrorBadge();
		}

		public void UpdateValidation(IReadOnlyList<FieldValidationError> errors, ValidationTouchTracker touchTracker)
		{
			_errorCount = errors.Count;
			_fieldRegistry.ApplyErrors(errors, _localizer, touchTracker);
			UpdateErrorBadge();
		}

		public bool ExpandAndRevealField(string fieldKey)
		{
			_expandableSection.IsExpanded = true;
			var control = FindControlForFieldKey(fieldKey);
			if (control is null)
			{
				return false;
			}

			control.Focus();
			return true;
		}

		public bool TryApplyFieldText(string fieldName, string text)
		{
			if (!string.Equals(fieldName, WorkExperienceFieldKeys.Description, StringComparison.Ordinal))
			{
				return false;
			}

			_entry.Description = text;
			_descriptionTextBox.Text = text;
			UpdateCharacterCounters();
			Changed?.Invoke(this, EventArgs.Empty);
			return true;
		}

		public Control? FindControlForFieldKey(string fieldKey) => _fieldRegistry.FindControlForFieldKey(fieldKey);

		private void UpdateErrorBadge()
		{
			ValidationErrorBadgeFactory.Update(
				_errorBadgePanel,
				_errorBadgeTextBlock,
				_errorCount,
				!_expandableSection.IsExpanded,
				_localizer.Format(TranslationKeys.WorkExperienceValidationErrors, _errorCount),
				() => _expandableSection.IsExpanded = true);
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
			_isLoadingFromEntry = true;
			try
			{
				_jobTitleTextBox.Text = _entry.JobTitle;
				_companyTextBox.Text = _entry.Company;
				_locationTextBox.Text = _entry.Location;
				_startDatePicker.SelectedDate = MonthYearDateHelper.ToSelectedDate(_entry.StartMonth, _entry.StartYear);
				_endDatePicker.SelectedDate = MonthYearDateHelper.ToSelectedDate(_entry.EndMonth, _entry.EndYear);
				_currentlyWorkingCheckBox.IsChecked = _entry.IsCurrentlyWorking;
				_descriptionTextBox.Text = _entry.Description;
				_achievementsTextBox.Text = _entry.Achievements;
				_technologiesTextBox.Text = _entry.Technologies;
				_companyUrlTextBox.Text = _entry.CompanyUrl;
				RefreshEmploymentTypeItems();
			}
			finally
			{
				_isLoadingFromEntry = false;
			}
		}

		private void SyncToEntry()
		{
			_entry.JobTitle = _jobTitleTextBox.Text ?? string.Empty;
			_entry.Company = _companyTextBox.Text ?? string.Empty;
			_entry.Location = _locationTextBox.Text ?? string.Empty;
			_entry.EmploymentType = _employmentTypeComboBox.SelectedItem is ComboBoxItem { Tag: EmploymentType employmentType }
				? employmentType
				: EmploymentType.FullTime;
			(_entry.StartMonth, _entry.StartYear) = MonthYearDateHelper.FromSelectedDate(_startDatePicker.SelectedDate);
			(_entry.EndMonth, _entry.EndYear) = MonthYearDateHelper.FromSelectedDate(_endDatePicker.SelectedDate);
			_entry.IsCurrentlyWorking = _currentlyWorkingCheckBox.IsChecked == true;
			_entry.Description = _descriptionTextBox.Text ?? string.Empty;
			_entry.Achievements = _achievementsTextBox.Text ?? string.Empty;
			_entry.Technologies = _technologiesTextBox.Text ?? string.Empty;
			_entry.CompanyUrl = _companyUrlTextBox.Text ?? string.Empty;
		}

		private void OnFieldChanged(object? sender, EventArgs e)
		{
			if (_isLoadingFromEntry)
			{
				return;
			}

			SyncToEntry();
			_expandableSection.Title = _entry.BuildHeaderSummary(_localizer.Get(TranslationKeys.WorkExperiencePresent));
			UpdateEndDateVisibility();
			UpdateCharacterCounters();
			Changed?.Invoke(this, EventArgs.Empty);
		}

		private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => OnFieldChanged(sender, e);

		private void OnDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e) => OnFieldChanged(sender, EventArgs.Empty);

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
			_endDatePicker.IsEnabled = !isCurrent;
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

		private void RegisterImportConfidenceField(string fieldName, TextBox textBox)
		{
			_importConfidenceFields[WorkExperienceFieldKeys.Build(_entry.Id, fieldName)] = textBox;
			textBox.TextChanged += (_, _) => textBox.Classes.Remove(UiClasses.ImportHint);
		}

	}
}
