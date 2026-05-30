using ProductService.Domain.Events;
using ProductService.Domain.ValueObjects;

namespace ProductService.Domain.Entities;

public class Product : IEquatable<Product>
{
    public Guid Id { get; init; }
    public int Sku { get; init; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public int CategoryId { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    
    public int Version { get;  private set; }
    
    private readonly List<ProductImage> _images = [];
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
    
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    
    private Product() { }

    public static Product Create(
        int sku,
        string name,
        string description,
        int categoryId,
        Money price,
        List<ProductImage> images)
    {
        if (sku <= 0)
        {
            throw new ArgumentException("Sku cannot be zero or negative", nameof(sku));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name cannot be null or empty", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(price);

        if (categoryId <= 0)
        {
            throw new ArgumentException("Category Id cannot be negative", nameof(categoryId));
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            Name = name,
            Description = description,
            Price = price,
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        product._images.AddRange(images);

        product._domainEvents.Add(new ProductCreatedEvent(
            product.Id,
            product.Sku,
            product.Name,
            product.Price.Amount,
            product.CategoryId,
            product.Images.Select(i => i.Url).ToList()));
        
        return product;
    }
    
    public void ChangePrice(Money newPrice)
    {
        var oldPrice = Price;
        Price = newPrice ?? throw new ArgumentNullException(nameof(newPrice));
        UpdateTimestamp();
        
        _domainEvents.Add(new ProductPriceChangedEvent(
            Id,
            oldPrice.Amount,
            newPrice.Amount));
    }
    
    public void UpdateDetails(string name, string description, int categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name cannot be null or empty", nameof(name));
        }

        if (categoryId <= 0)
        {
            throw new ArgumentException("Category Id cannot be negative", nameof(categoryId));
        }

        Name = name;
        Description = description;
        CategoryId = categoryId;
        UpdateTimestamp();
        
        _domainEvents.Add(new ProductDetailsUpdatedEvent(
            Id,
            Name,
            Description,
            CategoryId));
    }
    
    public void AddImage(ProductImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (_images.Any(i => i.Url.Equals(image.Url, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Image with the same URL already exists", nameof(image));
        }

        _images.Add(image);
        UpdateTimestamp();
        
        _domainEvents.Add(new ProductImagesUpdatedEvent(
            Id,
            _images.Select(i => i.Url).ToList()));
    }
    
    public void RemoveImage(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        var imageToRemove = _images.FirstOrDefault(i => i.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
        if (imageToRemove == null)
        {
            throw new ArgumentException("Image not found", nameof(url));
        }
        
        _images.Remove(imageToRemove);
        UpdateTimestamp();
        
        _domainEvents.Add(new ProductImagesUpdatedEvent(
            Id,
            _images.Select(i => i.Url).ToList()));
    }
    
    public bool Equals(Product? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Product);

    public override int GetHashCode() => Id.GetHashCode();
    
    private void UpdateTimestamp()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }
    
    public static bool operator ==(Product? left, Product? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(Product? left, Product? right) => !(left == right);
}