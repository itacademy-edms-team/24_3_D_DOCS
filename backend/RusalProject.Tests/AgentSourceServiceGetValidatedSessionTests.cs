using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RusalProject.Models.Entities;
using RusalProject.Models.Types;
using RusalProject.Provider.Database;
using RusalProject.Services.AgentSources;
using RusalProject.Services.Chat;
using RusalProject.Services.Storage;
using Xunit;

namespace RusalProject.Tests;

public class AgentSourceServiceGetValidatedSessionTests
{
    private static AgentSourceService CreateService(ApplicationDbContext ctx)
    {
        var minio = new Mock<IMinioService>();
        var chat = new Mock<IChatService>();
        var logger = new Mock<ILogger<AgentSourceService>>();
        return new AgentSourceService(ctx, minio.Object, chat.Object, logger.Object);
    }

    [Fact]
    public async Task GetValidatedSessionAsync_SessionWithoutDocument_AllowsDocumentContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new ApplicationDbContext(options);
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var anyDocumentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        ctx.Users.Add(new User
        {
            Id = userId,
            Email = "t@test.local",
            PasswordHash = "x",
            Name = "t",
        });
        ctx.ChatSessions.Add(new ChatSession
        {
            Id = chatId,
            UserId = userId,
            Title = "global",
            Scope = ChatScope.Global,
        });
        ctx.AgentSourceSessions.Add(new AgentSourceSession
        {
            Id = sessionId,
            UserId = userId,
            ChatSessionId = chatId,
            DocumentId = null,
            OriginalFileName = "a.pdf",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx);
        var result = await svc.GetValidatedSessionAsync(userId, sessionId, chatId, anyDocumentId);

        Assert.NotNull(result);
        Assert.Null(result.DocumentId);
    }

    [Fact]
    public async Task GetValidatedSessionAsync_SessionBoundToDocument_RejectsOtherDocument()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new ApplicationDbContext(options);
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var docA = Guid.NewGuid();
        var docB = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        ctx.Users.Add(new User
        {
            Id = userId,
            Email = "t2@test.local",
            PasswordHash = "x",
            Name = "t",
        });
        ctx.ChatSessions.Add(new ChatSession
        {
            Id = chatId,
            UserId = userId,
            Title = "doc chat",
            Scope = ChatScope.Document,
            DocumentId = docA,
        });
        ctx.AgentSourceSessions.Add(new AgentSourceSession
        {
            Id = sessionId,
            UserId = userId,
            ChatSessionId = chatId,
            DocumentId = docA,
            OriginalFileName = "b.pdf",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx);
        var result = await svc.GetValidatedSessionAsync(userId, sessionId, chatId, docB);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidatedSessionAsync_GlobalContext_StillRejectsDocumentBoundSession()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new ApplicationDbContext(options);
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        ctx.Users.Add(new User
        {
            Id = userId,
            Email = "t3@test.local",
            PasswordHash = "x",
            Name = "t",
        });
        ctx.ChatSessions.Add(new ChatSession
        {
            Id = chatId,
            UserId = userId,
            Title = "doc chat",
            Scope = ChatScope.Document,
            DocumentId = docId,
        });
        ctx.AgentSourceSessions.Add(new AgentSourceSession
        {
            Id = sessionId,
            UserId = userId,
            ChatSessionId = chatId,
            DocumentId = docId,
            OriginalFileName = "c.pdf",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx);
        var result = await svc.GetValidatedSessionAsync(userId, sessionId, chatId, documentIdForContext: null);

        Assert.Null(result);
    }
}
