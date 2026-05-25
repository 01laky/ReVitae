namespace ReVitae.Core.Cv.Links;

public static class LinkPreviewFormatter
{
	public static string FormatLine(LinkEntry entry)
	{
		var label = entry.Label.Trim();
		var url = entry.Url.Trim();
		var line = string.IsNullOrEmpty(label) ? url : $"{label}: {url}";

		if (!string.IsNullOrWhiteSpace(entry.Note))
		{
			line += $" — {entry.Note.Trim()}";
		}

		return line;
	}
}
