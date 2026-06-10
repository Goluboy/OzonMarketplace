using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace Core.Minio.Service;

public class MinioStorageService(IAmazonS3 s3Client, IOptions<MinioOptions> options) : IS3StorageService
{
    private readonly MinioOptions _options = options.Value;

    public (string UploadUrl, string PublicUrl) GenerateUploadUrls(string objectKey,
        int expirationMinutes = 15)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(objectKey, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            ContentType = contentType,
            Protocol = _options.UseHttp ? Protocol.HTTP : Protocol.HTTPS 
        };

        var signingConfig = new AmazonS3Config
        {
            ServiceURL = _options.ExternalEndpoint,
            ForcePathStyle = true,
            UseHttp = _options.UseHttp
        };
        
        using var signingClient = new AmazonS3Client(_options.AccessKey, _options.SecretKey, signingConfig);
        
        var uploadUrl = signingClient.GetPreSignedURL(request);
        var publicUrl = $"{_options.ExternalEndpoint}/{_options.BucketName}/{objectKey}";

        return (uploadUrl, publicUrl);
    }

    public async Task DeleteFilesAsync(IReadOnlyList<string> objectKeys, CancellationToken ct = default)
    {
        if (objectKeys.Count == 0)
        {
            return;
        }
        
        var request = new DeleteObjectsRequest
        {
            BucketName = _options.BucketName,
            Objects = objectKeys
                .Select(key => new KeyVersion { Key = key })
                .ToList()
        };

        await s3Client.DeleteObjectsAsync(request, ct);
    }

    public string GetKeyFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }
        
        var prefix = $"{_options.ExternalEndpoint}/{_options.BucketName}/";
        return url.Replace(prefix, "");
    }
}