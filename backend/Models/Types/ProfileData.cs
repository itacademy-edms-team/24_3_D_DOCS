using System.Text.Json.Serialization;

namespace RusalProject.Models.Types;

public class ProfileData
{
    public PageSettings PageSettings { get; set; } = new();
    
    [JsonPropertyName("entityStyles")]
    public Dictionary<string, EntityStyle> EntityStyles { get; set; } = new();
    
    public HeadingNumberingSettings? HeadingNumbering { get; set; }
}

public class HeadingNumberingSettings
{
    public Dictionary<int, HeadingTemplate> Templates { get; set; } = new();
}

public class HeadingTemplate
{
    public string Format { get; set; } = "{n} {content}";
    public bool Enabled { get; set; } = false;
}

// Типы элементов для стилей
public static class EntityTypes
{
    public const string Paragraph = "paragraph";
    public const string Heading = "heading";
    public const string Heading1 = "heading-1";
    public const string Heading2 = "heading-2";
    public const string Heading3 = "heading-3";
    public const string Heading4 = "heading-4";
    public const string Heading5 = "heading-5";
    public const string Heading6 = "heading-6";
    public const string Image = "image";
    public const string ImageCaption = "image-caption";
    public const string OrderedList = "ordered-list";
    public const string UnorderedList = "unordered-list";
    public const string Table = "table";
    public const string TableCaption = "table-caption";
    public const string Formula = "formula";
    public const string FormulaCaption = "formula-caption";
    public const string CodeBlock = "code-block";
    public const string Blockquote = "blockquote";
}
