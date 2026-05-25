using System.Text.RegularExpressions;
using ReVitae.Core.Cv;
using ReVitae.Core.Cv.AdditionalInformation;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import.Patterns;
using ReVitae.Core.Import.Pdf;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Extraction;

internal static partial class ImportFieldExtractionCore
{
	internal static IReadOnlyList<CertificateEntry> ExtractCertificates(string body)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return [];
		}

		var entries = new List<CertificateEntry>();
		foreach (var block in SplitCertificateBlocks(body))
		{
			var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (lines.Length == 0)
			{
				continue;
			}

			var entry = new CertificateEntry();
			ParsedDateRange? issueDate = null;
			ParsedDateRange? expirationDate = null;
			var pendingLines = new List<string>();

			foreach (var line in lines)
			{
				if (TryParseCertificateDetailLine(line, entry, ref issueDate, ref expirationDate))
				{
					continue;
				}

				pendingLines.Add(line);
			}

			AssignCertificateHeader(entry, pendingLines);
			ApplyCertificateIssueDate(entry, issueDate);
			ApplyCertificateExpirationDate(entry, expirationDate);

			if (entry.HasUserInput())
			{
				entries.Add(entry);
			}
		}

		return entries;
	}

	internal static bool TryParseCertificateDetailLine(
		string line,
		CertificateEntry entry,
		ref ParsedDateRange? issueDate,
		ref ParsedDateRange? expirationDate)
	{
		if (TryParseLabeledIssueDateLine(line, out var labeledIssueDate))
		{
			issueDate = labeledIssueDate;
			return true;
		}

		if (TryParseLabeledExpirationDateLine(line, out var labeledExpirationDate))
		{
			expirationDate = labeledExpirationDate;
			return true;
		}

		if (TryParseLabeledCertificateValue(line, "issuing organization", out var issuer)
			|| TryParseLabeledCertificateValue(line, "issuer", out issuer))
		{
			entry.Issuer = issuer;
			return true;
		}

		if (TryParseLabeledCertificateValue(line, "credential id", out var credentialId))
		{
			entry.CredentialId = credentialId;
			return true;
		}

		if (TryParseLabeledCertificateValue(line, "credential url", out var credentialUrl))
		{
			entry.CredentialUrl = NormalizeImportedUrl(credentialUrl);
			return true;
		}

		if (issueDate is null
			&& !LooksLikeCertificateMetadataLine(line)
			&& !LooksLikeInlineCertificateHeaderLine(line)
			&& DateRangeParser.TryParse(line, out var parsedIssueDate))
		{
			issueDate = parsedIssueDate;
			return true;
		}

		return false;
	}

	internal static bool LooksLikeInlineCertificateHeaderLine(string line) =>
		line.Contains('·', StringComparison.Ordinal)
		&& (line.StartsWith("Professional Certification #", StringComparison.OrdinalIgnoreCase)
			|| CertificateEntryHeader.IsMatch(line));

	internal static void AssignCertificateHeader(CertificateEntry entry, List<string> pendingLines)
	{
		if (pendingLines.Count == 0)
		{
			return;
		}

		var headerLine = pendingLines[0];
		pendingLines.RemoveAt(0);

		if (TryParseInlineCertificateHeader(headerLine, entry, out var trailingDescription))
		{
			if (!string.IsNullOrWhiteSpace(trailingDescription))
			{
				pendingLines.Insert(0, trailingDescription);
			}
		}
		else
		{
			entry.Name = headerLine;
			if (pendingLines.Count > 0 && string.IsNullOrWhiteSpace(entry.Issuer))
			{
				if (TryParseLabeledCertificateValue(pendingLines[0], "issuing organization", out var labeledIssuer)
					|| TryParseLabeledCertificateValue(pendingLines[0], "issuer", out labeledIssuer))
				{
					entry.Issuer = labeledIssuer;
					pendingLines.RemoveAt(0);
				}
				else if (!LooksLikeCertificateMetadataLine(pendingLines[0]))
				{
					entry.Issuer = pendingLines[0];
					pendingLines.RemoveAt(0);
				}
			}
		}

		if (pendingLines.Count > 0)
		{
			entry.Description = string.IsNullOrWhiteSpace(entry.Description)
				? string.Join('\n', pendingLines).Trim()
				: $"{entry.Description}\n{string.Join('\n', pendingLines).Trim()}".Trim();
		}
	}

	internal static bool TryParseInlineCertificateHeader(
		string line,
		CertificateEntry entry,
		out string trailingDescription)
	{
		trailingDescription = string.Empty;
		if (!line.Contains('·', StringComparison.Ordinal))
		{
			return false;
		}

		var segments = line.Split('·', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (segments.Length == 0)
		{
			return false;
		}

		entry.Name = segments[0];
		var segmentIndex = 1;
		if (segmentIndex < segments.Length && !LooksLikeCertificateDateSegment(segments[segmentIndex]))
		{
			entry.Issuer = segments[segmentIndex++];
		}

		ParsedDateRange? issueDate = null;
		ParsedDateRange? expirationDate = null;
		for (; segmentIndex < segments.Length; segmentIndex++)
		{
			var segment = segments[segmentIndex];
			if (segment.StartsWith("Valid until", StringComparison.OrdinalIgnoreCase)
				|| segment.StartsWith("Valid through", StringComparison.OrdinalIgnoreCase))
			{
				var expirationValue = segment[(segment.IndexOf(' ', StringComparison.Ordinal) + 1)..].Trim();
				if (DateRangeParser.TryParse(expirationValue, out var parsedExpiration))
				{
					expirationDate = parsedExpiration;
				}

				continue;
			}

			if (issueDate is null && DateRangeParser.TryParse(segment, out var parsedIssue))
			{
				issueDate = parsedIssue;
			}
			else if (string.IsNullOrWhiteSpace(trailingDescription))
			{
				trailingDescription = segment;
			}
			else
			{
				trailingDescription = $"{trailingDescription} · {segment}";
			}
		}

		if (issueDate is not null)
		{
			ApplyCertificateIssueDate(entry, issueDate);
		}

		if (expirationDate is not null)
		{
			ApplyCertificateExpirationDate(entry, expirationDate);
		}

		return true;
	}

	internal static bool LooksLikeCertificateDateSegment(string segment) =>
		DateRangeParser.TryParse(segment, out _)
		|| segment.StartsWith("Valid until", StringComparison.OrdinalIgnoreCase)
		|| segment.StartsWith("Valid through", StringComparison.OrdinalIgnoreCase);

	internal static bool LooksLikeCertificateMetadataLine(string line) =>
		line.Contains(':', StringComparison.Ordinal)
		|| line.Contains("Credential", StringComparison.OrdinalIgnoreCase)
		|| line.Contains("Issuing", StringComparison.OrdinalIgnoreCase)
		|| line.Contains("Focus area", StringComparison.OrdinalIgnoreCase);

	internal static bool TryParseLabeledCertificateValue(string line, string labelKeyword, out string value)
	{
		value = string.Empty;
		var labeled = CvImportPatterns.LabeledValue.Match(line);
		if (!labeled.Success
			|| !labeled.Groups["label"].Value.Contains(labelKeyword, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		value = labeled.Groups["value"].Value.Trim();
		return !string.IsNullOrWhiteSpace(value);
	}

}
