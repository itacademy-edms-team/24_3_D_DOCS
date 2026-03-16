using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RusalProject.Models.Entities;

[Table("user_ollama_model_preferences")]
public class UserOllamaModelPreferences
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("agent_model_name")]
    public string? AgentModelName { get; set; }

    [Column("attachment_text_model_name")]
    public string? AttachmentTextModelName { get; set; }

    [Column("vision_model_name")]
    public string? VisionModelName { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
