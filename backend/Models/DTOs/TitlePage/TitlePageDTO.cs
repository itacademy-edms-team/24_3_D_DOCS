using System.ComponentModel.DataAnnotations;
using RusalProject.Models.Types;

namespace RusalProject.Models.DTOs.TitlePage;

public class TitlePageDTO
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TitlePageWithDataDTO : TitlePageDTO
{
    public TitlePageData Data { get; set; } = new();
}

public class CreateTitlePageDTO
{
    [Required(ErrorMessage = "Название обязательно")]
    [MaxLength(255, ErrorMessage = "Название не должно превышать 255 символов")]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000, ErrorMessage = "Описание не должно превышать 1000 символов")]
    public string? Description { get; set; }
    
    public TitlePageData? Data { get; set; }
}

public class UpdateTitlePageDTO
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public TitlePageData? Data { get; set; }
}
