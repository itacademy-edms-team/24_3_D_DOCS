using System.ComponentModel.DataAnnotations;

namespace RusalProject.Models.DTOs.AI;

public class SetOllamaApiKeyDTO
{
    [Required(ErrorMessage = "API ключ обязателен")]
    [MinLength(1)]
    public string ApiKey { get; set; } = string.Empty;
}
