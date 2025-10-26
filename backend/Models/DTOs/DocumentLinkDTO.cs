namespace RusalProject.Models.DTOs;

public class DocumentLinkDTO
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MdMinioPath { get; set; } = string.Empty;
    public string? PdfMinioPath { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ConversionLog { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserDTO? Creator { get; set; }
}

public class CreateDocumentLinkDTO
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MdMinioPath { get; set; } = string.Empty;
}

public class UpdateDocumentLinkDTO
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ConvertDocumentDTO
{
    public Guid SchemaLinkId { get; set; }
}
