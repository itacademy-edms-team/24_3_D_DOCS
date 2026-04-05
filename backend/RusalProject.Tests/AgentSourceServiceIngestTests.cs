using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Models.Entities;
using RusalProject.Models.Types;
using RusalProject.Provider.Database;
using RusalProject.Services.AgentSources;
using RusalProject.Services.Chat;
using RusalProject.Services.Storage;
using Xunit;

namespace RusalProject.Tests;

public class AgentSourceServiceIngestTests
{
    [Fact]
    public async Task IngestAsync_TextFileWithNullBytes_StripsNullBytesBeforeSavingInlineText()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new ApplicationDbContext(options);
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();

        ctx.Users.Add(new User
        {
            Id = userId,
            Email = "ingest@test.local",
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
        await ctx.SaveChangesAsync();

        var minio = new Mock<IMinioService>();
        minio.Setup(x => x.EnsureBucketExistsAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        minio.Setup(x => x.UploadFileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var chat = new Mock<IChatService>();
        chat.Setup(x => x.GetChatByIdAsync(chatId, userId))
            .ReturnsAsync(new ChatSessionWithMessagesDTO
            {
                Id = chatId,
                Scope = ChatScope.Global,
                DocumentId = null,
                Title = "global",
                Messages = []
            });

        var logger = new Mock<ILogger<AgentSourceService>>();
        var service = new AgentSourceService(ctx, minio.Object, chat.Object, logger.Object);

        var bytes = Encoding.UTF8.GetBytes("abc\0def");
        await using var stream = new MemoryStream(bytes);
        IFormFile file = new FormFile(stream, 0, bytes.Length, "file", "notes.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var result = await service.IngestAsync(userId, documentId: null, chatId, file);

        var savedPart = await ctx.AgentSourceParts.SingleAsync(x => x.SessionId == result.SourceSessionId);
        Assert.NotNull(savedPart.InlineText);
        Assert.Equal("abcdef", savedPart.InlineText);
        Assert.DoesNotContain('\0', savedPart.InlineText);
    }
}
