using System.Text.Json.Serialization;

namespace RusalProject.Models.DTOs.Collab;

public class InviteCollaboratorRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class CollabInvitePayloadDto
{
    [JsonPropertyName("inviteId")]
    public Guid InviteId { get; set; }

    [JsonPropertyName("documentId")]
    public Guid DocumentId { get; set; }

    [JsonPropertyName("documentName")]
    public string DocumentName { get; set; } = string.Empty;

    [JsonPropertyName("inviterName")]
    public string InviterName { get; set; } = string.Empty;

    [JsonPropertyName("notificationId")]
    public Guid NotificationId { get; set; }
}

public class DocumentCollaboratorListItemDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class CollabStatusChangePayloadDto
{
    [JsonPropertyName("documentId")]
    public Guid DocumentId { get; set; }

    [JsonPropertyName("documentName")]
    public string DocumentName { get; set; } = string.Empty;

    [JsonPropertyName("collaboratorName")]
    public string CollaboratorName { get; set; } = string.Empty;
}
