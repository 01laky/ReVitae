using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Quality;
using CvCertificateEntry = ReVitae.Core.Cv.Certificates.CertificateEntry;
using CvEducationEntry = ReVitae.Core.Cv.Education.EducationEntry;
using CvLanguageEntry = ReVitae.Core.Cv.Languages.LanguageEntry;
using CvLinkEntry = ReVitae.Core.Cv.Links.LinkEntry;
using CvProjectEntry = ReVitae.Core.Cv.Projects.ProjectEntry;
using CvSkillsGroupEntry = ReVitae.Core.Cv.Skills.SkillsGroupEntry;
using CvWorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;
using CvSkillItem = ReVitae.Core.Cv.Skills.SkillItem;

namespace ReVitae.Tests.Quality;

public sealed class CvQualityAnalyzerEdgeCaseTests
{
	private static CvExportSourceData Snapshot(
		PersonalInformationImport? personal = null,
		IEnumerable<CvWorkExperienceEntry>? work = null,
		IEnumerable<CvEducationEntry>? education = null,
		IEnumerable<CvSkillsGroupEntry>? skills = null,
		IEnumerable<CvLanguageEntry>? languages = null,
		IEnumerable<CvCertificateEntry>? certificates = null,
		IEnumerable<CvProjectEntry>? projects = null,
		IEnumerable<CvLinkEntry>? links = null,
		string? additional = null) =>
		CvExportSourceDataFactory.Create(
			personal ?? new PersonalInformationImport(),
			work ?? [],
			education ?? [],
			skills ?? [],
			languages ?? [],
			certificates ?? [],
			projects ?? [],
			links ?? [],
			additional);

	private static CvWorkExperienceEntry WorkEntry(string description = "Implemented CI pipelines cutting build times by 40%.") =>
		new() { JobTitle = "Engineer", Company = "Corp", Description = description };

	private static CvEducationEntry EduEntry() =>
		new() { Institution = "MIT", Degree = "BSc Computer Science" };

	private static CvSkillsGroupEntry SkillGroup(int count)
	{
		var group = new CvSkillsGroupEntry { Category = "Programming" };
		for (var i = 0; i < count; i++)
		{
			group.Skills.Add(new CvSkillItem { Name = $"Skill{i}" });
		}

		return group;
	}

	// ── PersonalSummary boundary ────────────────────────────────────────────

