using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RusalProject.Models.Types;

namespace RusalProject.Models.Entities;

[Table("document_blocks")]
public class DocumentBlock
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("document_id")]
    public Guid DocumentId { get; set; }

    [Required]
    [Column("block_type")]
    [MaxLength(50)]
    public string BlockType { get; set; } = string.Empty;

    [Required]
    [Column("start_line")]
    public int StartLine { get; set; }

    [Required]
    [Column("end_line")]
    public int EndLine { get; set; }

    [Required]
    [Column("raw_text")]
    public string RawText { get; set; } = string.Empty;

    [Required]
    [Column("normalized_text")]
    public string NormalizedText { get; set; } = string.Empty;

    [Required]
    [Column("content_hash")]
    [MaxLength(64)]
    public string ContentHash { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    [ForeignKey("DocumentId")]
    public virtual DocumentLink? Document { get; set; }

    public virtual BlockEmbedding? Embedding { get; set; }
}
