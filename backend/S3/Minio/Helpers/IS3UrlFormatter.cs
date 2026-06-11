namespace Core.Minio.Helpers;

public interface IS3UrlFormatter
{
    string ToAbsoluteUrl(string objectKey);
    string ToObjectKey(string absoluteUrl);
}