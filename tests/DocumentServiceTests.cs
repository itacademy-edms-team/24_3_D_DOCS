using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Moq;
using RusalProject.Models.DTOs.Documents;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;
using RusalProject.Services.Documents;
using Xunit;

namespace RusalProject.Tests;

public class DocumentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IMinioClient> _mockMinio;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<DocumentService>> _mockLogger;
    private readonly DocumentService _service;
    private readonly Guid _testUserId = Guid.NewGuid();

    public DocumentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _mockMinio = new Mock<IMinioClient>();
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<DocumentService>>();

        // Mock configuration to return empty string for default content
        _mockConfig.Setup(c => c[It.IsAny<string>()]).Returns((string?)null);

        _service = new DocumentService(_context, _mockMinio.Object, _mockConfig.Object, _mockLogger.Object);

        SetupMinioMocks();
    }

    private void SetupMinioMocks()
    {
        _mockMinio.Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), default))
            .ReturnsAsync(true);
        
        _mockMinio.Setup(m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), default))
            .Returns(Task.CompletedTask);
        
        _mockMinio.Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), default))
            .ReturnsAsync(It.IsAny<Minio.DataModel.Response.PutObjectResponse>());
        
        _mockMinio.Setup(m => m.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), default))
            .Returns(Task.CompletedTask);
        
        // GetObjectAsync will throw ObjectNotFoundException by default (files don't exist)
        // Tests that need file reading should provide Overrides in DTO to avoid reading from MinIO
    }

    [Fact]
    public async Task CreateDocumentAsync_CreatesDocumentInDb()
    {
        // Create Config directory and file if needed
        var configDir = Path.Combine(AppContext.BaseDirectory, "Config");
        Directory.CreateDirectory(configDir);
        var configFile = Path.Combine(configDir, "DefaultContent.md");
        if (!File.Exists(configFile))
        {
            await File.WriteAllTextAsync(configFile, "");
        }

        var dto = new CreateDocumentDTO { Name = "Test Doc" };

        var result = await _service.CreateDocumentAsync(dto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal("Test Doc", result.Name);

        var dbDoc = await _context.Documents.FindAsync(result.Id);
        Assert.NotNull(dbDoc);
        Assert.Equal("Test Doc", dbDoc.Name);
        Assert.Equal(_testUserId, dbDoc.CreatorId);
    }

    [Fact]
    public async Task GetAllDocumentsAsync_ReturnsOnlyUserDocuments()
    {
        var user1 = new User { Id = _testUserId, Email = "user1@test.com", Name = "User1", PasswordHash = "hash" };
        var user2 = new User { Id = Guid.NewGuid(), Email = "user2@test.com", Name = "User2", PasswordHash = "hash" };
        _context.Users.AddRange(user1, user2);

        var doc1 = new Document { Id = Guid.NewGuid(), CreatorId = _testUserId, Name = "Doc1" };
        var doc2 = new Document { Id = Guid.NewGuid(), CreatorId = user2.Id, Name = "Doc2" };
        var doc3 = new Document { Id = Guid.NewGuid(), CreatorId = _testUserId, Name = "Doc3" };
        _context.Documents.AddRange(doc1, doc2, doc3);
        await _context.SaveChangesAsync();

        var result = await _service.GetAllDocumentsAsync(_testUserId);

        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.Contains(d.Name, new[] { "Doc1", "Doc3" }));
    }

    [Fact]
    public async Task GetDocumentByIdAsync_ReturnsNullForNonExistent()
    {
        var result = await _service.GetDocumentByIdAsync(Guid.NewGuid(), _testUserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateDocumentAsync_UpdatesName()
    {
        var doc = new Document { Id = Guid.NewGuid(), CreatorId = _testUserId, Name = "Old Name" };
        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        var dto = new UpdateDocumentDTO { Name = "New Name", Overrides = new Dictionary<string, object>() };
        var result = await _service.UpdateDocumentAsync(doc.Id, dto, _testUserId);

        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);

        var updated = await _context.Documents.FindAsync(doc.Id);
        Assert.Equal("New Name", updated!.Name);
    }

    [Fact]
    public async Task DeleteDocumentAsync_RemovesFromDb()
    {
        var doc = new Document { Id = Guid.NewGuid(), CreatorId = _testUserId, Name = "To Delete" };
        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteDocumentAsync(doc.Id, _testUserId);

        Assert.True(result);
        var deleted = await _context.Documents.FindAsync(doc.Id);
        Assert.Null(deleted);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
