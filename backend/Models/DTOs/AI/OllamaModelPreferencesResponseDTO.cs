namespace RusalProject.Models.DTOs.AI;

public class OllamaModelPreferencesResponseDTO
{
    public string? AgentModelName { get; set; }

    public string? AttachmentTextModelName { get; set; }

    public string? VisionModelName { get; set; }

    public string EffectiveAgentModel { get; set; } = string.Empty;

    public string EffectiveAttachmentTextModel { get; set; } = string.Empty;

    public string EffectiveVisionModel { get; set; } = string.Empty;
}
