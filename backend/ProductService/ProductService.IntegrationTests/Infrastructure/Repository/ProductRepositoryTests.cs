using System.Data;
using Dapper;
using FluentAssertions;
using Npgsql;
using ProductService.Domain.Entities;
using ProductService.Domain.ValueObjects;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using ProductService.Infrastructure.DAO;
using ProductService.Infrastructure.Helpers.JsonbSerialization;

namespace ProductService.IntegrationTests.Infrastructure.Repository;

public class ProductRepositoryTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private readonly IProductRepository _repository;

    public ProductRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        
        SqlMapper.AddTypeHandler(new JsonbTypeHandler<List<ProductImageDao>>());
        
        var (_, session) = _fixture.CreateUnitOfWorkContext();
        _repository = _fixture.CreateProductRepository(session);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    
    public async Task DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync("TRUNCATE products, categories RESTART IDENTITY CASCADE;");
    }

    #region Вспомогательные методы (Seed Helpers & Factory)

    private async Task<int> InsertCategoryDirectlyAsync(string name, string path)
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        
        const string sql = "INSERT INTO categories (name, path) VALUES (@Name, @Path) RETURNING id;";
        return await connection.ExecuteScalarAsync<int>(sql, new { Name = name, Path = path });
    }

    private async Task SeedProductDirectlyAsync(ProductDao dao)
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        const string sql = """
            INSERT INTO products (id, sku, seller_id, name, description, price_amount, price_currency, category_id, created_at, updated_at, version, images)
            VALUES (@Id, @Sku, @SellerId, @Name, @Description, @PriceAmount, @PriceCurrency, @CategoryId, @CreatedAt, @UpdatedAt, @Version, @Images::jsonb);
            """;
        await connection.ExecuteAsync(sql, dao);
    }
    
    private Product CreateTestProduct(int categoryId, int version = 1)
    {
        var price = new Money(1500m, "RUB");
        var images = new List<ProductImage> { new("http://img1.png") };
        
        var product = Product.Create(
            sku: 111222,
            sellerId: Guid.NewGuid(),
            name: "Test Product",
            description: "Test Description",
            price: price,
            categoryId: categoryId,
            images: images
        );

        typeof(Product).GetProperty(nameof(Product.Version))?.SetValue(product, version);

        return product;
    }

    #endregion
    
    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ShouldReturnCorrectDomainModel()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");

        var productId = Guid.NewGuid();
        var dao = new ProductDao
        {
            Id = productId,
            Sku = 123456,
            SellerId = Guid.NewGuid(),
            Name = "Smartphone",
            Description = "Latest smartphone",
            PriceAmount = 999.99m,
            PriceCurrency = "USD",
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow.ToUniversalTime(),
            UpdatedAt = DateTimeOffset.UtcNow.ToUniversalTime(),
            Version = 1,
            Images = [new ProductImageDao("http://image.png")]
        };

        await SeedProductDirectlyAsync(dao);

        var (uow, _) = _fixture.CreateUnitOfWorkContext();

        // Act
        await uow.BeginTransactionAsync();
        var result = await _repository.GetByIdAsync(productId);
        await uow.CommitAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
        result.Sku.Should().Be(123456);
        result.Name.Should().Be("Smartphone");
        result.Price.Amount.Should().Be(999.99m);
        result.Price.Currency.Should().Be("USD");
        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var (uow, _) = _fixture.CreateUnitOfWorkContext();

        // Act
        await uow.BeginTransactionAsync();
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        await uow.CommitAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion
    
    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidProduct_ShouldPersistSuccessfullyInDatabase()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        
        var (uow, session) = _fixture.CreateUnitOfWorkContext();
        var repository = _fixture.CreateProductRepository(session);
        
        var product = CreateTestProduct(categoryId, 1);

        // Act
        await uow.BeginTransactionAsync();
        await repository.AddAsync(product);
        await uow.CommitAsync();

        // Assert
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        
        var dbRow = await connection.QueryFirstOrDefaultAsync<ProductDao>(
            "SELECT id, name, price_amount, category_id, version FROM products WHERE id = @Id;", 
            new { Id = product.Id });

        dbRow.Should().NotBeNull();
        dbRow.Id.Should().Be(product.Id);
        dbRow.Name.Should().Be(product.Name);
        dbRow.PriceAmount.Should().Be(product.Price.Amount);
        dbRow.CategoryId.Should().Be(categoryId);
        dbRow.Version.Should().Be(product.Version);
    }

    #endregion
    
    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenNoConcurrencyConflict_ShouldUpdateSuccessfullyAndIncrementVersion()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        
        var product = CreateTestProduct(categoryId, version: 2); 
        
        var initialDao = new ProductDao
        {
            Id = product.Id,
            Sku = 123456,
            SellerId = Guid.NewGuid(),
            Name = "Old Name",
            Description = "Old Desc",
            PriceAmount = 100m,
            PriceCurrency = "RUB",
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow.ToUniversalTime(),
            UpdatedAt = DateTimeOffset.UtcNow.ToUniversalTime(),
            Version = 1,
            Images = []
        };
        await SeedProductDirectlyAsync(initialDao);
        
        typeof(Product).GetProperty(nameof(Product.Name))?.SetValue(product, "New Name");

        var (uow, _) = _fixture.CreateUnitOfWorkContext();

        // Act
        await uow.BeginTransactionAsync();
        await _repository.UpdateAsync(product);
        await uow.CommitAsync();

        // Assert
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        
        var dbRow = await connection.QueryFirstOrDefaultAsync<ProductDao>(
            "SELECT name, version FROM products WHERE id = @Id;", new { Id = product.Id });

        dbRow.Should().NotBeNull();
        dbRow.Name.Should().Be("New Name");
        dbRow.Version.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_WhenConcurrencyConflictExists_ShouldThrowDBConcurrencyException()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        
        var product = CreateTestProduct(categoryId, version: 2); 
        
        var initialDao = new ProductDao
        {
            Id = product.Id,
            Sku = 123456,
            SellerId = Guid.NewGuid(),
            Name = "Initial Name",
            Description = "Desc",
            PriceAmount = 100m,
            PriceCurrency = "RUB",
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow.ToUniversalTime(),
            UpdatedAt = DateTimeOffset.UtcNow.ToUniversalTime(),
            Version = 2,
            Images = []
        };
        await SeedProductDirectlyAsync(initialDao);

        var (uow, _) = _fixture.CreateUnitOfWorkContext();

        // Act
        await uow.BeginTransactionAsync();
        var act = async () => await _repository.UpdateAsync(product);

        // Assert
        await act.Should().ThrowAsync<DBConcurrencyException>()
            .WithMessage($"Concurrency conflict. Product with ID {product.Id} has been modified by another process.");

        await uow.RollbackAsync();
    }

    #endregion
    
    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenProductExists_ShouldRemoveRecordSuccessfully()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        
        var productId = Guid.NewGuid();
        var dao = new ProductDao
        {
            Id = productId,
            Sku = 123456,
            SellerId = Guid.NewGuid(),
            Name = "To Be Deleted",
            Description = "Desc",
            PriceAmount = 100m,
            PriceCurrency = "RUB",
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow.ToUniversalTime(),
            UpdatedAt = DateTimeOffset.UtcNow.ToUniversalTime(),
            Version = 1,
            Images = []
        };
        await SeedProductDirectlyAsync(dao);

        var (uow, _) = _fixture.CreateUnitOfWorkContext();

        // Act
        await uow.BeginTransactionAsync();
        await _repository.DeleteAsync(productId);
        await uow.CommitAsync();

        // Assert
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        
        var exists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM products WHERE id = @Id);", new { Id = productId });

        exists.Should().BeFalse();
    }

    #endregion
}