using RusalProject.Models.DTOs.Chat;
using RusalProject.Models.Entities;
using RusalProject.Services.Agent;
using RusalProject.Services.AgentSources;
using RusalProject.Services.Ollama;
using Moq;
using Xunit;

namespace RusalProject.Tests;

public class AgentAttachmentContextServiceTests
{
    [Fact]
    public void ParseAttachmentIdsNewestFirst_ReturnsIdsFromEndOfArrayFirst()
    {
        var json =
            """[{"sourceSessionId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"},{"sourceSessionId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"}]""";
        var ids = AgentAttachmentContextService.ParseAttachmentIdsNewestFirst(json).ToList();
        Assert.Equal(2, ids.Count);
        Assert.Equal(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), ids[0]);
        Assert.Equal(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), ids[1]);
    }

    [Fact]
    public void EnumerateAttachmentSessionIdsNewestFirst_UserMessagesByCreatedAtDescThenAttachmentOrder()
    {
        var older = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var idOldMsg = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var idNewMsg = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var messages = new List<ChatMessageDTO>
        {
            new()
            {
                Role = "user",
                CreatedAt = older,
                AttachmentsJson = $$"""[{"sourceSessionId":"{{idOldMsg}}"}]""",
            },
            new()
            {
                Role = "user",
                CreatedAt = newer,
                AttachmentsJson = $$"""[{"sourceSessionId":"{{idNewMsg}}"}]""",
            },
        };

        var ordered = AgentAttachmentContextService.EnumerateAttachmentSessionIdsNewestFirst(messages).ToList();
        Assert.Equal(new[] { idNewMsg, idOldMsg }, ordered);
    }

    [Fact]
    public async Task ResolveAndInjectCatalogAsync_NoRequestId_UsesFirstValidSessionFromHistory()
    {
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var staleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var goodId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var session = new AgentSourceSession { Id = goodId, ChatSessionId = chatId };

        var mockSources = new Mock<IAgentSourceService>();
        mockSources
            .Setup(s => s.GetValidatedSessionAsync(userId, goodId, chatId, docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        mockSources.Setup(s => s.BuildCatalog(session, null)).Returns("CATALOG");

        var svc = new AgentAttachmentContextService(mockSources.Object);
        var messages = new List<ChatMessageDTO>
        {
            new()
            {
                Role = "user",
                CreatedAt = DateTime.UtcNow,
                Content = "hi",
                AttachmentsJson =
                    $$"""[{"sourceSessionId":"{{staleId}}"},{"sourceSessionId":"{{goodId}}"}]""",
            },
        };
        var history = new List<OllamaMessageInput>
        {
            new() { Role = "user", Content = "hi" },
        };

        var resolved = await svc.ResolveAndInjectCatalogAsync(
            userId,
            chatId,
            AgentAttachmentContextScope.Document,
            docId,
            null,
            messages,
            history);

        Assert.Equal(goodId, resolved);
        Assert.Equal(2, history.Count);
        Assert.Equal("CATALOG", history[0].Content);
        Assert.Equal("hi", history[1].Content);

        mockSources.Verify(
            s => s.GetValidatedSessionAsync(userId, goodId, chatId, docId, It.IsAny<CancellationToken>()),
            Times.Once);
        // В JSON массиве последний id проверяется первым; stale не должен вызываться.
        mockSources.Verify(
            s => s.GetValidatedSessionAsync(userId, staleId, chatId, docId, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAndInjectCatalogAsync_ExplicitSessionWithoutDocument_AllowedForDocumentScope()
    {
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var session = new AgentSourceSession
        {
            Id = sessionId,
            ChatSessionId = chatId,
            DocumentId = null,
        };

        var mockSources = new Mock<IAgentSourceService>();
        mockSources
            .Setup(s => s.GetValidatedSessionAsync(userId, sessionId, chatId, docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        mockSources.Setup(s => s.BuildCatalog(session, null)).Returns("CATALOG");

        var svc = new AgentAttachmentContextService(mockSources.Object);
        var history = new List<OllamaMessageInput> { new() { Role = "user", Content = "x" } };

        var resolved = await svc.ResolveAndInjectCatalogAsync(
            userId,
            chatId,
            AgentAttachmentContextScope.Document,
            docId,
            sessionId,
            Array.Empty<ChatMessageDTO>(),
            history);

        Assert.Equal(sessionId, resolved);
        Assert.Equal(2, history.Count);
        Assert.Equal("CATALOG", history[0].Content);
        Assert.Equal("x", history[1].Content);
    }

    [Fact]
    public async Task ResolveAndInjectCatalogAsync_InvalidExplicitRequestId_Throws()
    {
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var badId = Guid.NewGuid();

        var mockSources = new Mock<IAgentSourceService>();
        mockSources
            .Setup(s => s.GetValidatedSessionAsync(userId, badId, chatId, docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentSourceSession?)null);

        var svc = new AgentAttachmentContextService(mockSources.Object);
        var history = new List<OllamaMessageInput> { new() { Role = "user", Content = "x" } };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ResolveAndInjectCatalogAsync(
                userId,
                chatId,
                AgentAttachmentContextScope.Document,
                docId,
                badId,
                Array.Empty<ChatMessageDTO>(),
                history));
    }
}
