namespace RusalProject.Models.DTOs.Agent;

public class AgentSourceIngestResponseDTO
{
    public Guid SourceSessionId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public List<AgentSourcePartSummaryDTO> Parts { get; set; } = new();
    public string? Notes { get; set; }
}

public class AgentSourcePartSummaryDTO
{
    public int Index { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
