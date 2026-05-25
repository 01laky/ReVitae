using ReVitae.Core.Import;
using ReVitae.Core.Export;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;
using ReVitae.Ui.Quality;

namespace ReVitae.Tests.Ui.Quality;

public sealed class QualityHintDismissalStoreEdgeCaseTests
{
	private static CvQualityHint CreateHint(string id) =>
		new(
			id,
			TranslationKeys.QualityHintPersonalSummaryTooShort,
			CvQualityHintSeverity.Suggestion,
			CvImportSectionId.PersonalInformation);

	[Fact]
	public void Dismiss_AddsStableDismissKey()
	{
		var store = new QualityHintDismissalStore();
		var hint = CreateHint(CvQualityHintIds.PersonalSummaryTooShort);

		store.Dismiss(hint);

		Assert.Contains(CvQualityAnalyzer.BuildDismissKey(hint), store.DismissedKeys);
	}

	[Fact]
	public void Restore_ReloadsPersistedKeys()
	{
		var store = new QualityHintDismissalStore();
		var key = CvQualityAnalyzer.BuildDismissKey(CreateHint(CvQualityHintIds.PersonalSummaryMissing));

		store.Restore([key, "  ", string.Empty]);

		Assert.Single(store.DismissedKeys);
		Assert.Contains(key, store.DismissedKeys);
	}

	[Fact]
	public void Clear_RemovesAllDismissals()
	{
		var store = new QualityHintDismissalStore();
		store.Dismiss(CreateHint(CvQualityHintIds.PersonalSummaryTooShort));

		store.Clear();

		Assert.Empty(store.DismissedKeys);
	}

	[Fact]
	public void Restore_ReplacesPreviousKeys()
	{
		var store = new QualityHintDismissalStore();
		store.Dismiss(CreateHint(CvQualityHintIds.PersonalSummaryTooShort));
		var newKey = CvQualityAnalyzer.BuildDismissKey(CreateHint(CvQualityHintIds.WorkSectionEmpty));

		store.Restore([newKey]);

		Assert.Single(store.DismissedKeys);
		Assert.Contains(newKey, store.DismissedKeys);
	}

	[Fact]
	public void Dismiss_IsIdempotentForSameHint()
	{
		var store = new QualityHintDismissalStore();
		var hint = CreateHint(CvQualityHintIds.LanguagesSectionEmpty);

		store.Dismiss(hint);
		store.Dismiss(hint);

		Assert.Single(store.DismissedKeys);
	}

	[Fact]
	public void DismissedKeys_IsReadOnlyView()
	{
		var store = new QualityHintDismissalStore();
		store.Dismiss(CreateHint(CvQualityHintIds.PersonalSummaryTooShort));

		Assert.IsAssignableFrom<IReadOnlySet<string>>(store.DismissedKeys);
	}
}
