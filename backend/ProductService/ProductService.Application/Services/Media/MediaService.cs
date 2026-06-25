using Core.Minio.Service;
using Microsoft.Extensions.Logging;
using ProductService.Application.DTO.Media;
using ProductService.Application.Helpers;

namespace ProductService.Application.Services.Media;

public class MediaService(IS3StorageService storageService, ICurrentUserHelper userHelper, ILogger<MediaService> logger) : IMediaService
{
    private readonly Random _random = new();
    public UploadFilesBatchOutput PrepareBatchUpload(UploadFilesBatchInput input, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        var userId = userHelper.UserId;

        logger.LogInformation("Preparing upload batch. Files count: {FileCount}, SellerId: {SellerId}", 
            input.FileNames.Count, userId);
        
        var urlPairs = new List<UploadFileMetadata>();

        foreach (var fileName in input.FileNames)
        {
            var objectKey = $"sellers/{userId:N}/products/{_random.Next()}_{fileName}";
            var (uploadUrl, publicUrl) = storageService.GenerateUploadUrls(objectKey);
            
            urlPairs.Add(new UploadFileMetadata(fileName, uploadUrl, publicUrl));
        }
        
        return new UploadFilesBatchOutput(urlPairs);
    }
}