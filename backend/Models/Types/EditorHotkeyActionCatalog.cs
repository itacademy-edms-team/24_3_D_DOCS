namespace RusalProject.Models.Types;

/// <summary>Стабильные идентификаторы операций редактора для хоткеев (совпадают с фронтенд-каталогом).</summary>
public static class EditorHotkeyActionCatalog
{
	public const string ToggleBold = "toggle_bold";
	public const string ToggleItalic = "toggle_italic";
	public const string ToggleList = "toggle_list";
	public const string ToggleHighlight = "toggle_highlight";
	public const string ToggleHeading = "toggle_heading";
	public const string InsertFormula = "insert_formula";
	public const string AddCaptionTable = "add_caption_table";
	public const string AddCaptionFormula = "add_caption_formula";
	public const string AddCaptionImage = "add_caption_image";
	public const string InsertTable = "insert_table";
	public const string ImageUploadInsert = "image_upload_insert";
	public const string ImageUploadCrop = "image_upload_crop";

	public static readonly IReadOnlyList<string> All =
	[
		ToggleBold,
		ToggleItalic,
		ToggleList,
		ToggleHighlight,
		ToggleHeading,
		InsertFormula,
		AddCaptionTable,
		AddCaptionFormula,
		AddCaptionImage,
		InsertTable,
		ImageUploadInsert,
		ImageUploadCrop,
	];

	public static readonly HashSet<string> AllSet = new(All, StringComparer.Ordinal);
}
