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

    [Column("conversion_log")]
    public string? ConversionLog { get; set; } // Лог ошибок конвертации

    [Column("profile_id")]
    public Guid? ProfileId { get; set; } // FK к SchemaLink (профиль стилей)

    [Column("title_page_id")]
    public Guid? TitlePageId { get; set; } // FK к TitlePage

    [Column("metadata")]
    public string? Metadata { get; set; } // JSONB - переменные титульного листа (Author, Year, Title, Group, etc.)

    [Column("is_archived")]
    public bool IsArchived { get; set; } = false;

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; } // Soft delete

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CreatorId")]
    public virtual User? Creator { get; set; }

    [ForeignKey("ProfileId")]
    public virtual SchemaLink? Profile { get; set; }

    [ForeignKey("TitlePageId")]
    public virtual TitlePage? TitlePage { get; set; }
}
