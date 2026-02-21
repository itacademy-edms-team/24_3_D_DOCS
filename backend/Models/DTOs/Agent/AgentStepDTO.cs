namespace RusalProject.Models.DTOs.Agent;

public class AgentStepDTO
{
    public int StepNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<ToolCallDTO>? ToolCalls { get; set; }
    public string? ToolResult { get; set; }
    public List<DocumentEntityChangeDTO>? DocumentChanges { get; set; }
}
