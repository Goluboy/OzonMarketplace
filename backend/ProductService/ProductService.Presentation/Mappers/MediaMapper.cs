using ProductService.Application.DTO.Media;
using ProductService.Presentation.Models;

namespace ProductService.Presentation.Mappers;

public static class MediaMapper
{
    public static UploadFilesBatchInput ToDto(this UploadFilesRequest request)
    {
        return new UploadFilesBatchInput(request.FileNames);
    }

    public static UploadFilesMetadataResponse ToHttpResponse(this UploadFileMetadata dto)
    {
        return new UploadFilesMetadataResponse(
            dto.FileName,
            dto.UploadUrl,
            dto.PublicUrl);
    }

    public static UploadFilesResponse ToHttpResponse(this UploadFilesBatchOutput dto)
    {
        return new UploadFilesResponse(dto.FilesMetadata.Select(metadata => metadata.ToHttpResponse()).ToList());
    }
}