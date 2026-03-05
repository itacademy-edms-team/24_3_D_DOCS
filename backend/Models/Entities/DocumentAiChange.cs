using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RusalProject.Models.Entities;

[Table("document_ai_changes")]
public class DocumentAiChange
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("document_id")]
    public Guid DocumentId { get; set; }

    [Required]
    [Column("change_id")]
    [MaxLength(64)]
    public string ChangeId { get; set; } = string.Empty;

    [Required]
    [Column("change_type")]
    [MaxLength(16)]
    public string ChangeType { get; set; } = string.Empty;

    [Required]
    [Column("entity_type")]
    [MaxLength(64)]
    public string EntityType { get; set; } = "markdown";

    [Column("start_line")]
    public int StartLine { get; set; }

    [Column("end_line")]
    public int? EndLine { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("group_id")]
    [MaxLength(64)]
    public string? GroupId { get; set; }

    [Column("change_order")]
    public int? Order { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("DocumentId")]
    public virtual DocumentLink? Document { get; set; }
}
