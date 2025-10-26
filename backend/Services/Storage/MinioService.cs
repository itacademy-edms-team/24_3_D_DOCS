using Minio;
using Minio.DataModel.Args;

namespace RusalProject.Services.Storage;

public interface IMinioService
{
    Task<bool> BucketExistsAsync(string bucketName);
    Task CreateBucketAsync(string bucketName);
    Task<string> UploadFileAsync(string bucketName, string objectName, Stream fileStream, string contentType);
    Task<Stream> DownloadFileAsync(string bucketName, string objectName);
    Task<string> GetPresignedUrlAsync(string bucketName, string objectName, int expirySeconds = 3600);
    Task DeleteFileAsync(string bucketName, string objectName);
    Task<bool> FileExistsAsync(string bucketName, string objectName);
    Task<string> GetFileContentAsync(string bucketName, string objectName);
}

public class MinioService : IMinioService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioService> _logger;

    public MinioService(IMinioClient minioClient, ILogger<MinioService> logger)
    {
        _minioClient = minioClient;
        _logger = logger;
    }

    public async Task<bool> BucketExistsAsync(string bucketName)
    {
        try
        {
            var args = new BucketExistsArgs()
                .WithBucket(bucketName);
            
            return await _minioClient.BucketExistsAsync(args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if bucket {BucketName} exists", bucketName);
            return false;
        }
    }

    public async Task CreateBucketAsync(string bucketName)
    {
        try
        {
            var args = new MakeBucketArgs()
                .WithBucket(bucketName);
            
            await _minioClient.MakeBucketAsync(args);
            _logger.LogInformation("Created bucket: {BucketName}", bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bucket {BucketName}", bucketName);
            throw;
        }
    }

    public async Task<string> UploadFileAsync(string bucketName, string objectName, Stream fileStream, string contentType)
    {
        try
        {
            var args = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(args);
            _logger.LogInformation("Uploaded file {ObjectName} to bucket {BucketName}", objectName, bucketName);
            
            return objectName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {ObjectName} to bucket {BucketName}", objectName, bucketName);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string bucketName, string objectName)
    {
        try
        {
            var stream = new MemoryStream();
            var args = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(s => s.CopyTo(stream));

            await _minioClient.GetObjectAsync(args);
            stream.Position = 0;
            
            _logger.LogInformation("Downloaded file {ObjectName} from bucket {BucketName}", objectName, bucketName);
            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {ObjectName} from bucket {BucketName}", objectName, bucketName);
            throw;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string bucketName, string objectName, int expirySeconds = 3600)
    {
        try
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(expirySeconds);

            var url = await _minioClient.PresignedGetObjectAsync(args);
            _logger.LogInformation("Generated presigned URL for {ObjectName} in bucket {BucketName}", objectName, bucketName);
            
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for {ObjectName} in bucket {BucketName}", objectName, bucketName);
            throw;
        }
    }

    public async Task DeleteFileAsync(string bucketName, string objectName)
    {
        try
        {
            var args = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(args);
            _logger.LogInformation("Deleted file {ObjectName} from bucket {BucketName}", objectName, bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {ObjectName} from bucket {BucketName}", objectName, bucketName);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string bucketName, string objectName)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(args);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetFileContentAsync(string bucketName, string objectName)
    {
        try
        {
            using var stream = await DownloadFileAsync(bucketName, objectName);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file content for {BucketName}/{ObjectName}", bucketName, objectName);
            throw;
        }
    }
}
