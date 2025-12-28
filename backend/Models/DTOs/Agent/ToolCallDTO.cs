namespace RusalProject.Models.DTOs.Agent;

public class ToolCallDTO
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Arguments { get; set; } = new();
    public string? Result { get; set; }
}
