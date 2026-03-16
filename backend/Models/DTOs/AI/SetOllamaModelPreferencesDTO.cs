using System.ComponentModel.DataAnnotations;

namespace RusalProject.Models.DTOs.AI;

public class SetOllamaModelPreferencesDTO
{
    [MaxLength(512)]
    public string? AgentModelName { get; set; }

    [MaxLength(512)]
    public string? AttachmentTextModelName { get; set; }

    [MaxLength(512)]
    public string? VisionModelName { get; set; }
}
