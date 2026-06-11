namespace ProductService.Application.DTO.Media;

public record UploadFilesBatchOutput(
    IReadOnlyList<UploadFileMetadata> FilesMetadata);