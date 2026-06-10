namespace ProductService.Application.DTO.Media;

public record UploadFilesBatchInput(
    IReadOnlyList<string> FileNames);