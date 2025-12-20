using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RusalProject.Controllers;
using RusalProject.Models.DTOs.Documents;
using RusalProject.Services.Documents;
using Xunit;

namespace RusalProject.Tests;

public class DocumentsControllerTests
{
    private readonly Mock<IDocumentService> _mockService;
    private readonly Mock<ILogger<DocumentsController>> _mockLogger;
    private readonly DocumentsController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public DocumentsControllerTests()
    {
        _mockService = new Mock<IDocumentService>();
        _mockLogger = new Mock<ILogger<DocumentsController>>();
        _controller = new DocumentsController(_mockService.Object, _mockLogger.Object);
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult()
    {
        var documents = new List<DocumentMetaDTO>
        {
            new() { Id = Guid.NewGuid(), Name = "Doc1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        _mockService.Setup(s => s.GetAllDocumentsAsync(_testUserId))
            .ReturnsAsync(documents);

        var result = await _controller.GetAll();

        var okResult = Assert.IsType<ActionResult<List<DocumentMetaDTO>>>(result);
        var actionResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var returnValue = Assert.IsType<List<DocumentMetaDTO>>(actionResult.Value);
        Assert.Single(returnValue);
    }

    [Fact]
    public async Task GetById_ReturnsOkResult()
    {
        var documentId = Guid.NewGuid();
        var document = new DocumentDTO
        {
            Id = documentId,
            Name = "Test Doc",
            Content = "# Test",
            Overrides = new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockService.Setup(s => s.GetDocumentByIdAsync(documentId, _testUserId))
            .ReturnsAsync(document);

        var result = await _controller.GetById(documentId);

        var okResult = Assert.IsType<ActionResult<DocumentDTO>>(result);
        var actionResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var returnValue = Assert.IsType<DocumentDTO>(actionResult.Value);
        Assert.Equal(documentId, returnValue.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound()
    {
        var documentId = Guid.NewGuid();

        _mockService.Setup(s => s.GetDocumentByIdAsync(documentId, _testUserId))
            .ReturnsAsync((DocumentDTO?)null);

        var result = await _controller.GetById(documentId);

        var actionResult = Assert.IsType<ActionResult<DocumentDTO>>(result);
        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedResult()
    {
        var dto = new CreateDocumentDTO { Name = "New Doc" };
        var created = new DocumentDTO
        {
            Id = Guid.NewGuid(),
            Name = "New Doc",
            Content = "",
            Overrides = new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockService.Setup(s => s.CreateDocumentAsync(dto, _testUserId))
            .ReturnsAsync(created);

        var result = await _controller.Create(dto);

        var actionResult = Assert.IsType<ActionResult<DocumentDTO>>(result);
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var returnValue = Assert.IsType<DocumentDTO>(createdResult.Value);
        Assert.Equal("New Doc", returnValue.Name);
    }

    [Fact]
    public async Task Update_ReturnsOkResult()
    {
        var documentId = Guid.NewGuid();
        var dto = new UpdateDocumentDTO { Name = "Updated" };
        var updated = new DocumentDTO
        {
            Id = documentId,
            Name = "Updated",
            Content = "",
            Overrides = new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockService.Setup(s => s.UpdateDocumentAsync(documentId, dto, _testUserId))
            .ReturnsAsync(updated);

        var result = await _controller.Update(documentId, dto);

        var okResult = Assert.IsType<ActionResult<DocumentDTO>>(result);
        var actionResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var returnValue = Assert.IsType<DocumentDTO>(actionResult.Value);
        Assert.Equal("Updated", returnValue.Name);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var documentId = Guid.NewGuid();

        _mockService.Setup(s => s.DeleteDocumentAsync(documentId, _testUserId))
            .ReturnsAsync(true);

        var result = await _controller.Delete(documentId);

        Assert.IsType<NoContentResult>(result);
    }
}
