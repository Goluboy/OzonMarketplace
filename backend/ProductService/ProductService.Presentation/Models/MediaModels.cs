
namespace ProductService.Presentation.Models;

public record UploadFilesRequest(
    List<string> FileNames);

public record UploadFilesMetadataResponse(
    string FileName,
    string UploadUrl,
    string PublicUrl);

public record UploadFilesResponse(
    List<UploadFilesMetadataResponse> FilesMetadata);