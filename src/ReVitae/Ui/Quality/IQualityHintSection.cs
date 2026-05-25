using System;
using System.Collections.Generic;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;

namespace ReVitae.Ui.Quality;

public interface IQualityHintSection
{
	void ApplyQualityHints(
		IReadOnlyList<CvQualityHint> sectionHints,
		AppLocalizer localizer,
		Func<CvQualityHint, bool>? navigateToHint,
		Action<CvQualityHint>? dismissHint,
		Action? flyoutOpened);
}
