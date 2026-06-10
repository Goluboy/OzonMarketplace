using Amazon.S3;
using Core.Minio.Helpers;
using Core.Minio.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Minio;

public static class MinioServiceExtensions
{
    public static IServiceCollection AddMinioStorage(this IServiceCollection services, Action<MinioOptions> configureOptions)
    {
        var minioOpt = new MinioOptions();
        configureOptions(minioOpt);
        services.Configure(configureOptions);

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = minioOpt.Endpoint,
                ForcePathStyle = true,
                UseHttp = minioOpt.UseHttp
            };
            return new AmazonS3Client(minioOpt.AccessKey, minioOpt.SecretKey, config);
        });

        services.AddSingleton<IS3StorageService, MinioStorageService>();
        services.AddSingleton<IS3UrlFormatter, MinioUrlFormatter>();
        
        return services;
    }
}