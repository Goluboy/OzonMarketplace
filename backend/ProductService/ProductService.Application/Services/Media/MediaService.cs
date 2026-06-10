using Core.Minio.Service;
using ProductService.Application.DTO.Media;

namespace ProductService.Application.Services.Media;

public class MediaService(IS3StorageService storageService) : IMediaService
{
    private readonly Random _random = new();
    public UploadFilesBatchOutput PrepareBatchUpload(UploadFilesBatchInput input, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        //TODO Authorization

        var urlPairs = new List<UploadFileMetadata>();

        foreach (var fileName in input.FileNames)
        {
            var objectKey = $"products/{_random.Next()}_{fileName}"; //TODO Добавить секцию в пути по SellerId
            var (uploadUrl, publicUrl) = storageService.GenerateUploadUrls(objectKey);
            
            urlPairs.Add(new UploadFileMetadata(fileName, uploadUrl, publicUrl));
        }
        
        return new UploadFilesBatchOutput(urlPairs);
    }
}