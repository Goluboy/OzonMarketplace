namespace ProductService.Infrastructure.Abstractions.DTO.Product.Query;

public record ProductPagedRowModel(
    Guid Id, 
    string SortValue);