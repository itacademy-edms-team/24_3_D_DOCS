namespace RusalProject.Models.DTOs.Agent;

public class AgentLogDTO
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ChatSessionId { get; set; }
    public string LogType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public int IterationNumber { get; set; }
    public int? StepNumber { get; set; }
    public DateTime Timestamp { get; set; }
}
