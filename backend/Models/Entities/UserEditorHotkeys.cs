using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RusalProject.Models.Entities;

[Table("user_editor_hotkeys")]
public class UserEditorHotkeys
{
	[Key]
	[Column("user_id")]
	public Guid UserId { get; set; }

	[Column("bindings")]
	public string BindingsJson { get; set; } = "{}";

	[Column("updated_at")]
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	[ForeignKey(nameof(UserId))]
	public virtual User? User { get; set; }
}
