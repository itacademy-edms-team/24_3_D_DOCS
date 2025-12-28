namespace RusalProject.Models.DTOs.Agent;

public class AgentResponseDTO
{
    public string FinalMessage { get; set; } = string.Empty;
    public List<AgentStepDTO> Steps { get; set; } = new();
    public bool IsComplete { get; set; }
}
