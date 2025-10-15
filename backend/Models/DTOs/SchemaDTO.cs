namespace RusalProject.Models.DTOs;

public class SchemaDTO
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public string SchemaData { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserDTO? Creator { get; set; }
}

public class CreateSchemaDTO
{
    public Guid CreatorId { get; set; }
    public string SchemaData { get; set; } = string.Empty;
}

public class UpdateSchemaDTO
{
    public string SchemaData { get; set; } = string.Empty;
}

