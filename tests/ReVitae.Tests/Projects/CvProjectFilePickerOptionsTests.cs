using ReVitae.Core.Localization;
using ReVitae.Projects;

namespace ReVitae.Tests.Projects;

[Trait("Category", "Projects")]
public sealed class CvProjectFilePickerOptionsTests
{
	[Fact]
	public void CreateOpenOptions_PrefersBroadSupportedCvFilterOnMacOs()
	{
		var localizer = new AppLocalizer(AppLocalizer.FallbackLanguageCode);
		var options = CvProjectFilePickerOptions.CreateOpenOptions(localizer);

		Assert.NotNull(options.SuggestedFileType);
		Assert.NotNull(options.SuggestedFileType!.Patterns);
		Assert.Contains("*.pdf", options.SuggestedFileType.Patterns);
		Assert.Contains("*.txt", options.SuggestedFileType.Patterns);
		Assert.Contains("*.revitae.json", options.SuggestedFileType.Patterns);
		Assert.Same(options.SuggestedFileType, options.FileTypeFilter![0]);
		Assert.Contains(
			options.FileTypeFilter,
			filter => filter.Patterns is { Count: 1 } patterns && patterns.Contains("*.revitae.json"));
	}
}
