using ReVitae.Core.Export;
using ReVitae.Tests.Infrastructure;

namespace ReVitae.Tests.Export;

/// <summary>
/// Prompt 049 B14 — ATS-readability and PDF text-layer integrity across every template.
/// The product's core promise is that the exported PDF stays machine-readable: every
/// template must produce an extractable text layer carrying the candidate name and the
/// key content from each major section, with no control-character corruption. Matching is
/// whitespace-tolerant (carry forward the 0.2.13 PdfPig segmentation fix).
/// </summary>
public sealed class AtsReadabilityTests
{
	public static IEnumerable<object[]> TemplateIds =>
		CvExportTestHarness.AllTemplateIds.Select(id => new object[] { id });

	private static (string Text, string Compact) RenderText(CvExportTemplateId templateId)
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument(templateId);
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var pdf = CvExportTestHarness.ExportBytes(document, source, CvExportFormat.Pdf);
		var text = CvExportTestHarness.ExtractPdfText(pdf);
		return (text, CvExportTestHarness.RemoveWhitespace(text));
	}

	[Theory]
	[MemberData(nameof(TemplateIds))]
	public void Template_ProducesExtractableTextLayer(CvExportTemplateId templateId)
	{
		var (text, _) = RenderText(templateId);
		Assert.False(string.IsNullOrWhiteSpace(text), $"{templateId} produced an empty PDF text layer.");
	}

	[Theory]
	[MemberData(nameof(TemplateIds))]
	public void Template_ContainsCandidateName(CvExportTemplateId templateId)
	{
		var (_, compact) = RenderText(templateId);
		Assert.True(
			compact.Contains("Kostolný", StringComparison.Ordinal)
				|| compact.Contains("KOSTOLNÝ", StringComparison.Ordinal),
			$"{templateId} dropped the candidate name from the text layer.");
	}

	[Theory]
	[MemberData(nameof(TemplateIds))]
	public void Template_ContainsCoreSectionContent(CvExportTemplateId templateId)
	{
		var (_, compact) = RenderText(templateId);

		Assert.True(compact.Contains("Acme", StringComparison.Ordinal), $"{templateId} dropped work experience.");
		Assert.True(compact.Contains("Technical", StringComparison.Ordinal), $"{templateId} dropped education.");
	}

	/// <summary>
	/// The Projects section is rendered by every themed template (fixed in 0.2.13), but a
	/// known set of legacy <c>*PdfTemplate.cs</c> templates omit it. Rewriting legacy
	/// templates is an explicit non-goal of prompt 049, so this test pins the exact gap:
	/// it fails loudly if a NEW or themed template regresses, while documenting the legacy
	/// shortfall rather than hiding it.
	/// </summary>
	[Fact]
	public void Projects_RenderedByAllTemplates_ExceptKnownLegacyGap()
	{
		var knownLegacyGap = new HashSet<CvExportTemplateId>
		{
			CvExportTemplateId.CenteredMinimal,
			CvExportTemplateId.PeachDesigner,
			CvExportTemplateId.NavyProfileSplit,
			CvExportTemplateId.NavyOverlapPhoto,
			CvExportTemplateId.YellowSkillDots,
			CvExportTemplateId.RoyalBlueSidebar,
			CvExportTemplateId.OrangeTimeline,
			CvExportTemplateId.BlueAccentSummary,
			CvExportTemplateId.PillHeaderSplit
		};

		var missingProjects = CvExportTestHarness.AllTemplateIds
			.Where(id => !RenderText(id).Compact.Contains("ReVitae", StringComparison.Ordinal))
			.ToHashSet();

		Assert.Equal(knownLegacyGap, missingProjects);
	}

	[Theory]
	[MemberData(nameof(TemplateIds))]
	public void Template_TextLayerHasNoControlCharacterCorruption(CvExportTemplateId templateId)
	{
		var (text, _) = RenderText(templateId);

		Assert.DoesNotContain('\0', text);
		Assert.DoesNotContain(
			text,
			character => char.IsControl(character) && character is not ('\n' or '\r' or '\t' or '\f'));
	}
}
