using ProductService.Application.DTO.Media;

namespace ProductService.Application.Services.Media;

public interface IMediaService
{
    UploadFilesBatchOutput PrepareBatchUpload(UploadFilesBatchInput input, CancellationToken ct = default);
}