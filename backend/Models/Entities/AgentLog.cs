using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RusalProject.Models.Entities;

[Table("agent_logs")]
public class AgentLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("document_id")]
    public Guid DocumentId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("chat_session_id")]
    public Guid? ChatSessionId { get; set; }

    [Required]
    [Column("log_type")]
    [MaxLength(50)]
    public string LogType { get; set; } = string.Empty; // "user_message", "llm_request", "llm_response", "tool_call", "tool_result", "document_change", "status_check", "completion"

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("metadata")]
    public string? Metadata { get; set; } // JSON string with additional data

    [Required]
    [Column("iteration_number")]
    public int IterationNumber { get; set; }

    [Column("step_number")]
    public int? StepNumber { get; set; }

    [Required]
    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("DocumentId")]
    public virtual DocumentLink? Document { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("ChatSessionId")]
    public virtual ChatSession? ChatSession { get; set; }
}
