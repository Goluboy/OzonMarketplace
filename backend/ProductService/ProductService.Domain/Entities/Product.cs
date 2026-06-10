using ProductService.Domain.Events;
using ProductService.Domain.ValueObjects;

namespace ProductService.Domain.Entities;

public class Product : IEquatable<Product>
{
    public Guid Id { get; private init; }
    public long Sku { get; init; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public int CategoryId { get; private set; }
    public Guid SellerId { get; private init; }
    
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
        long sku,
        string name,
        string description,
        int categoryId,
        Guid sellerId,
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

        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException("SellerId cannot be empty", nameof(sellerId));
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
            SellerId = sellerId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        product._images.AddRange(images);

        product._domainEvents.Add(new ProductCreatedEvent(
            product.Id,
            product.SellerId,
            product.Sku,
            product.Name,
            product.Price.Amount,
            product.Price.Currency,
            product.CategoryId,
            product.Images.Select(i => i.Url).ToList()));
        
        return product;
    }

    public static Product Reconstruct(
        Guid id,
        Guid sellerId,
        long sku,
        string name,
        string description,
        Money price,
        int categoryId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        int version,
        List<ProductImage> images)
    {
        var product = new Product
        {
            Id = id,
            SellerId = sellerId,
            Sku = sku,
            Name = name,
            Description = description,
            Price = price,
            CategoryId = categoryId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Version = version
        };

        product._images.AddRange(images);

        return product;
    }
    
    public bool IsOwnedBy(Guid userId)
    {
        return SellerId == userId;
    }
    
    public void ChangePrice(Money newPrice)
    {
        var oldPrice = Price;
        Price = newPrice ?? throw new ArgumentNullException(nameof(newPrice));
        
        
        _domainEvents.Add(new ProductPriceChangedEvent(
            Id,
            oldPrice.Amount,
            oldPrice.Currency,
            newPrice.Amount,
            newPrice.Currency));
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
        
        _domainEvents.Add(new ProductDetailsUpdatedEvent(
            Id,
            Name,
            Description,
            CategoryId));
    }

    public void UpdateImages(IReadOnlyList<string> imageUrlsSnapshot)
    {
        ArgumentNullException.ThrowIfNull(imageUrlsSnapshot);
        
        var inputUrlsSet = new HashSet<string>(imageUrlsSnapshot, StringComparer.OrdinalIgnoreCase);
        
        var currentUrlsSet = new HashSet<string>(_images.Select(img => img.Url), StringComparer.OrdinalIgnoreCase);

        var imagesToRemove = currentUrlsSet
            .Where(url => !inputUrlsSet.Contains(url))
            .ToList();
        
        var imagesToAdd = inputUrlsSet
            .Where(url => !currentUrlsSet.Contains(url))
            .ToList();
        
        if (imagesToRemove.Count == 0 && imagesToAdd.Count == 0)
        {
            return;
        }

        if (imagesToRemove.Count != 0)
        {
            _images.RemoveAll(img => !inputUrlsSet.Contains(img.Url));
        }

        foreach (var url in imagesToAdd)
        {
            _images.Add(new ProductImage(url));
        }
        
        _domainEvents.Add(new ProductImagesUpdatedEvent(Id, _images.Select(i => i.Url).ToList(), imagesToRemove));
    }

    public void IncrementVersion()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
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