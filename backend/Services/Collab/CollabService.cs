using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RusalProject.Models.DTOs.Collab;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;

namespace RusalProject.Services.Collab;

public class CollabService : ICollabService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CollabService> _logger;

    public CollabService(ApplicationDbContext db, ILogger<CollabService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InviteByEmailAsync(Guid documentId, Guid inviterUserId, string email, CancellationToken ct = default)
    {
        var document = await _db.DocumentLinks
            .Include(d => d.Creator)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatorId == inviterUserId, ct);

        if (document == null)
            throw new InvalidOperationException("Document not found or access denied");

        var normalized = email.Trim();
        if (normalized.Length == 0)
            throw new InvalidOperationException("Email is required");

        var invitee = await _db.Users.FirstOrDefaultAsync(
            u => EF.Functions.ILike(u.Email, normalized),
            ct);

        if (invitee == null)
            throw new InvalidOperationException("Пользователь с таким email не найден");

        if (invitee.Id == inviterUserId)
            throw new InvalidOperationException("Нельзя пригласить самого себя");

        var inviter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == inviterUserId, ct);
        var inviterName = inviter?.Name ?? "Пользователь";

        var existing = await _db.DocumentCollaborators.FirstOrDefaultAsync(
            c => c.DocumentId == documentId && c.UserId == invitee.Id,
            ct);

        if (existing != null)
        {
            if (existing.Status == DocumentCollaborator.StatusAccepted)
                throw new InvalidOperationException("Пользователь уже является соавтором");
            if (existing.Status == DocumentCollaborator.StatusPending)
                throw new InvalidOperationException("Приглашение уже отправлено");
            // revoked → re-invite
            existing.Status = DocumentCollaborator.StatusPending;
            existing.InvitedBy = inviterUserId;
            existing.Role = DocumentCollaborator.RoleEditor;
        }
        else
        {
            _db.DocumentCollaborators.Add(new DocumentCollaborator
            {
                DocumentId = documentId,
                UserId = invitee.Id,
                Role = DocumentCollaborator.RoleEditor,
                Status = DocumentCollaborator.StatusPending,
                InvitedBy = inviterUserId,
            });
        }

        await _db.SaveChangesAsync(ct);

        var row = await _db.DocumentCollaborators.FirstAsync(
            c => c.DocumentId == documentId && c.UserId == invitee.Id,
            ct);

        var notifId = Guid.NewGuid();
        var payload = new CollabInvitePayloadDto
        {
            InviteId = row.Id,
            DocumentId = document.Id,
            DocumentName = document.Name,
            InviterName = inviterName,
            NotificationId = notifId,
        };

        _db.UserNotifications.Add(new UserNotification
        {
            Id = notifId,
            UserId = invitee.Id,
            Type = UserNotification.TypeCollabInvite,
            PayloadJson = JsonSerializer.Serialize(payload),
        });
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Collab invite: document {DocId} invitee {Invitee} by {Inviter}",
            documentId,
            invitee.Id,
            inviterUserId);
    }

    public async Task AcceptInviteAsync(Guid inviteId, Guid userId, CancellationToken ct = default)
    {
        var row = await _db.DocumentCollaborators
            .FirstOrDefaultAsync(c => c.Id == inviteId && c.UserId == userId, ct);

        if (row == null)
            throw new InvalidOperationException("Приглашение не найдено");

        if (row.Status != DocumentCollaborator.StatusPending)
            throw new InvalidOperationException("Приглашение недействительно");

        row.Status = DocumentCollaborator.StatusAccepted;

        var notifications = await _db.UserNotifications
            .Where(n => n.UserId == userId && n.Type == UserNotification.TypeCollabInvite)
            .ToListAsync(ct);

        foreach (var n in notifications)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<CollabInvitePayloadDto>(n.PayloadJson);
                if (payload?.InviteId == inviteId)
                    n.ReadAt = DateTime.UtcNow;
            }
            catch
            {
                // ignore malformed
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeclineInviteAsync(Guid inviteId, Guid userId, CancellationToken ct = default)
    {
        var row = await _db.DocumentCollaborators
            .FirstOrDefaultAsync(c => c.Id == inviteId && c.UserId == userId, ct);

        if (row == null)
            throw new InvalidOperationException("Приглашение не найдено");

        if (row.Status != DocumentCollaborator.StatusPending)
            throw new InvalidOperationException("Приглашение недействительно");

        row.Status = DocumentCollaborator.StatusRevoked;

        var notifications = await _db.UserNotifications
            .Where(n => n.UserId == userId && n.Type == UserNotification.TypeCollabInvite)
            .ToListAsync(ct);

        foreach (var n in notifications)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<CollabInvitePayloadDto>(n.PayloadJson);
                if (payload?.InviteId == inviteId)
                    n.ReadAt = DateTime.UtcNow;
            }
            catch
            {
                // ignore
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DocumentCollaboratorListItemDto>> ListCollaboratorsAsync(
        Guid documentId,
        Guid ownerUserId,
        CancellationToken ct = default)
    {
        var isOwner = await _db.DocumentLinks.AnyAsync(
            d => d.Id == documentId && d.CreatorId == ownerUserId,
            ct);
        if (!isOwner)
            throw new InvalidOperationException("Access denied");

        return await _db.DocumentCollaborators
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.DocumentId == documentId && c.Status != DocumentCollaborator.StatusRevoked)
            .Select(c => new DocumentCollaboratorListItemDto
            {
                UserId = c.UserId,
                Email = c.User!.Email,
                Name = c.User.Name,
                Status = c.Status,
            })
            .ToListAsync(ct);
    }

    public async Task RevokeCollaboratorAsync(
        Guid documentId,
        Guid ownerUserId,
        Guid collaboratorUserId,
        CancellationToken ct = default)
    {
        var isOwner = await _db.DocumentLinks.AnyAsync(
            d => d.Id == documentId && d.CreatorId == ownerUserId,
            ct);
        if (!isOwner)
            throw new InvalidOperationException("Access denied");

        if (collaboratorUserId == ownerUserId)
            throw new InvalidOperationException("Invalid collaborator");

        var row = await _db.DocumentCollaborators.FirstOrDefaultAsync(
            c => c.DocumentId == documentId && c.UserId == collaboratorUserId,
            ct);

        if (row == null)
            return;

        row.Status = DocumentCollaborator.StatusRevoked;
        await _db.SaveChangesAsync(ct);
    }
}
