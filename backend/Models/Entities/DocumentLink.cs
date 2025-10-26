using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RusalProject.Models.Entities;

[Table("document_links")]
public class DocumentLink
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("creator_id")]
    public Guid CreatorId { get; set; }

    [Required]
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Column("md_minio_path")]
    [MaxLength(500)]
    public string MdMinioPath { get; set; } = string.Empty;

    [Column("pdf_minio_path")]
    [MaxLength(500)]
    public string? PdfMinioPath { get; set; } // Может быть null если PDF еще не сгенерирован

    [Required]
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "draft"; // draft, processing, completed, failed

    [Column("conversion_log")]
    public string? ConversionLog { get; set; } // Лог ошибок конвертации

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CreatorId")]
    public virtual User? Creator { get; set; }
}
