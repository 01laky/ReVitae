using System.Reflection;

namespace ReVitae.Core;

public static class AppVersion
{
	private static Assembly? _applicationAssembly;

	public static void Initialize(Assembly applicationAssembly)
	{
		_applicationAssembly = applicationAssembly ?? throw new ArgumentNullException(nameof(applicationAssembly));
	}

	public static string Current => GetSemVerBase(Informational);

	public static string Informational => ResolveInformationalVersion();

	public static bool IsPreRelease =>
		HasPreReleaseLabel(Informational) || GetMajorVersion(Current) == 0;

	public static string Author =>
		ResolveApplicationAssembly()
			.GetCustomAttribute<AssemblyCompanyAttribute>()
			?.Company
		?? "Ladislav Kostolny";

	public static string AuthorEmail =>
		ResolveApplicationAssembly()
			.GetCustomAttributes<AssemblyMetadataAttribute>()
			.FirstOrDefault(metadata => metadata.Key == "AuthorEmail")
			?.Value
		?? "01laky@gmail.com";

	private static Assembly ResolveApplicationAssembly() =>
		_applicationAssembly ?? Assembly.GetExecutingAssembly();

	private static string ResolveInformationalVersion()
	{
		var assembly = ResolveApplicationAssembly();
		var informational = assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
			?.InformationalVersion;

		if (!string.IsNullOrWhiteSpace(informational))
		{
			return informational.Trim();
		}

		return assembly.GetName().Version?.ToString() ?? "0.0.0";
	}

	private static string GetSemVerBase(string informationalVersion)
	{
		var version = informationalVersion.Trim();
		var plusIndex = version.IndexOf('+', StringComparison.Ordinal);
		if (plusIndex >= 0)
		{
			version = version[..plusIndex];
		}

		return version;
	}

	private static bool HasPreReleaseLabel(string informationalVersion)
	{
		var version = GetSemVerBase(informationalVersion);
		return version.Contains('-', StringComparison.Ordinal);
	}

	private static int GetMajorVersion(string semVerBase)
	{
		var majorToken = semVerBase.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.FirstOrDefault();

		return int.TryParse(majorToken, out var major) ? major : 0;
	}
}
