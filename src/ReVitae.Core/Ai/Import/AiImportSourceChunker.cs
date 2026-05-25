using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Import;

public sealed class AiImportSourceChunker
{
	private readonly AiImportBatchProfile _profile;

	public AiImportSourceChunker(AiImportBatchProfile profile)
	{
		_profile = profile;
	}

	public IReadOnlyList<AiImportBatchDescriptor> BuildBatches(
		string normalizedText,
		CvSegmentationResult segmentation)
	{
		var batches = new List<AiImportBatchDescriptor>();
		var step = 0;

		void AddPhase(AiImportPhase phase, string sectionText, int entriesPerBatch)
		{
			if (string.IsNullOrWhiteSpace(sectionText))
			{
				return;
			}

			var slices = SplitSection(sectionText, entriesPerBatch);
			for (var i = 0; i < slices.Count; i++)
			{
				batches.Add(new AiImportBatchDescriptor(phase, i + 1, slices.Count, step++, slices[i]));
			}
		}

		var personalText = BuildPersonalText(normalizedText, segmentation);
		AddPhase(AiImportPhase.Personal, personalText, 1);
		AddPhase(
			AiImportPhase.Work,
			GetSectionBody(segmentation, CvImportSectionId.WorkExperience),
			_profile.WorkEntriesPerBatch);
		AddPhase(
			AiImportPhase.Education,
			GetSectionBody(segmentation, CvImportSectionId.Education),
			_profile.EducationEntriesPerBatch);

		if (_profile.CombineSkillsAndLanguages)
		{
			var combined = JoinNonEmpty(
				GetSectionBody(segmentation, CvImportSectionId.Skills),
				GetSectionBody(segmentation, CvImportSectionId.Languages));
			AddPhase(AiImportPhase.SkillsAndLanguages, combined, 1);
		}
		else
		{
			AddPhase(
				AiImportPhase.Skills,
				GetSectionBody(segmentation, CvImportSectionId.Skills),
				1);
			AddPhase(
				AiImportPhase.Languages,
				GetSectionBody(segmentation, CvImportSectionId.Languages),
				1);
		}

		AddPhase(
			AiImportPhase.Certificates,
			GetSectionBody(segmentation, CvImportSectionId.Certificates),
			_profile.EducationEntriesPerBatch);
		AddPhase(
			AiImportPhase.Projects,
			GetSectionBody(segmentation, CvImportSectionId.Projects),
			_profile.ProjectsEntriesPerBatch);
		AddPhase(
			AiImportPhase.Links,
			GetSectionBody(segmentation, CvImportSectionId.Links),
			1);
		AddPhase(
			AiImportPhase.Additional,
			GetSectionBody(segmentation, CvImportSectionId.AdditionalInformation),
			1);

		if (batches.Count == 0 && !string.IsNullOrWhiteSpace(normalizedText))
		{
			foreach (var slice in SplitWindows(normalizedText))
			{
				batches.Add(new AiImportBatchDescriptor(AiImportPhase.Personal, batches.Count + 1, 1, step++, slice));
			}
		}

		return batches;
	}

	internal int MaxSliceChars(int carryForwardChars) =>
		Math.Max(200, _profile.MaxInputChars - carryForwardChars - _profile.PromptOverheadChars);

	private List<string> SplitSection(string sectionText, int entriesPerBatch)
	{
		var maxChars = MaxSliceChars(0);
		var paragraphs = sectionText.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (paragraphs.Length > 1 && entriesPerBatch >= 1)
		{
			var slices = new List<string>();
			var current = new List<string>();
			foreach (var paragraph in paragraphs)
			{
				current.Add(paragraph);
				if (current.Count >= Math.Max(1, entriesPerBatch))
				{
					slices.Add(TruncateSlice(string.Join("\n\n", current), maxChars));
					current.Clear();
				}
			}

			if (current.Count > 0)
			{
				slices.Add(TruncateSlice(string.Join("\n\n", current), maxChars));
			}

			if (slices.Count > 0)
			{
				return slices;
			}
		}

		if (sectionText.Length <= maxChars)
		{
			return [TruncateSlice(sectionText, maxChars)];
		}

		if (paragraphs.Length <= 1)
		{
			return SplitWindows(sectionText);
		}

		var fallbackSlices = new List<string>();
		var fallbackCurrent = new List<string>();
		foreach (var paragraph in paragraphs)
		{
			fallbackCurrent.Add(paragraph);
			if (fallbackCurrent.Count >= Math.Max(1, entriesPerBatch))
			{
				fallbackSlices.Add(TruncateSlice(string.Join("\n\n", fallbackCurrent), maxChars));
				fallbackCurrent.Clear();
			}
		}

		if (fallbackCurrent.Count > 0)
		{
			fallbackSlices.Add(TruncateSlice(string.Join("\n\n", fallbackCurrent), maxChars));
		}

		return fallbackSlices.Count > 0 ? fallbackSlices : SplitWindows(sectionText);
	}

	private List<string> SplitWindows(string text)
	{
		var maxChars = MaxSliceChars(0);
		if (text.Length <= maxChars)
		{
			return [TruncateSlice(text, maxChars)];
		}

		var overlap = _profile.PhaseMode is AiImportPhaseMode.SequentialMicro or AiImportPhaseMode.SequentialSmall
			? Math.Min(120, Math.Max(1, maxChars / 10))
			: 0;
		var slices = new List<string>();
		var start = 0;
		while (start < text.Length)
		{
			var length = Math.Min(maxChars, text.Length - start);
			slices.Add(text.Substring(start, length));
			if (start + length >= text.Length)
			{
				break;
			}

			start += Math.Max(1, length - overlap);
		}

		return slices;
	}

	private static string TruncateSlice(string text, int maxChars)
	{
		if (text.Length <= maxChars)
		{
			return text;
		}

		return text[..maxChars] + "[...]";
	}

	private static string BuildPersonalText(string normalizedText, CvSegmentationResult segmentation)
	{
		var header = segmentation.HeaderBlock;
		var summary = GetSectionBody(segmentation, CvImportSectionId.Summary);
		var contact = GetSectionBody(segmentation, CvImportSectionId.Contact);
		var combined = JoinNonEmpty(header, contact, summary);
		return string.IsNullOrWhiteSpace(combined) ? normalizedText : combined;
	}

	private static string GetSectionBody(CvSegmentationResult segmentation, CvImportSectionId sectionId) =>
		segmentation.SectionBodies.TryGetValue(sectionId, out var body) ? body : string.Empty;

	private static string JoinNonEmpty(params string[] parts) =>
		string.Join("\n\n", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
}
