namespace RusalProject.Models.DTOs;

public class EditorHotkeyChordDto
{
	public string Code { get; set; } = "";

	public bool CtrlKey { get; set; }

	public bool ShiftKey { get; set; }

	public bool AltKey { get; set; }

	public bool MetaKey { get; set; }
}

public class EditorHotkeysResponseDto
{
	/// <summary>Ключ — id действия из <see cref="Types.EditorHotkeyActionCatalog"/>.</summary>
	public Dictionary<string, EditorHotkeyChordDto?> Bindings { get; set; } = new();
}

public class SetEditorHotkeysDto
{
	public Dictionary<string, EditorHotkeyChordDto?> Bindings { get; set; } = new();
}
