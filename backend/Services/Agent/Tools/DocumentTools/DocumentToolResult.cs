using RusalProject.Models.DTOs.Agent;

namespace RusalProject.Services.Agent.Tools.DocumentTools;

public class DocumentToolResult
{
    public string ResultMessage { get; set; } = string.Empty;
    public List<DocumentEntityChangeDTO> DocumentChanges { get; set; } = new();
}
