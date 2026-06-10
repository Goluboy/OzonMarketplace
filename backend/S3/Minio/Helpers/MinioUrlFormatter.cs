namespace Core.Minio.Helpers;

public class MinioUrlFormatter(MinioOptions options) : IS3UrlFormatter
{
    public string ToAbsoluteUrl(string objectKey)
    {
        if (string.IsNullOrEmpty(objectKey))
        {
            return string.Empty;
        }
        
        if (objectKey.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
            objectKey.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return objectKey;
        }
        
        return $"{options.ExternalEndpoint}/{options.BucketName}/{objectKey}";
    }

    public string ToObjectKey(string absoluteUrl)
    {
        if (string.IsNullOrEmpty(absoluteUrl))
        {
            return string.Empty;
        }
        
        var prefix = $"{options.ExternalEndpoint}/{options.BucketName}/";
        return absoluteUrl.Replace(prefix, "");
    }
}