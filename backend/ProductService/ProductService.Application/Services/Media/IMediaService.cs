using ProductService.Application.DTO.Media;

namespace ProductService.Application.Services.Media;

public interface IMediaService
{
    UploadFilesBatchOutput PrepareBatchUploadAsync(UploadFilesBatchInput input, CancellationToken ct = default);
}