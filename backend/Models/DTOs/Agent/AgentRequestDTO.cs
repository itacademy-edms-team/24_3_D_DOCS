using RusalProject.Models.Types;

namespace RusalProject.Models.DTOs.Agent;

public class AgentRequestDTO
{
    public Guid DocumentId { get; set; }
    public string UserMessage { get; set; } = string.Empty;
    public int? StartLine { get; set; }
    public int? EndLine { get; set; }
    public Guid? ChatId { get; set; }
    public AgentMode? Mode { get; set; }
}
