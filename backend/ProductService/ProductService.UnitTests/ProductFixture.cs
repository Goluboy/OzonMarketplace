using ProductService.Domain.Entities;
using ProductService.Domain.ValueObjects;

namespace ProductService.UnitTests;

public class ProductFixture
{
    public int DefaultSku => 1001;
    public string DefaultName => "Standard Test Product";
    public string DefaultDescription => "Standard Description";
    public int DefaultCategoryId => 0123456789;
    public Money DefaultPrice => new Money(100);
    public Guid SellerId => Guid.NewGuid();
    public List<ProductImage> DefaultImages => [new ProductImage("https://example.com/image1.png")];

    public Product CreateDefaultProduct()
    {
        return Product.Create(
            DefaultSku,
            DefaultName,
            DefaultDescription,
            DefaultCategoryId,
            SellerId,
            DefaultPrice,
            new List<ProductImage>(DefaultImages)
        );
    }
}