using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RusalProject.Models.Entities;

[Table("agent_source_sessions")]
public class AgentSourceSession
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("document_id")]
    public Guid DocumentId { get; set; }

    [Required]
    [Column("chat_session_id")]
    public Guid ChatSessionId { get; set; }

    [Required]
    [Column("original_file_name")]
    [MaxLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("ingest_notes")]
    [MaxLength(2000)]
    public string? IngestNotes { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("DocumentId")]
    public virtual DocumentLink? Document { get; set; }

    [ForeignKey("ChatSessionId")]
    public virtual ChatSession? ChatSession { get; set; }

    public virtual ICollection<AgentSourcePart> Parts { get; set; } = new List<AgentSourcePart>();
}
