using System;

namespace ReVitae.Ui.Quality;

/// <summary>
/// A section view that exposes a per-section "Ask AI for tips" advisor action (045 A.2).
/// Implemented by delegating to the section's <see cref="SectionHeaderBadges"/>.
/// </summary>
public interface IAiAdvisorSection
{
	void ConfigureAdvisor(Action onClick, string tooltip);

	void SetAdvisorVisible(bool visible);
}
