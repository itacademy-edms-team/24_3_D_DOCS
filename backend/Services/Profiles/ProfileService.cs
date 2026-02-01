using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using RusalProject.Models.DTOs.Profile;
using RusalProject.Models.Entities;
using RusalProject.Models.Types;
using RusalProject.Provider.Database;

namespace RusalProject.Services.Profiles;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioClient _minioClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProfileService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

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

    public async Task<List<ProfileDTO>> GetProfilesAsync(Guid userId, bool includePublic = true)
    {
        // Get user's own profiles from DB
        var dbProfiles = await _context.Profiles
            .Where(p => p.CreatorId == userId)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new { p.Id, p.CreatorId, p.UpdatedAt })
            .ToListAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        // Read profile data for each profile in parallel
        var profileTasks = dbProfiles.Select(async dbProfile =>
        {
            try
            {
                var profileData = await ReadJsonAsync<ProfileStorageDTO>(bucketName, GetProfilePath(dbProfile.Id));
                if (profileData == null) return null;

                return new ProfileDTO
                {
                    Id = dbProfile.Id,
                    CreatorId = dbProfile.CreatorId,
                    Name = profileData.Name ?? "Unnamed Profile",
                    Description = profileData.Description,
                    IsPublic = profileData.IsPublic,
                    CreatedAt = profileData.CreatedAt,
                    UpdatedAt = dbProfile.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read profile JSON for profile {ProfileId}", dbProfile.Id);
                return null;
            }
        });

        var profiles = (await Task.WhenAll(profileTasks))
            .Where(p => p != null)
            .Cast<ProfileDTO>()
            .ToList();

        // If includePublic, also get public profiles from other users
        if (includePublic)
        {
            var otherUsersProfiles = await _context.Profiles
                .Where(p => p.CreatorId != userId)
                .OrderByDescending(p => p.UpdatedAt)
                .Select(p => new { p.Id, p.CreatorId, p.UpdatedAt })
                .ToListAsync();

            foreach (var dbProfile in otherUsersProfiles)
            {
                try
                {
                    var otherBucketName = GetBucketName(dbProfile.CreatorId);
                    await EnsureBucketExistsAsync(otherBucketName);
                    
                    var profileData = await ReadJsonAsync<ProfileStorageDTO>(otherBucketName, GetProfilePath(dbProfile.Id));
                    if (profileData != null && profileData.IsPublic)
                    {
                        profiles.Add(new ProfileDTO
                        {
                            Id = dbProfile.Id,
                            CreatorId = dbProfile.CreatorId,
                            Name = profileData.Name ?? "Unnamed Profile",
                            Description = profileData.Description,
                            IsPublic = profileData.IsPublic,
                            CreatedAt = profileData.CreatedAt,
                            UpdatedAt = dbProfile.UpdatedAt
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read public profile {ProfileId}", dbProfile.Id);
                }
            }
        }

        return profiles;
    }

    public async Task<ProfileWithDataDTO?> GetProfileWithDataAsync(Guid id, Guid userId)
    {
        // First try to find in user's own profiles
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatorId == userId);

        Guid ownerId = userId;
        
        if (profile == null)
        {
            // Try to find public profile from another user
            profile = await _context.Profiles.FirstOrDefaultAsync(p => p.Id == id);
            if (profile == null) return null;
            
            ownerId = profile.CreatorId;
        }

        var bucketName = GetBucketName(ownerId);
        await EnsureBucketExistsAsync(bucketName);

        var storageData = await ReadJsonAsync<ProfileStorageDTO>(bucketName, GetProfilePath(id));
        if (storageData == null) return null;

        // If profile belongs to another user, it must be public
        if (profile.CreatorId != userId && !storageData.IsPublic)
        {
            return null;
        }

        return new ProfileWithDataDTO
        {
            Id = id,
            CreatorId = profile.CreatorId,
            Name = storageData.Name ?? "Unnamed Profile",
            Description = storageData.Description,
            IsPublic = storageData.IsPublic,
            CreatedAt = storageData.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            Data = storageData.Data ?? new ProfileData()
        };
    }

    public async Task<ProfileDTO> CreateProfileAsync(Guid userId, CreateProfileDTO dto)
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

        // Try to load default profile configuration
        var defaultData = new ProfileData();
        var defaultProfilePath = Path.Combine(AppContext.BaseDirectory, "Config", "DefaultProfile.json");
        if (File.Exists(defaultProfilePath))
        {
            try
            {
                var defaultJson = await File.ReadAllTextAsync(defaultProfilePath);
                defaultData = JsonSerializer.Deserialize<ProfileData>(defaultJson, JsonOptions) ?? new ProfileData();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load default profile configuration");
            }
        }

        var storageData = new ProfileStorageDTO
        {
            Name = dto.Name,
            Description = dto.Description,
            IsPublic = dto.IsPublic,
            CreatedAt = now,
            Data = dto.Data ?? defaultData
        };

        await WriteJsonAsync(bucketName, GetProfilePath(profileId), storageData);

        return new ProfileDTO
        {
            Id = profileId,
            CreatorId = userId,
            Name = storageData.Name,
            Description = storageData.Description,
            IsPublic = storageData.IsPublic,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public async Task<ProfileDTO> UpdateProfileAsync(Guid id, Guid userId, UpdateProfileDTO dto)
    {
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatorId == userId);

        if (profile == null)
        {
            throw new FileNotFoundException($"Profile {id} not found");
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var bucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(bucketName);

        var storageData = await ReadJsonAsync<ProfileStorageDTO>(bucketName, GetProfilePath(id));
        if (storageData == null)
        {
            throw new FileNotFoundException($"Profile data for {id} not found in storage");
        }

        // Update fields if provided
        if (dto.Name != null) storageData.Name = dto.Name;
        if (dto.Description != null) storageData.Description = dto.Description;
        if (dto.IsPublic.HasValue) storageData.IsPublic = dto.IsPublic.Value;
        if (dto.Data != null) storageData.Data = dto.Data;

        await WriteJsonAsync(bucketName, GetProfilePath(id), storageData);

        return new ProfileDTO
        {
            Id = id,
            CreatorId = userId,
            Name = storageData.Name ?? "Unnamed Profile",
            Description = storageData.Description,
            IsPublic = storageData.IsPublic,
            CreatedAt = storageData.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

    public async Task DeleteProfileAsync(Guid id, Guid userId)
    {
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatorId == userId);

        if (profile == null)
        {
            throw new FileNotFoundException($"Profile {id} not found");
        }

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
    }

    public async Task<ProfileDTO> DuplicateProfileAsync(Guid id, Guid userId, string? newName = null)
    {
        // Get the source profile (can be user's own or public)
        var sourceProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.Id == id);
        if (sourceProfile == null)
        {
            throw new FileNotFoundException($"Profile {id} not found");
        }

        var sourceBucketName = GetBucketName(sourceProfile.CreatorId);
        await EnsureBucketExistsAsync(sourceBucketName);

        var sourceData = await ReadJsonAsync<ProfileStorageDTO>(sourceBucketName, GetProfilePath(id));
        if (sourceData == null)
        {
            throw new FileNotFoundException($"Profile data for {id} not found");
        }

        // Check access: must be own profile or public
        if (sourceProfile.CreatorId != userId && !sourceData.IsPublic)
        {
            throw new UnauthorizedAccessException("Cannot duplicate private profile from another user");
        }

        // Create new profile
        var newProfileId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var newProfile = new Profile
        {
            Id = newProfileId,
            CreatorId = userId,
            UpdatedAt = now
        };

        _context.Profiles.Add(newProfile);
        await _context.SaveChangesAsync();

        var targetBucketName = GetBucketName(userId);
        await EnsureBucketExistsAsync(targetBucketName);

        var newStorageData = new ProfileStorageDTO
        {
            Name = newName ?? $"{sourceData.Name} (копия)",
            Description = sourceData.Description,
            IsPublic = false, // Duplicated profiles are private by default
            CreatedAt = now,
            Data = sourceData.Data
        };

        await WriteJsonAsync(targetBucketName, GetProfilePath(newProfileId), newStorageData);

        return new ProfileDTO
        {
            Id = newProfileId,
            CreatorId = userId,
            Name = newStorageData.Name ?? "Unnamed Profile",
            Description = newStorageData.Description,
            IsPublic = newStorageData.IsPublic,
            CreatedAt = now,
            UpdatedAt = now
        };
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
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (ObjectNotFoundException)
        {
            return default;
        }
    }

    private async Task WriteJsonAsync<T>(string bucketName, string objectName, T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
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

    // Internal DTO for storage
    private class ProfileStorageDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public ProfileData? Data { get; set; }
    }
}
