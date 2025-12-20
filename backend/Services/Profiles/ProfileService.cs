using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using RusalProject.Models.DTOs.Profiles;
using RusalProject.Models.Entities;
using RusalProject.Provider.Database;

namespace RusalProject.Services.Profiles;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioClient _minioClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        ApplicationDbContext context,
        IMinioClient minioClient,
        IConfiguration configuration,
        ILogger<ProfileService> logger)
    {
        _context = context;
        _minioClient = minioClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetBucketName(Guid userId) => $"user-{userId}";
    private string GetProfilePath(Guid profileId) => $"Profile/{profileId}.json";

    public async Task<List<ProfileMetaDTO>> GetAllProfilesAsync(Guid userId)
    {
        var profiles = await _context.Profiles
            .Where(p => p.CreatorId == userId)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new ProfileMetaDTO
            {
                Id = p.Id,
                Name = p.Name,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return profiles;
    }

    public async Task<ProfileDTO?> GetProfileByIdAsync(Guid id, Guid userId)
    {
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatorId == userId);

        if (profile == null) return null;

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var profileData = await ReadJsonAsync<ProfileDTO>(bucketName, GetProfilePath(id));
        if (profileData == null) return null;

        profileData.Id = profile.Id;
        profileData.Name = profile.Name;
        profileData.CreatedAt = profile.CreatedAt;
        profileData.UpdatedAt = profile.UpdatedAt;

        return profileData;
    }

    public async Task<ProfileDTO> CreateProfileAsync(CreateProfileDTO dto, Guid userId)
    {
        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            CreatorId = userId,
            Name = dto.Name
        };

        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var defaultProfilePath = Path.Combine(AppContext.BaseDirectory, "Config", "DefaultProfile.json");
        var defaultProfileJson = await File.ReadAllTextAsync(defaultProfilePath);
        var defaultProfile = JsonSerializer.Deserialize<ProfileDTO>(defaultProfileJson) ?? new ProfileDTO();

        defaultProfile.Id = profile.Id;
        defaultProfile.Name = profile.Name;
        defaultProfile.CreatedAt = profile.CreatedAt;
        defaultProfile.UpdatedAt = profile.UpdatedAt;

        await WriteJsonAsync(bucketName, GetProfilePath(profile.Id), defaultProfile);

        return defaultProfile;
    }

    public async Task<ProfileDTO?> UpdateProfileAsync(Guid id, UpdateProfileDTO dto, Guid userId)
    {
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatorId == userId);

        if (profile == null) return null;

        if (dto.Name != null) profile.Name = dto.Name;

        await _context.SaveChangesAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var profileData = await ReadJsonAsync<ProfileDTO>(bucketName, GetProfilePath(id)) ?? new ProfileDTO();

        if (dto.Name != null) profileData.Name = dto.Name;
        if (dto.Page != null) profileData.Page = dto.Page;
        if (dto.Entities != null) profileData.Entities = dto.Entities;

        profileData.Id = profile.Id;
        profileData.CreatedAt = profile.CreatedAt;
        profileData.UpdatedAt = profile.UpdatedAt;

        await WriteJsonAsync(bucketName, GetProfilePath(id), profileData);

        return profileData;
    }

    public async Task<bool> DeleteProfileAsync(Guid id, Guid userId)
    {
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatorId == userId);

        if (profile == null) return false;

        var bucketName = GetBucketName(userId);
        var profilePath = GetProfilePath(id);

        try
        {
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(profilePath);
            await _minioClient.RemoveObjectAsync(removeArgs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete profile file from MinIO for profile {ProfileId}", id);
        }

        _context.Profiles.Remove(profile);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task EnsureBucketExistsAsync(string bucketName)
    {
        var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs);
        if (!exists)
        {
            var makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
            await _minioClient.MakeBucketAsync(makeBucketArgs);
        }
    }

    private async Task<T?> ReadJsonAsync<T>(string bucketName, string objectName)
    {
        try
        {
            var memoryStream = new MemoryStream();
            var getArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getArgs);
            memoryStream.Position = 0;
            var content = Encoding.UTF8.GetString(memoryStream.ToArray());
            return JsonSerializer.Deserialize<T>(content);
        }
        catch (ObjectNotFoundException)
        {
            return default;
        }
    }

    private async Task WriteJsonAsync<T>(string bucketName, string objectName, T data)
    {
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream(bytes);

        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(bytes.Length)
            .WithContentType("application/json");

        await _minioClient.PutObjectAsync(putArgs);
    }
}
