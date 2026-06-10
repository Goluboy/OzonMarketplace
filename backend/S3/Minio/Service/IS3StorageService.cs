namespace Core.Minio.Service;

public interface IS3StorageService
{
    (string UploadUrl, string PublicUrl) GenerateUploadUrls(string objectKey, int expirationMinutes = 15);
    Task DeleteFileAsync(string objectKey);
    string GetKeyFromUrl(string url);
}