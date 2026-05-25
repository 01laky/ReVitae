namespace ReVitae.Core.Ai.Ollama;

public interface IOllamaServeSupervisor
{
	bool TryStartManagedServe();
}

public sealed class DefaultOllamaServeSupervisor : IOllamaServeSupervisor
{
	public static DefaultOllamaServeSupervisor Instance { get; } = new();

	public bool TryStartManagedServe() =>
		OllamaServeSupervisor.TryStartManagedServe();
}

public interface IOllamaProcessLauncher
{
	bool TryStartProcess(string fileName, IReadOnlyList<string> arguments, out int? processId);
}

public sealed class DefaultOllamaProcessLauncher : IOllamaProcessLauncher
{
	public static DefaultOllamaProcessLauncher Instance { get; } = new();

	public bool TryStartProcess(string fileName, IReadOnlyList<string> arguments, out int? processId)
	{
		processId = null;
		try
		{
			var startInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = fileName,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			foreach (var argument in arguments)
			{
				startInfo.ArgumentList.Add(argument);
			}

			var process = System.Diagnostics.Process.Start(startInfo);
			if (process is null)
			{
				return false;
			}

			processId = process.Id;
			return true;
		}
		catch
		{
			return false;
		}
	}
}
