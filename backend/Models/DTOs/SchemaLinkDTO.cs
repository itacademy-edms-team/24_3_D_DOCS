namespace RusalProject.Models.DTOs;

public class SchemaLinkDTO
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MinioPath { get; set; } = string.Empty;
    public string? PandocOptions { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserDTO? Creator { get; set; }
}

public class CreateSchemaLinkDTO
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MinioPath { get; set; } = string.Empty;
    public string? PandocOptions { get; set; }
    public bool IsPublic { get; set; } = false;
}

public class UpdateSchemaLinkDTO
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PandocOptions { get; set; }
    public bool IsPublic { get; set; }
}