	[Fact]
	public void Summary_ExactlyOneChar_TooShortHint()
	{
		var personal = new PersonalInformationImport { FirstName = "A", ShortSummary = "X" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.PersonalSummaryTooShort);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(40)]
	[InlineData(79)]
	public void Summary_LessThan80NonWhitespace_TooShortHint(int length)
	{
		var personal = new PersonalInformationImport { ShortSummary = new string('x', length) };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.PersonalSummaryTooShort);
	}

	[Theory]
	[InlineData(80)]
	[InlineData(300)]
	[InlineData(600)]
	public void Summary_80To600NonWhitespace_NoBoundaryHint(int length)
	{
		var personal = new PersonalInformationImport { ShortSummary = new string('x', length) };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.PersonalSummaryTooShort);
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.PersonalSummaryTooLong);
	}

	[Fact]
	public void Summary_Exactly601NonWhitespace_TooLongHint()
	{
		var personal = new PersonalInformationImport { ShortSummary = new string('x', 601) };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.PersonalSummaryTooLong);
	}

	[Fact]
	public void Summary_WhitespaceOnly_NeitherTooShortNorMissing()
	{
		var personal = new PersonalInformationImport
		{
			FirstName = "Jane",
			ShortSummary = new string(' ', 200)
		};
		var work = new[] { WorkEntry() };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, work: work));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.PersonalSummaryTooShort);
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.PersonalSummaryMissing);
	}

	[Fact]
	public void Summary_TooShort_DoesNotAlsoFireMissingHint()
	{
		var personal = new PersonalInformationImport
		{
			FirstName = "Jane",
			ShortSummary = "Brief."
		};
		var work = new[] { WorkEntry() };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, work: work));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.PersonalSummaryTooShort);
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.PersonalSummaryMissing);
	}

	// ── PersonalMissingTitle ────────────────────────────────────────────────

	[Fact]
	public void PersonalMissingTitle_CvStarted_HintFires()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.PersonalMissingTitle);
	}

	[Fact]
	public void PersonalMissingTitle_CvStartedWithTitle_HintDoesNotFire()
	{
		var personal = new PersonalInformationImport
		{
			FirstName = "Jane",
			ProfessionalTitle = "Software Engineer"
		};
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.PersonalMissingTitle);
	}

	[Fact]
	public void PersonalMissingTitle_EmptyCv_HintDoesNotFire()
	{
		var report = CvQualityAnalyzer.Analyze(Snapshot());
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.PersonalMissingTitle);
	}

	// ── WorkSectionEmpty ────────────────────────────────────────────────────

	[Fact]
	public void WorkSectionEmpty_CvStartedWithEducation_HintFires()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, education: [EduEntry()]));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.WorkSectionEmpty);
	}

	[Fact]
	public void WorkSectionEmpty_CvStartedNameOnly_HintDoesNotFire()
	{
		// HasOtherSectionData(excluding work) returns false when only name is set
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.WorkSectionEmpty);
	}

	// ── WorkEntry descriptions ──────────────────────────────────────────────

	[Fact]
	public void WorkEntry_EmptyDescription_MissingDescriptionHint_NotGenericHint()
	{
		var work = new[] { new CvWorkExperienceEntry { JobTitle = "Dev", Company = "Co" } };
		var report = CvQualityAnalyzer.Analyze(Snapshot(
			personal: new PersonalInformationImport { FirstName = "A" }, work: work));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.WorkEntryMissingDescription);
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.WorkGenericDescription);
	}

	[Fact]
	public void WorkEntry_MissingDescription_HintCarriesEntryId()
	{
		var entry = new CvWorkExperienceEntry { JobTitle = "Dev", Company = "Co" };
		var work = new[] { entry };
		var report = CvQualityAnalyzer.Analyze(Snapshot(
			personal: new PersonalInformationImport { FirstName = "A" }, work: work));
		var hint = Assert.Single(report.Hints, h => h.Id == CvQualityHintIds.WorkEntryMissingDescription);
		Assert.Equal(entry.Id, hint.EntryId);
	}

	[Fact]
	public void WorkEntry_GenericDescription_HintCarriesEntryId()
	{
		var entry = new CvWorkExperienceEntry
		{
			JobTitle = "Dev",
			Company = "Co",
			Description = "Responsible for various duties and ongoing team operations and more tasks."
		};
		var report = CvQualityAnalyzer.Analyze(Snapshot(
			personal: new PersonalInformationImport { FirstName = "A" }, work: [entry]));
		var hint = Assert.Single(report.Hints, h => h.Id == CvQualityHintIds.WorkGenericDescription);
		Assert.Equal(entry.Id, hint.EntryId);
	}

	[Fact]
	public void WorkEntries_MultipleEntries_OnlyGenericOnesGetHint()
	{
		var generic = new CvWorkExperienceEntry
		{
			JobTitle = "Dev",
			Company = "Co",
			Description = "Responsible for various duties and ongoing team operations across departments."
		};
		var specific = WorkEntry("Reduced deployment time by 50% via CI/CD automation.");
		var report = CvQualityAnalyzer.Analyze(Snapshot(
			personal: new PersonalInformationImport { FirstName = "A" }, work: [generic, specific]));
		var genericHints = report.Hints.Where(h => h.Id == CvQualityHintIds.WorkGenericDescription).ToArray();
		Assert.Single(genericHints);
		Assert.Equal(generic.Id, genericHints[0].EntryId);
	}

	// ── Skills ──────────────────────────────────────────────────────────────

	[Fact]
	public void SkillsSectionEmpty_CvStarted_HintFires()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.SkillsSectionEmpty);
	}

	[Fact]
	public void SkillsSingleLargeGroup_Exactly15Skills_NoHint()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, skills: [SkillGroup(15)]));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.SkillsSingleLargeGroup);
	}

	[Fact]
	public void SkillsSingleLargeGroup_Exactly16Skills_HintFires()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, skills: [SkillGroup(16)]));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.SkillsSingleLargeGroup);
	}

	[Fact]
	public void SkillsSingleLargeGroup_HintCarriesGroupId()
	{
		var group = SkillGroup(20);
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, skills: [group]));
		var hint = Assert.Single(report.Hints, h => h.Id == CvQualityHintIds.SkillsSingleLargeGroup);
		Assert.Equal(group.Id, hint.EntryId);
	}

	[Fact]
	public void SkillsSingleLargeGroup_MultipleGroups_OnlyOversizedGetsHint()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var big = SkillGroup(20);
		var small = SkillGroup(5);
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, skills: [big, small]));
		var hints = report.Hints.Where(h => h.Id == CvQualityHintIds.SkillsSingleLargeGroup).ToArray();
		Assert.Single(hints);
		Assert.Equal(big.Id, hints[0].EntryId);
	}

	// ── Languages ──────────────────────────────────────────────────────────

	[Fact]
	public void LanguagesSectionEmpty_NoWork_NoHint()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.LanguagesSectionEmpty);
	}

	[Fact]
	public void LanguagesSectionEmpty_WithWorkAndLanguages_NoHint()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var lang = new CvLanguageEntry { Language = "English" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(
			personal: personal, work: [WorkEntry()], languages: [lang]));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.LanguagesSectionEmpty);
	}

	// ── EducationSectionEmpty ───────────────────────────────────────────────

	[Fact]
	public void EducationSectionEmpty_CvStartedWithWork_HintFires()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, work: [WorkEntry()]));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.EducationSectionEmpty);
	}

	[Fact]
	public void EducationSectionEmpty_CvStartedNameOnly_HintDoesNotFire()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.EducationSectionEmpty);
	}

	// ── CertificatesSectionEmpty ────────────────────────────────────────────

	[Fact]
	public void CertificatesSectionEmpty_CvStartedWithWork_HintFires()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, work: [WorkEntry()]));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.CertificatesSectionEmpty);
	}

	// ── ProjectsSectionEmpty & ProjectsEntryMissingDescription ─────────────

	[Fact]
	public void ProjectsSectionEmpty_CvStartedWithWork_HintFires()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, work: [WorkEntry()]));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.ProjectsSectionEmpty);
	}

	[Fact]
	public void ProjectsEntry_WithDescription_NoMissingDescriptionHint()
	{
		var project = new CvProjectEntry { Name = "MyApp", Description = "A full-stack web app." };
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, projects: [project]));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.ProjectsEntryMissingDescription);
	}

	[Fact]
	public void ProjectsEntry_WithHighlightsOnly_NoMissingDescriptionHint()
	{
		var project = new CvProjectEntry { Name = "MyApp", Highlights = "Won hackathon." };
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, projects: [project]));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.ProjectsEntryMissingDescription);
	}

	[Fact]
	public void ProjectsEntry_NeitherDescNorHighlights_HintFires()
	{
		var project = new CvProjectEntry { Name = "MyApp" };
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, projects: [project]));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.ProjectsEntryMissingDescription);
	}

	[Fact]
	public void ProjectsEntry_MissingDescription_HintCarriesEntryId()
	{
		var project = new CvProjectEntry { Name = "MyApp" };
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, projects: [project]));
		var hint = Assert.Single(report.Hints, h => h.Id == CvQualityHintIds.ProjectsEntryMissingDescription);
		Assert.Equal(project.Id, hint.EntryId);
	}

	// ── Links duplicate detection ───────────────────────────────────────────

	[Fact]
	public void Links_DuplicateGitHubUrl_HintFires()
	{
		var personal = new PersonalInformationImport
		{
			FirstName = "Jane",
			GitHubUrl = "https://github.com/jane"
		};
		var links = new[] { new CvLinkEntry { Label = "GitHub", Url = "https://www.github.com/jane/" } };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, links: links));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.LinksDuplicatePersonalUrl);
	}

	[Fact]
	public void Links_DuplicatePortfolioUrl_HintFires()
	{
		var personal = new PersonalInformationImport
		{
			FirstName = "Jane",
			PortfolioUrl = "https://jane.dev"
		};
		var links = new[] { new CvLinkEntry { Label = "Portfolio", Url = "https://jane.dev/" } };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, links: links));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.LinksDuplicatePersonalUrl);
	}

	[Fact]
	public void Links_EmptyUrl_NoDuplicateHint()
	{
		var personal = new PersonalInformationImport
		{
			FirstName = "Jane",
			LinkedInUrl = "https://linkedin.com/in/jane"
		};
		var links = new[] { new CvLinkEntry { Label = "Something", Url = string.Empty } };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, links: links));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.LinksDuplicatePersonalUrl);
	}

	[Fact]
	public void Links_PersonalLinkedInEmpty_NoDuplicateHint()
	{
		var personal = new PersonalInformationImport
		{
			FirstName = "Jane",
			LinkedInUrl = string.Empty
		};
		var links = new[] { new CvLinkEntry { Label = "Li", Url = "https://linkedin.com/in/jane" } };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, links: links));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.LinksDuplicatePersonalUrl);
	}

	[Fact]
	public void Links_NonMatchingUrl_NoDuplicateHint()
	{
		var personal = new PersonalInformationImport
		{
			FirstName = "Jane",
			LinkedInUrl = "https://linkedin.com/in/jane"
		};
		var links = new[] { new CvLinkEntry { Label = "Blog", Url = "https://myblog.com" } };
		var report = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, links: links));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.LinksDuplicatePersonalUrl);
	}

	// ── Import confidence ───────────────────────────────────────────────────

	[Fact]
	public void ImportConfidence_SingleLowField_NoSectionHint()
	{
		var entry = WorkEntry();
		var confidences = new[]
		{
			new ImportedFieldConfidence(
				WorkExperienceFieldKeys.Build(entry.Id, WorkExperienceFieldKeys.Company),
				CvImportConfidence.Low)
		};
		var report = CvQualityAnalyzer.Analyze(
			Snapshot(personal: new PersonalInformationImport { FirstName = "A" }, work: [entry]),
			new CvQualityAnalysisOptions(confidences));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.ImportReviewSection);
	}

	[Fact]
	public void ImportConfidence_TwoLowFieldsSameSection_SectionHintFires()
	{
		var entry = WorkEntry();
		var confidences = new[]
		{
			new ImportedFieldConfidence(
				WorkExperienceFieldKeys.Build(entry.Id, WorkExperienceFieldKeys.JobTitle),
				CvImportConfidence.Low),
			new ImportedFieldConfidence(
				WorkExperienceFieldKeys.Build(entry.Id, WorkExperienceFieldKeys.Company),
				CvImportConfidence.Low)
		};
		var report = CvQualityAnalyzer.Analyze(
			Snapshot(personal: new PersonalInformationImport { FirstName = "A" }, work: [entry]),
			new CvQualityAnalysisOptions(confidences));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.ImportReviewSection);
	}

	[Fact]
	public void ImportConfidence_LowFieldsTwoDifferentSections_SectionHintFires()
	{
		// Deduplication collapses ImportReviewSection hints (no EntryId/FieldKey in dismiss key),
		// so even two qualifying sections produce exactly one deduplicated section hint.
		var workEntry = WorkEntry();
		var eduEntry = EduEntry();
		var confidences = new[]
		{
			new ImportedFieldConfidence(
				WorkExperienceFieldKeys.Build(workEntry.Id, WorkExperienceFieldKeys.JobTitle),
				CvImportConfidence.Low),
			new ImportedFieldConfidence(
				WorkExperienceFieldKeys.Build(workEntry.Id, WorkExperienceFieldKeys.Company),
				CvImportConfidence.Low),
			new ImportedFieldConfidence(
				EducationFieldKeys.Build(eduEntry.Id, EducationFieldKeys.Institution),
				CvImportConfidence.Low),
			new ImportedFieldConfidence(
				EducationFieldKeys.Build(eduEntry.Id, EducationFieldKeys.Degree),
				CvImportConfidence.Low)
		};
		var report = CvQualityAnalyzer.Analyze(
			Snapshot(
				personal: new PersonalInformationImport { FirstName = "A" },
				work: [workEntry],
				education: [eduEntry]),
			new CvQualityAnalysisOptions(confidences));
		Assert.Contains(report.Hints, h => h.Id == CvQualityHintIds.ImportReviewSection);
	}

	[Fact]
	public void ImportConfidence_NullList_NoImportHints()
	{
		var report = CvQualityAnalyzer.Analyze(
			Snapshot(personal: new PersonalInformationImport { FirstName = "A" }),
			new CvQualityAnalysisOptions(ImportConfidences: null));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.ImportReviewSection);
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.ImportReviewField);
	}

	[Fact]
	public void ImportConfidence_HighConfidenceFields_NoImportHints()
	{
		var entry = WorkEntry();
		var confidences = new[]
		{
			new ImportedFieldConfidence(
				WorkExperienceFieldKeys.Build(entry.Id, WorkExperienceFieldKeys.JobTitle),
				CvImportConfidence.High),
			new ImportedFieldConfidence(
				WorkExperienceFieldKeys.Build(entry.Id, WorkExperienceFieldKeys.Company),
				CvImportConfidence.High)
		};
		var report = CvQualityAnalyzer.Analyze(
			Snapshot(personal: new PersonalInformationImport { FirstName = "A" }, work: [entry]),
			new CvQualityAnalysisOptions(confidences));
		Assert.DoesNotContain(report.Hints, h => h.Id == CvQualityHintIds.ImportReviewSection);
	}

	// ── Deduplication ───────────────────────────────────────────────────────

	[Fact]
	public void BuildDismissKey_ContainsAllThreeParts()
	{
		var hint = new CvQualityHint(
			CvQualityHintIds.WorkGenericDescription,
			"key",
			CvQualityHintSeverity.Suggestion,
			CvImportSectionId.WorkExperience,
			FieldKey: "work.entry-1.description",
			EntryId: "entry-1");

		var key = CvQualityAnalyzer.BuildDismissKey(hint);

		Assert.Contains(CvQualityHintIds.WorkGenericDescription, key, StringComparison.Ordinal);
		Assert.Contains("entry-1", key, StringComparison.Ordinal);
		Assert.Contains("work.entry-1.description", key, StringComparison.Ordinal);
	}

	[Fact]
	public void DismissedHints_MultipleKeysAllFiltered()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane" };
		var work = new[] { WorkEntry() };
		var initialReport = CvQualityAnalyzer.Analyze(Snapshot(personal: personal, work: work));
		var dismissKeys = initialReport.Hints.Select(CvQualityAnalyzer.BuildDismissKey).ToHashSet();

		var filtered = CvQualityAnalyzer.Analyze(
			Snapshot(personal: personal, work: work),
			new CvQualityAnalysisOptions(DismissedHintKeys: dismissKeys));

		Assert.Empty(filtered.Hints);
	}

	[Fact]
	public void Hints_NoDuplicates_WhenSameHintCouldFireTwice()
	{
		// Two work entries with the same generic-ish text would each generate their own hint,
		// but the same entry should never produce duplicate hint IDs for the same field key.
		var entry = new CvWorkExperienceEntry
		{
			JobTitle = "Dev",
			Company = "Corp",
			Description = "Responsible for various duties and ongoing operations across the department."
		};
		var report = CvQualityAnalyzer.Analyze(Snapshot(
			personal: new PersonalInformationImport { FirstName = "A" }, work: [entry]));
		var genericHints = report.Hints.Where(h =>
			h.Id == CvQualityHintIds.WorkGenericDescription && h.EntryId == entry.Id).ToArray();
		Assert.Single(genericHints);
	}

	// ── FullFeatured CV produces no quality hints ───────────────────────────

	[Fact]
	public void Analyze_FullFeaturedCv_NoSpuriousHints()
	{
		var personal = new PersonalInformationImport
		{
			FirstName = "Jane",
			LastName = "Doe",
			ProfessionalTitle = "Senior Software Engineer",
			Email = "jane@example.com",
			ShortSummary = new string('x', 200)
		};
		var work = new[]
		{
			new CvWorkExperienceEntry
			{
				JobTitle = "Engineer",
				Company = "Corp",
				Description = "Increased API throughput by 30% through caching and query optimization."
			}
		};
		var education = new[] { EduEntry() };
		var skills = new[] { SkillGroup(5) };
		var languages = new[] { new CvLanguageEntry { Language = "English" } };
		var certs = new[] { new CvCertificateEntry { Name = "AWS SAA" } };
		var projects = new[] { new CvProjectEntry { Name = "App", Description = "A full-stack app." } };

		var report = CvQualityAnalyzer.Analyze(
			Snapshot(personal: personal, work: work, education: education,
				skills: skills, languages: languages, certificates: certs, projects: projects));

		Assert.Empty(report.Hints);
	}
}
