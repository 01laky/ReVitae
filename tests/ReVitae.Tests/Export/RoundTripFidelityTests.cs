using System.Text;
using ReVitae.Core.Export;
using ReVitae.Core.Import.Structured;
using ReVitae.Tests.Infrastructure;

namespace ReVitae.Tests.Export;

/// <summary>
/// Prompt 049 A7 + C6 — round-trip fidelity. Exporting to the native structured format and
/// re-importing must lose no structured data (proven field-by-field via <see cref="CvModelDiff"/>),
/// including across Unicode content. This is the quantified data-loss guard the diff harness exists for.
/// </summary>
public sealed class RoundTripFidelityTests
{
	private static string ExportRevitaeJson(CvExportSourceData source)
	{
		using var stream = new MemoryStream();
		CvStructuredExportWriter.WriteRevitaeJson(source, stream);
		return Encoding.UTF8.GetString(stream.ToArray());
	}

	[Fact]
	public void RevitaeJson_RoundTrip_LosesNoStructuredData()
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();

		var imported = ReVitaeJsonMapper.Map(ExportRevitaeJson(source));
		Assert.True(imported.Success);

		var diffs = CvModelDiff.Compare(source, imported);
		Assert.True(diffs.Count == 0, $"Round-trip lost data: {string.Join(" | ", diffs)}");
	}

	[Fact]
	public void RevitaeJson_RoundTrip_PreservesUnicodeContent()
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		source.Personal.FirstName = "山田";
		source.Personal.LastName = "太郎";
		source.Personal.ShortSummary = "محمد 🚀 Łódź café — 简体中文";

		var imported = ReVitaeJsonMapper.Map(ExportRevitaeJson(source));
		Assert.True(imported.Success);

		var diffs = CvModelDiff.Compare(source, imported);
		Assert.True(diffs.Count == 0, $"Unicode round-trip lost data: {string.Join(" | ", diffs)}");
	}

	[Fact]
	public void RevitaeJson_RoundTrip_IsSemanticallyIdempotentOnSecondPass()
	{
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();

		var imported = ReVitaeJsonMapper.Map(ExportRevitaeJson(source));
		Assert.True(imported.Success);

		// Project the first import back to source shape, round-trip again, and assert the
		// second import carries the same structured fields — no progressive data loss across
		// passes (robust to internal id regeneration / field ordering, unlike byte equality).
		var rebuilt = new CvExportSourceData(
			imported.Personal,
			imported.WorkExperienceEntries,
			imported.EducationEntries,
			imported.SkillsGroups,
			imported.LanguageEntries,
			imported.CertificateEntries,
			imported.ProjectEntries,
			imported.LinkEntries,
			imported.AdditionalInformationContent);

		var reimported = ReVitaeJsonMapper.Map(ExportRevitaeJson(rebuilt));
		Assert.True(reimported.Success);

		var diffs = CvModelDiff.Compare(rebuilt, reimported);
		Assert.True(diffs.Count == 0, $"Second-pass round-trip drifted: {string.Join(" | ", diffs)}");
	}
}
