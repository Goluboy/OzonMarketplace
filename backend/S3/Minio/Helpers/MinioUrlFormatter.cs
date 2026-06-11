using Microsoft.Extensions.Options;

namespace Core.Minio.Helpers;

public class MinioUrlFormatter : IS3UrlFormatter
{
    private readonly MinioOptions _options;
    
    public MinioUrlFormatter(IOptions<MinioOptions> options)
    {
        _options = options.Value;
    }
    
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
        
        return $"{_options.ExternalEndpoint}/{_options.BucketName}/{objectKey}";
    }

    public string ToObjectKey(string absoluteUrl)
    {
        if (string.IsNullOrEmpty(absoluteUrl))
        {
            return string.Empty;
        }
        
        var prefix = $"{_options.ExternalEndpoint}/{_options.BucketName}/";
        return absoluteUrl.Replace(prefix, "");
    }
}