namespace ProductService.Infrastructure.DAO;

public record CategoryDao
{
    public CategoryDao() {}
    
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }
}

public record ProductDao
{
    public ProductDao() {}
    public Guid Id { get; init; }
    public Guid SellerId { get; init; }
    public long Sku { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public decimal PriceAmount { get; init; }
    public required string PriceCurrency { get; init; }
    public int CategoryId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public int Version { get; init; }
    public List<ProductImageDao> Images { get; init; } = [];
}

public record ProductImageDao
{
    public required string Url { get; init; }
}

public record OutboxMessageDao
{
    public OutboxMessageDao() {}
    public Guid Id { get; set; }
    public required string Type { get; set; }
    public required string Payload { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}