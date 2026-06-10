using System.Diagnostics;
using ReVitae.Core.Export;
using ReVitae.Tests.Infrastructure;

namespace ReVitae.Tests.Export;

/// <summary>
/// Prompt 049 C4 — performance and allocation guards. These are not micro-benchmarks: the
/// bounds are deliberately generous (≈10×+ headroom) so they only trip on a catastrophic
/// O(n²) time or allocation regression, never on normal CI jitter. Allocation is measured with
/// <c>GC.GetAllocatedBytesForCurrentThread</c> (deterministic, single-threaded), which is far
/// less flaky than wall-clock; the time ceilings are anti-hang backstops.
/// </summary>
[Trait("Category", "Perf")]
public sealed class PerformanceGuardTests
{
	private static long MeasureAllocations(Action action)
	{
		var before = GC.GetAllocatedBytesForCurrentThread();
		action();
		return GC.GetAllocatedBytesForCurrentThread() - before;
	}

	[Fact]
	public void LongCv_PdfExport_StaysUnderAllocationCeiling()
	{
		var document = CvExportTestFixtures.CreateLongContentDocument();
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();

		// Warm up so one-time static init (font registration, QuestPDF) is not counted.
		_ = CvExportTestHarness.ExportBytes(document, source, CvExportFormat.Pdf);

		var allocated = MeasureAllocations(
			() => CvExportTestHarness.ExportBytes(document, source, CvExportFormat.Pdf));

		Assert.True(
			allocated < 300_000_000,
			$"Long-CV PDF export allocated {allocated:N0} bytes — possible quadratic regression.");
	}

	[Fact]
	public void LongCv_PdfExport_CompletesWellWithinHangBound()
	{
		var document = CvExportTestFixtures.CreateLongContentDocument();
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		_ = CvExportTestHarness.ExportBytes(document, source, CvExportFormat.Pdf);

		var stopwatch = Stopwatch.StartNew();
		_ = CvExportTestHarness.ExportBytes(document, source, CvExportFormat.Pdf);
		stopwatch.Stop();

		Assert.True(
			stopwatch.Elapsed < TimeSpan.FromSeconds(30),
			$"Long-CV PDF export took {stopwatch.Elapsed.TotalSeconds:F1}s — likely hung or quadratic.");
	}

	[Fact]
	public void AllDocumentFormats_ExportWellWithinHangBound()
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument();
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();

		foreach (var format in CvExportTestHarness.DocumentFormats.Concat(CvExportTestHarness.StructuredFormats))
		{
			_ = CvExportTestHarness.ExportBytes(document, source, format);
		}

		var stopwatch = Stopwatch.StartNew();
		foreach (var format in CvExportTestHarness.DocumentFormats.Concat(CvExportTestHarness.StructuredFormats))
		{
			Assert.True(CvExportTestHarness.TryExport(document, source, format, out _));
		}

		stopwatch.Stop();
		Assert.True(
			stopwatch.Elapsed < TimeSpan.FromSeconds(60),
			$"Exporting all formats took {stopwatch.Elapsed.TotalSeconds:F1}s.");
	}

	[Fact]
	public void StructuredExport_ScalesLinearlyWithEntryCount()
	{
		// A 10× larger CV must not allocate dramatically more than 10× — guards against an
		// accidental O(n²) serializer. Compares allocation ratio with generous slack.
		long Allocate(int repeat)
		{
			var source = BuildSource(repeat);
			var document = CvExportTestFixtures.CreateRepresentativeDocument();
			_ = CvExportTestHarness.ExportBytes(document, source, CvExportFormat.RevitaeJson);
			return MeasureAllocations(
				() => CvExportTestHarness.ExportBytes(document, source, CvExportFormat.RevitaeJson));
		}

		var small = Allocate(2);
		var large = Allocate(20);

		// 10× the data should cost well under 40× the allocations if scaling is roughly linear.
		Assert.True(
			large < small * 40,
			$"Structured export scaled poorly: {small:N0} → {large:N0} bytes for 10× data.");
	}

	private static CvExportSourceData BuildSource(int workEntries)
	{
		var baseSource = CvExportTestFixtures.CreateRepresentativeSourceData();
		var work = Enumerable.Range(0, workEntries)
			.Select(i => new ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry
			{
				JobTitle = $"Engineer {i}",
				Company = $"Company {i}",
				Description = $"Did meaningful work number {i} with measurable impact.",
			})
			.ToArray();

		return new CvExportSourceData(
			baseSource.Personal,
			work,
			baseSource.Education,
			baseSource.Skills,
			baseSource.Languages,
			baseSource.Certificates,
			baseSource.Projects,
			baseSource.Links,
			baseSource.AdditionalInformation);
	}
}
