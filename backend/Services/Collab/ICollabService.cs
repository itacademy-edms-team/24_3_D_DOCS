using RusalProject.Models.DTOs.Collab;

namespace RusalProject.Services.Collab;

public interface ICollabService
{
    Task InviteByEmailAsync(Guid documentId, Guid inviterUserId, string email, CancellationToken ct = default);
    Task AcceptInviteAsync(Guid inviteId, Guid userId, CancellationToken ct = default);
    Task DeclineInviteAsync(Guid inviteId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentCollaboratorListItemDto>> ListCollaboratorsAsync(
        Guid documentId,
        Guid ownerUserId,
        CancellationToken ct = default);
    Task RevokeCollaboratorAsync(Guid documentId, Guid ownerUserId, Guid collaboratorUserId, CancellationToken ct = default);
    Task LeaveCollabAsync(Guid documentId, Guid userId, CancellationToken ct = default);
}
