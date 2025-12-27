using Minio;
using Minio.DataModel;
using Minio.Exceptions;
using System.Text.Json;
using System.IO;

namespace RusalProject.Services.Storage;

public class MinioService : IMinioService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioService> _logger;
    private readonly string _publicUrl;

    public MinioService(IConfiguration configuration, ILogger<MinioService> logger)
    {
        _logger = logger;
        
        var endpoint = configuration["MinIO:Endpoint"] ?? "localhost:9000";
        _publicUrl = configuration["MinIO:PublicUrl"] ?? "http://localhost:9000";
        var accessKey = configuration["MinIO:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["MinIO:SecretKey"] ?? "minioadmin";
        var useSSL = configuration.GetValue<bool>("MinIO:UseSSL", false);

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSSL)
            .Build();
    }

    public async Task EnsureBucketExistsAsync(string bucketName)
    {
        try
        {
            var existsArgs = new BucketExistsArgs()
                .WithBucket(bucketName);
            
            var exists = await _minioClient.BucketExistsAsync(existsArgs);
            
            if (!exists)
            {
                var makeArgs = new MakeBucketArgs()
                    .WithBucket(bucketName);
                
                await _minioClient.MakeBucketAsync(makeArgs);
                _logger.LogInformation("Created bucket: {BucketName}", bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring bucket exists: {BucketName}", bucketName);
            throw;
        }
    }

    public async Task UploadFileAsync(string bucketName, string objectName, Stream data, string contentType = "application/octet-stream")
    {
        try
        {
            await EnsureBucketExistsAsync(bucketName);

            // Reset stream position to beginning if possible
            if (data.CanSeek && data.Position != 0)
            {
                data.Position = 0;
            }

            // Get object size BEFORE reading the stream
            var objectSize = data.Length;
            
            // Ensure stream is at the beginning
            if (data.CanSeek)
            {
                data.Position = 0;
            }

            PutObjectArgs putObjectArgs;

            // MinIO SDK requires ObjectSize > 0. For empty files, use a single space byte
            if (objectSize == 0)
            {
                var singleByte = new byte[] { 32 }; // space character
                var singleByteStream = new MemoryStream(singleByte); // Don't use 'using' - MinIO SDK manages the stream lifecycle
                putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(singleByteStream)
                    .WithObjectSize(1)
                    .WithContentType(contentType);
            }
            else
            {
                putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(data)
                    .WithObjectSize(objectSize)
                    .WithContentType(contentType);
            }

            await _minioClient.PutObjectAsync(putObjectArgs);
            _logger.LogDebug("Uploaded file: {BucketName}/{ObjectName}", bucketName, objectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {BucketName}/{ObjectName}", bucketName, objectName);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string bucketName, string objectName)
    {
        try
        {
            var memoryStream = new MemoryStream();
            
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getObjectArgs);
            
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (BucketNotFoundException)
        {
            _logger.LogWarning("Bucket not found: {BucketName}", bucketName);
            throw new FileNotFoundException($"Bucket {bucketName} not found");
        }
        catch (ObjectNotFoundException)
        {
            _logger.LogWarning("Object not found: {BucketName}/{ObjectName}", bucketName, objectName);
            throw new FileNotFoundException($"Object {objectName} not found in bucket {bucketName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {BucketName}/{ObjectName}", bucketName, objectName);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string bucketName, string objectName)
    {
        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(statObjectArgs);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence: {BucketName}/{ObjectName}", bucketName, objectName);
            throw;
        }
    }

    public async Task DeleteFileAsync(string bucketName, string objectName)
    {
        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);
            _logger.LogDebug("Deleted file: {BucketName}/{ObjectName}", bucketName, objectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {BucketName}/{ObjectName}", bucketName, objectName);
            throw;
        }
    }

    public async Task DeleteDirectoryAsync(string bucketName, string prefix)
    {
        try
        {
            var listArgs = new ListObjectsArgs()
                .WithBucket(bucketName)
                .WithPrefix(prefix)
                .WithRecursive(true);

            var objects = new List<Item>();
            var tcs = new TaskCompletionSource<bool>();
            Exception? subscriptionException = null;

            _minioClient.ListObjectsAsync(listArgs).Subscribe(
                item => objects.Add(item),
                ex =>
                {
                    subscriptionException = ex;
                    tcs.TrySetException(ex);
                },
                () =>
                {
                    tcs.TrySetResult(true);
                }
            );

            await tcs.Task;

            if (subscriptionException != null)
                throw subscriptionException;

            foreach (var obj in objects)
            {
                await DeleteFileAsync(bucketName, obj.Key);
            }

            _logger.LogDebug("Deleted directory: {BucketName}/{Prefix}", bucketName, prefix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting directory: {BucketName}/{Prefix}", bucketName, prefix);
            throw;
        }
    }

    public async Task<List<string>> ListFilesAsync(string bucketName, string prefix)
    {
        try
        {
            var listArgs = new ListObjectsArgs()
                .WithBucket(bucketName)
                .WithPrefix(prefix)
                .WithRecursive(true);

            var fileList = new List<string>();
            var tcs = new TaskCompletionSource<bool>();
            
            _minioClient.ListObjectsAsync(listArgs).Subscribe(
                item => fileList.Add(item.Key),
                ex =>
                {
                    tcs.TrySetException(ex);
                },
                () =>
                {
                    tcs.TrySetResult(true);
                }
            );

            await tcs.Task;
            return fileList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files: {BucketName}/{Prefix}", bucketName, prefix);
            throw;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string bucketName, string objectName, int expirySeconds = 3600)
    {
        try
        {
            var presignedGetArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(expirySeconds);

            return await _minioClient.PresignedGetObjectAsync(presignedGetArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL: {BucketName}/{ObjectName}", bucketName, objectName);
            throw;
        }
    }

    public async Task<string> GetPresignedPutUrlAsync(string bucketName, string objectName, int expirySeconds = 3600)
    {
        try
        {
            var presignedPutArgs = new PresignedPutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(expirySeconds);

            return await _minioClient.PresignedPutObjectAsync(presignedPutArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned PUT URL: {BucketName}/{ObjectName}", bucketName, objectName);
            throw;
        }
    }
}
