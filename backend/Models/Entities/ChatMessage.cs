using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RusalProject.Models.Entities;

[Table("chat_messages")]
public class ChatMessage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("chat_session_id")]
    public Guid ChatSessionId { get; set; }

    [Required]
    [Column("role")]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("step_number")]
    public int? StepNumber { get; set; }

    [Column("tool_calls")]
    public string? ToolCalls { get; set; } // JSON string

    [Column("client_message_id")]
    [MaxLength(128)]
    public string? ClientMessageId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ChatSessionId")]
    public virtual ChatSession? ChatSession { get; set; }
}
