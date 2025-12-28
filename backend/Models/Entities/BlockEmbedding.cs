using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RusalProject.Models.Entities;

[Table("block_embeddings")]
public class BlockEmbedding
{
    [Key]
    [Column("block_id")]
    public Guid BlockId { get; set; }

    [Required]
    [Column("embedding")]
    public float[] Embedding { get; set; } = Array.Empty<float>();

    [Required]
    [Column("model")]
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [Column("version")]
    public int Version { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("BlockId")]
    public virtual DocumentBlock? Block { get; set; }
}
