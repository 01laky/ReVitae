using ReVitae.Core.Export;
using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Cv;

/// <summary>
/// Minimum-content gate for the per-section advisor (045 C.7), mirroring the 80-char import
/// gate. The generic advisor is only worth calling when a section has enough signal to
/// reason about; truly empty sections route to the draft / advice tasks (A.1) instead.
/// </summary>
public static class AiCvAdvisorGate
{
	/// <summary>Minimum non-whitespace characters for a free-text section (summary).</summary>
	public const int MinSummaryChars = 30;

	/// <summary>True when the generic advisor should be offered for this section.</summary>
	public static bool ShouldOffer(CvImportSectionId section, CvExportSourceData snapshot)
	{
		if (!AiCvSectionContent.IsAdvisorSection(section))
		{
			return false;
		}

		var metrics = AiCvSectionContent.Measure(section, snapshot);
		return section switch
		{
			CvImportSectionId.Summary => metrics.NonWhitespaceCharCount >= MinSummaryChars,
			_ => metrics.EntryCount >= 1 && metrics.NonWhitespaceCharCount > 0,
		};
	}
}
