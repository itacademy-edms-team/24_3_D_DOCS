using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RusalProject.Controllers;
using RusalProject.Models.DTOs.Profiles;
using RusalProject.Services.Profiles;
using Xunit;

namespace RusalProject.Tests;

public class ProfilesControllerTests
{
    private readonly Mock<IProfileService> _mockService;
    private readonly Mock<ILogger<ProfilesController>> _mockLogger;
    private readonly ProfilesController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ProfilesControllerTests()
    {
        _mockService = new Mock<IProfileService>();
        _mockLogger = new Mock<ILogger<ProfilesController>>();
        _controller = new ProfilesController(_mockService.Object, _mockLogger.Object);
        
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
        var profiles = new List<ProfileMetaDTO>
        {
            new() { Id = Guid.NewGuid(), Name = "Profile1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        _mockService.Setup(s => s.GetAllProfilesAsync(_testUserId))
            .ReturnsAsync(profiles);

        var result = await _controller.GetAll();

        var okResult = Assert.IsType<ActionResult<List<ProfileMetaDTO>>>(result);
        var actionResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var returnValue = Assert.IsType<List<ProfileMetaDTO>>(actionResult.Value);
        Assert.Single(returnValue);
    }

    [Fact]
    public async Task GetById_ReturnsOkResult()
    {
        var profileId = Guid.NewGuid();
        var profile = new ProfileDTO
        {
            Id = profileId,
            Name = "Test Profile",
            Page = new ProfilePageDTO(),
            Entities = new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockService.Setup(s => s.GetProfileByIdAsync(profileId, _testUserId))
            .ReturnsAsync(profile);

        var result = await _controller.GetById(profileId);

        var okResult = Assert.IsType<ActionResult<ProfileDTO>>(result);
        var actionResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var returnValue = Assert.IsType<ProfileDTO>(actionResult.Value);
        Assert.Equal(profileId, returnValue.Id);
    }

    [Fact]
    public async Task Create_ReturnsCreatedResult()
    {
        var dto = new CreateProfileDTO { Name = "New Profile" };
        var created = new ProfileDTO
        {
            Id = Guid.NewGuid(),
            Name = "New Profile",
            Page = new ProfilePageDTO(),
            Entities = new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockService.Setup(s => s.CreateProfileAsync(dto, _testUserId))
            .ReturnsAsync(created);

        var result = await _controller.Create(dto);

        var actionResult = Assert.IsType<ActionResult<ProfileDTO>>(result);
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var returnValue = Assert.IsType<ProfileDTO>(createdResult.Value);
        Assert.Equal("New Profile", returnValue.Name);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var profileId = Guid.NewGuid();

        _mockService.Setup(s => s.DeleteProfileAsync(profileId, _testUserId))
            .ReturnsAsync(true);

        var result = await _controller.Delete(profileId);

        Assert.IsType<NoContentResult>(result);
    }
}
