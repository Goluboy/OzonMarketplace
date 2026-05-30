using ProductService.Domain.Entities;
using ProductService.Domain.ValueObjects;

namespace ProductService.UnitTests;

public class ProductFixture
{
    public int DefaultSku => 1001;
    public string DefaultName => "Standard Test Product";
    public string DefaultDescription => "Standard Description";
    public Guid DefaultCategoryId => Guid.Parse("11111111-2222-3333-4444-555555555555");
    public Money DefaultPrice => new Money(100);
    public List<ProductImage> DefaultImages => [new ProductImage("https://example.com/image1.png")];

    public Product CreateDefaultProduct()
    {
        return Product.Create(
            DefaultSku,
            DefaultName,
            DefaultDescription,
            DefaultCategoryId,
            DefaultPrice,
            new List<ProductImage>(DefaultImages)
        );
    }
}