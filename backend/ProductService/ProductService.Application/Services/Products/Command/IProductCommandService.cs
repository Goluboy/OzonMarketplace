using ProductService.Application.DTO.Product;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;

namespace ProductService.Application.Services.Products.Command;

public interface IProductCommandService
{
    Task<ProductDetailsDto> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default);
    Task<ProductDetailsDto> UpdateProductAsync(UpdateProductDto dto, CancellationToken ct = default);
    Task DeleteProductAsync(Guid id, CancellationToken ct = default);
}