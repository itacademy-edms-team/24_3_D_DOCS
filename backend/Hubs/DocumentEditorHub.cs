using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;

namespace RusalProject.Hubs;

[Authorize]
public class DocumentEditorHub : Hub
{
    private readonly ApplicationDbContext _db;

    public DocumentEditorHub(ApplicationDbContext db)
    {
        _db = db;
    }

    public static string DocumentGroupName(Guid documentId) => $"doc:{documentId}";

    private Guid? GetUserId()
    {
        var raw = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public async Task JoinDocument(string documentIdRaw)
    {
        if (!Guid.TryParse(documentIdRaw, out var documentId))
        {
            throw new HubException("Некорректный идентификатор документа");
        }

        var userId = GetUserId();
        if (userId == null)
        {
            throw new HubException("Не авторизован");
        }

        var allowed = await _db.DocumentLinks.AnyAsync(
            d => d.Id == documentId
                && (d.CreatorId == userId.Value
                    || d.Collaborators.Any(c =>
                        c.UserId == userId.Value && c.Status == DocumentCollaborator.StatusAccepted)));

        if (!allowed)
        {
            throw new HubException("Нет доступа к документу");
        }

        if (Context.Items.TryGetValue("activeDocumentId", out var prev)
            && prev is string prevStr
            && Guid.TryParse(prevStr, out var prevId)
            && prevId != documentId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, DocumentGroupName(prevId));
        }

        Context.Items["activeDocumentId"] = documentId.ToString();
        await Groups.AddToGroupAsync(Context.ConnectionId, DocumentGroupName(documentId));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("activeDocumentId", out var raw)
            && raw is string s
            && Guid.TryParse(s, out var documentId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, DocumentGroupName(documentId));
        }

        await base.OnDisconnectedAsync(exception);
    }
}
