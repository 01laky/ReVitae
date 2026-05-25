namespace ReVitae.Core.Cv.Certificates;

using System.Globalization;

public sealed class CertificateEntry
{
	public CertificateEntry()
	{
		Id = Guid.NewGuid().ToString("N");
	}

	public CertificateEntry(string id)
	{
		Id = id;
	}

	public string Id { get; }

	public string Name { get; set; } = string.Empty;

	public string Issuer { get; set; } = string.Empty;

	public int? IssueMonth { get; set; }

	public int? IssueYear { get; set; }

	public int? ExpirationMonth { get; set; }

	public int? ExpirationYear { get; set; }

	public string CredentialId { get; set; } = string.Empty;

	public string CredentialUrl { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public bool HasUserInput()
	{
		if (IssueMonth.HasValue || IssueYear.HasValue || ExpirationMonth.HasValue || ExpirationYear.HasValue)
		{
			return true;
		}

		return HasText(Name)
			|| HasText(Issuer)
			|| HasText(CredentialId)
			|| HasText(CredentialUrl)
			|| HasText(Description);
	}

	public CertificateEntry Duplicate()
	{
		return new CertificateEntry
		{
			Name = Name,
			Issuer = Issuer,
			IssueMonth = IssueMonth,
			IssueYear = IssueYear,
			ExpirationMonth = ExpirationMonth,
			ExpirationYear = ExpirationYear,
			CredentialId = CredentialId,
			CredentialUrl = CredentialUrl,
			Description = Description
		};
	}

	public IReadOnlyDictionary<string, string?> ToFieldValues()
	{
		return new Dictionary<string, string?>(StringComparer.Ordinal)
		{
			[CertificatesFieldKeys.Build(Id, CertificatesFieldKeys.Name)] = Name,
			[CertificatesFieldKeys.Build(Id, CertificatesFieldKeys.Issuer)] = Issuer,
			[CertificatesFieldKeys.Build(Id, CertificatesFieldKeys.IssueMonth)] = IssueMonth?.ToString(CultureInfo.InvariantCulture),
			[CertificatesFieldKeys.Build(Id, CertificatesFieldKeys.IssueYear)] = IssueYear?.ToString(CultureInfo.InvariantCulture),
			[CertificatesFieldKeys.Build(Id, CertificatesFieldKeys.ExpirationMonth)] = ExpirationMonth?.ToString(CultureInfo.InvariantCulture),
			[CertificatesFieldKeys.Build(Id, CertificatesFieldKeys.ExpirationYear)] = ExpirationYear?.ToString(CultureInfo.InvariantCulture),
			[CertificatesFieldKeys.Build(Id, CertificatesFieldKeys.CredentialId)] = CredentialId,
			[CertificatesFieldKeys.Build(Id, CertificatesFieldKeys.CredentialUrl)] = CredentialUrl,
			[CertificatesFieldKeys.Build(Id, CertificatesFieldKeys.Description)] = Description
		};
	}

	public string BuildHeaderSummary()
	{
		var name = string.IsNullOrWhiteSpace(Name) ? "-" : Name.Trim();
		var issuer = string.IsNullOrWhiteSpace(Issuer) ? "-" : Issuer.Trim();
		var issueDate = FormatPartialDate(IssueMonth, IssueYear);
		return string.IsNullOrEmpty(issueDate)
			? $"{name} · {issuer}"
			: $"{name} · {issuer} · {issueDate}";
	}

	private static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);

	private static string FormatPartialDate(int? month, int? year)
	{
		if (month is null || year is null)
		{
			return string.Empty;
		}

		return $"{month.Value:D2} / {year.Value}";
	}
}
