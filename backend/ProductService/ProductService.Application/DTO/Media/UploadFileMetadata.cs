namespace ProductService.Application.DTO.Media;

public record UploadFileMetadata(
    string FileName,
    string UploadUrl,
    string PublicUrl);