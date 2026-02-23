using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RusalProject.Models.Entities;

[Table("chat_context_files")]
public class ChatContextFile
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("chat_session_id")]
    public Guid ChatSessionId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("file_name")]
    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [Column("content_type")]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    [Column("storage_path")]
    public string StoragePath { get; set; } = string.Empty;

    [Column("processed_content", TypeName = "text")]
    public string? ProcessedContent { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ChatSessionId")]
    public virtual ChatSession? ChatSession { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
