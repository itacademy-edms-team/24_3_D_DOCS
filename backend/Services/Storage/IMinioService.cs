namespace RusalProject.Services.Storage;

public interface IMinioService
{
    Task EnsureBucketExistsAsync(string bucketName);
    Task UploadFileAsync(string bucketName, string objectName, Stream data, string contentType = "application/octet-stream");
    Task<Stream> DownloadFileAsync(string bucketName, string objectName);
    Task<bool> FileExistsAsync(string bucketName, string objectName);
    Task DeleteFileAsync(string bucketName, string objectName);
    Task DeleteDirectoryAsync(string bucketName, string prefix);
    Task<List<string>> ListFilesAsync(string bucketName, string prefix);
    Task<string> GetPresignedUrlAsync(string bucketName, string objectName, int expirySeconds = 3600);
}
