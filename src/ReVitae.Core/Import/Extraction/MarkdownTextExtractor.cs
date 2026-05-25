using System.Text;
using Markdig;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Extraction;

public sealed class MarkdownTextExtractor : ICvTextExtractor
{
	private static readonly MarkdownPipeline Pipeline =
		new MarkdownPipelineBuilder().Build();

	public CvTextExtractionResult Extract(string filePath)
	{
		if (ImportExtractorGuards.TryRejectMissing(filePath, out var fail))
		{
			return fail;
		}

		try
		{
			var markdown = File.ReadAllText(filePath, new UTF8Encoding(false, false));
			if (string.IsNullOrWhiteSpace(markdown))
			{
				return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorEmptyDocument);
			}

			var plaintext = Markdown.ToPlainText(markdown, Pipeline).Trim();
			if (string.IsNullOrWhiteSpace(plaintext))
			{
				return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorEmptyDocument);
			}

			return new CvTextExtractionResult(true, plaintext, null);
		}
		catch (Exception)
		{
			return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
		}
	}
}
