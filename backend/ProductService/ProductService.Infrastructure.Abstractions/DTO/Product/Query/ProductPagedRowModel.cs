namespace ProductService.Infrastructure.Abstractions.DTO.Product.Query;

public record ProductPagedRowModel
{
    public ProductPagedRowModel() {}
    public Guid Id { get; init; }
    public required object SortValue { get; init; }
}