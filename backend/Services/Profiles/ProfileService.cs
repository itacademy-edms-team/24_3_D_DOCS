using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        // Get profiles from DB (only id and updatedAt for sorting)
        var dbProfiles = await _context.Profiles
            .Where(p => p.CreatorId == userId)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new { p.Id, p.UpdatedAt })
            .ToListAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        // Read Profile/{id}.json for each profile in parallel
        var metaTasks = dbProfiles.Select(async dbProfile =>
        {
            try
            {
                var profileData = await ReadJsonAsync<ProfileDTO>(bucketName, GetProfilePath(dbProfile.Id));
                if (profileData == null) return null;

                return new ProfileMetaDTO
                {
                    Id = profileData.Id,
                    Name = profileData.Name,
                    CreatedAt = profileData.CreatedAt,
                    UpdatedAt = profileData.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read profile JSON for profile {ProfileId}", dbProfile.Id);
                return null;
            }
        });

        var metas = await Task.WhenAll(metaTasks);
        return metas.Where(m => m != null).Cast<ProfileMetaDTO>().ToList();
    }

    public async Task<ProfileDTO?> GetProfileByIdAsync(Guid id, Guid userId)
    {
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatorId == userId);

        if (profile == null) return null;

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var profileData = await ReadJsonAsync<ProfileDTO>(bucketName, GetProfilePath(id));
        return profileData;
    }

    public async Task<ProfileDTO> CreateProfileAsync(CreateProfileDTO dto, Guid userId)
    {
        var profileId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var profile = new Profile
        {
            Id = profileId,
            CreatorId = userId,
            UpdatedAt = now
        };

        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var defaultProfilePath = Path.Combine(AppContext.BaseDirectory, "Config", "DefaultProfile.json");
        var defaultProfile = new ProfileDTO();
        if (File.Exists(defaultProfilePath))
        {
            var defaultProfileJson = await File.ReadAllTextAsync(defaultProfilePath);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            defaultProfile = JsonSerializer.Deserialize<ProfileDTO>(defaultProfileJson, jsonOptions) ?? new ProfileDTO();
        }

        defaultProfile.Id = profileId;
        defaultProfile.Name = dto.Name;
        defaultProfile.CreatedAt = now;
        defaultProfile.UpdatedAt = now;

        await WriteJsonAsync(bucketName, GetProfilePath(profileId), defaultProfile);

        return defaultProfile;
    }

    public async Task<ProfileDTO?> UpdateProfileAsync(Guid id, UpdateProfileDTO dto, Guid userId)
    {
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatorId == userId);

        if (profile == null) return null;

        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var profileData = await ReadJsonAsync<ProfileDTO>(bucketName, GetProfilePath(id));
        if (profileData == null) return null;

        if (dto.Name != null) profileData.Name = dto.Name;
        if (dto.Page != null)
        {
            if (profileData.Page == null) profileData.Page = new ProfilePageDTO();
            if (dto.Page.Size != null) profileData.Page.Size = dto.Page.Size;
            if (dto.Page.Orientation != null) profileData.Page.Orientation = dto.Page.Orientation;
            if (dto.Page.Margins != null) profileData.Page.Margins = dto.Page.Margins;
            if (dto.Page.PageNumbers != null) profileData.Page.PageNumbers = dto.Page.PageNumbers;
        }
        if (dto.Entities != null) profileData.Entities = dto.Entities;

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
