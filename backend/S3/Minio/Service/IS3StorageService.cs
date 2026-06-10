namespace Core.Minio.Service;

public interface IS3StorageService
{
    (string UploadUrl, string PublicUrl) GenerateUploadUrls(string objectKey, int expirationMinutes = 15);
    Task DeleteFilesAsync(IReadOnlyList<string> objectKeys, CancellationToken ct = default);
    string GetKeyFromUrl(string url);
}