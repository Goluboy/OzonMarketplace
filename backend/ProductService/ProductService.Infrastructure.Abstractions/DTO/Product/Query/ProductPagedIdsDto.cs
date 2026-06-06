namespace ProductService.Infrastructure.Abstractions.DTO.Product.Query;

public record ProductPagedIdsDto(
    IReadOnlyList<Guid> ProductIds,
    string? NextCursor);