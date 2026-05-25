namespace ReVitae.Core.Ai.Cv;

public sealed record AiCvCompletionContext(
	AiCvTaskKind Task,
	string CurrentText,
	string? JobTitle = null,
	string? Company = null,
	string? ProjectName = null,
	string? ProfessionalTitle = null);
