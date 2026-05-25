namespace ReVitae.Core.Ai.Providers;

public interface IAiSecretStorage
{
	string? TryGetApiKey(string providerId);

	void SaveApiKey(string providerId, string apiKey);

	void DeleteApiKey(string providerId);

	void DeleteAll();
}
