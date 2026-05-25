using Avalonia.Controls;
using ReVitae.Core.Import;
using ReVitae.Core.Export;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;
using ReVitae.Tests.Ui.Validation;
using ReVitae.Ui.Quality;

namespace ReVitae.Tests.Ui.Quality;

public sealed class QualityHintsServiceEdgeCaseTests
{
	private static readonly AppLocalizer Localizer = ValidationTestHelpers.EnglishLocalizer;

	private static CvQualityHint CreateHint(string id) =>
		new(
			id,
			TranslationKeys.QualityHintPersonalSummaryTooShort,
			CvQualityHintSeverity.Suggestion,
			CvImportSectionId.PersonalInformation);

	[Fact]
	public void UpdateSectionQualityBadge_NoHints_HidesBadge()
	{
		var panel = new StackPanel();
		var text = new TextBlock();

		QualityHintsService.UpdateSectionQualityBadge(
			panel,
			text,
			[],
			Localizer,
			"Personal");

		Assert.False(panel.IsVisible);
		Assert.False(text.IsVisible);
		Assert.Equal(string.Empty, text.Text);
	}

	[Fact]
	public void UpdateSectionQualityBadge_WithHints_ShowsCount()
	{
		var panel = new StackPanel();
		var text = new TextBlock();
		var hints = new[] { CreateHint(CvQualityHintIds.PersonalSummaryTooShort), CreateHint(CvQualityHintIds.PersonalSummaryMissing) };

		QualityHintsService.UpdateSectionQualityBadge(
			panel,
			text,
			hints,
			Localizer,
			"Personal");

		Assert.True(panel.IsVisible);
		Assert.True(text.IsVisible);
		Assert.Contains("2", text.Text, StringComparison.Ordinal);
	}

	[Fact]
	public void UpdateSectionQualityBadge_SingleHint_ShowsCountOne()
	{
		var panel = new StackPanel();
		var text = new TextBlock();

		QualityHintsService.UpdateSectionQualityBadge(
			panel,
			text,
			[CreateHint(CvQualityHintIds.WorkSectionEmpty)],
			Localizer,
			"Work");

		Assert.Contains("1", text.Text, StringComparison.Ordinal);
	}

	[Fact]
	public void UpdateSectionQualityBadge_ClearsPreviousHandlersWhenHidden()
	{
		var panel = new StackPanel();
		var text = new TextBlock();
		QualityHintsService.UpdateSectionQualityBadge(panel, text, [CreateHint(CvQualityHintIds.WorkSectionEmpty)], Localizer, "Work");

		QualityHintsService.UpdateSectionQualityBadge(panel, text, [], Localizer, "Work");

		Assert.False(panel.IsVisible);
	}

	[Fact]
	public void UpdateSectionQualityBadge_SetsAccessibilityNameWhenVisible()
	{
		var panel = new StackPanel();
		var text = new TextBlock();

		QualityHintsService.UpdateSectionQualityBadge(
			panel,
			text,
			[CreateHint(CvQualityHintIds.LanguagesSectionEmpty)],
			Localizer,
			"Languages");

		Assert.True(panel.IsVisible);
	}

	[Fact]
	public void UpdateSectionQualityBadge_AcceptsDismissCallback()
	{
		var panel = new StackPanel();
		var text = new TextBlock();
		CvQualityHint? dismissed = null;

		QualityHintsService.UpdateSectionQualityBadge(
			panel,
			text,
			[CreateHint(CvQualityHintIds.PersonalSummaryTooShort)],
			Localizer,
			"Personal",
			dismissHint: hint => dismissed = hint);

		Assert.Null(dismissed);
		Assert.True(panel.IsVisible);
	}
}
