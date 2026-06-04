using Dapper;
using FluentAssertions;
using Npgsql;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Product;
using ProductService.Infrastructure.DAO;
using ProductService.Infrastructure.Helpers;
using ProductService.Infrastructure.Helpers.JsonbSerialization;

namespace ProductService.IntegrationTests.Infrastructure.Repository;

public class ProductQueryRepositoryTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private readonly IProductQueryRepository _repository;

    public ProductQueryRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _repository = _fixture.CreateProductQueryRepository(_fixture.CreateConnectionFactory());
        
        SqlMapper.AddTypeHandler(new JsonbTypeHandler<List<ProductImageDao>>());
        SqlMapper.AddTypeHandler(new JsonbTypeHandler<List<string>>());
    }
    
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetAsync("products, categories");
    }
    
    #region Вспомогательные методы подготовки данных

    private async Task<int> InsertCategoryDirectlyAsync(string name, string path)
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        
        const string sql = "INSERT INTO categories (name, path) VALUES (@Name, @Path) RETURNING id;";
        return await connection.ExecuteScalarAsync<int>(sql, new { Name = name, Path = path });
    }
    
    private async Task SeedProductAsync(
        Guid id,
        long sku,
        Guid sellerId,
        string name,
        string description,
        decimal priceAmount,
        string priceCurrency,
        int categoryId,
        string imagesJson,
        DateTimeOffset createdAt)
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        
        const string sql = """
                           INSERT INTO products (id, sku, seller_id, name, description, price_amount, price_currency, category_id, images, created_at, updated_at)
                           VALUES (@Id, @Sku, @SellerId, @Name, @Description, @PriceAmount, @PriceCurrency, @CategoryId, @Images::jsonb, @CreatedAt, @CreatedAt);
                           """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            Sku = sku,
            SellerId = sellerId,
            Name = name,
            Description = description,
            PriceAmount = priceAmount,
            PriceCurrency = priceCurrency,
            CategoryId = categoryId,
            Images = imagesJson,
            CreatedAt = createdAt
        });
    }
    
    #endregion
    
    #region GetCardByIdAsync Tests

    [Fact]
    public async Task GetCardByIdAsync_WhenProductExists_ShouldReturnCorrectCardWithMainImage()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        var productId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        const string imagesJson = "[{\"Url\": \"http://img1.png\"}, {\"Url\": \"http://img2.png\"}]";
        
        await SeedProductAsync(productId, 12345, sellerId, "Smartphone", "Desc",
            999.99m, "RUB", categoryId, imagesJson, DateTimeOffset.UtcNow);
        
        // Act
        var result = await _repository.GetCardByIdAsync(productId);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
        result.Name.Should().Be("Smartphone");
        result.CategoryId.Should().Be(categoryId);
        result.PriceAmount.Should().Be(999.99m);
        result.PriceCurrency.Should().Be("RUB");
        result.MainImageUrl.Should().Be("http://img1.png");
    }

    [Fact]
    public async Task GetCardByIdAsync_WhenProductHasNoImages_ShouldReturnCardWithEmptyMainImageUrl()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        var productId = Guid.NewGuid();
        
        await SeedProductAsync(productId, 12345, Guid.NewGuid(), "Smartphone", "Desc",
            999.99m, "USD", categoryId, "[]", DateTimeOffset.UtcNow);

        // Act
        var result = await _repository.GetCardByIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.MainImageUrl.Should().BeEmpty();
    }
    
    [Fact]
    public async Task GetCardByIdAsync_WhenProductDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetCardByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }
    
    #endregion
    
    #region GetDetailsByIdAsync Tests

    [Fact]
    public async Task GetDetailsByIdAsync_WhenProductAndCategoryExist_ShouldReturnFullDetailsWithCategoryNameAndPath()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Clothing", "clothing");
        var productId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var imagesJson = "[\"http://img1.png\"]";
        var createdAt = DateTimeOffset.UtcNow;
        
        await SeedProductAsync(productId, 555, sellerId, "T-Shirt", "Best T-Shirt",
            19.99m, "USD", categoryId, imagesJson, createdAt);
        
        // Act
        var result = await _repository.GetDetailsByIdAsync(productId);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
        result.Sku.Should().Be(555);
        result.SellerId.Should().Be(sellerId);
        result.Name.Should().Be("T-Shirt");
        result.Description.Should().Be("Best T-Shirt");
        result.PriceAmount.Should().Be(19.99m);
        result.PriceCurrency.Should().Be("USD");
        result.CategoryId.Should().Be(categoryId);
        result.CategoryName.Should().Be("Clothing");
        result.CategoryPath.Should().Be("clothing");
        result.Images.Should().ContainSingle().Which.Should().Be("http://img1.png");
    }

    [Fact]
    public async Task GetDetailsByIdAsync_WhenCategoryDoesNotExist_ShouldReturnNullCategoryNameAndPath()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = await InsertCategoryDirectlyAsync("Temp", "temp");
        await SeedProductAsync(productId, 777, Guid.NewGuid(), "No Cat Product", "Desc", 
            10m, "USD", categoryId, "[]", DateTimeOffset.UtcNow);

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync("ALTER TABLE products DROP CONSTRAINT fk_products_categories;");
        await connection.ExecuteAsync($"DELETE FROM categories WHERE id = {categoryId};");
        
        // Act
        var result = await _repository.GetDetailsByIdAsync(productId);
        
        // Assert
        result.Should().NotBeNull();
        result.CategoryName.Should().BeNull();
        result.CategoryPath.Should().BeNull();
    }
    
    [Fact]
    public async Task GetDetailsByIdAsync_WhenProductDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetDetailsByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion
    
    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_WithBasicFilter_ShouldReturnAllProductsInCorrectOrder()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        
        await SeedProductAsync(id1, 101, Guid.NewGuid(), "Product A", "Desc",
            100m, "RUB", categoryId, "[]", now.AddMinutes(-5));
        await SeedProductAsync(id2, 102, Guid.NewGuid(), "Product B", "Desc", 
            200m, "RUB", categoryId, "[]", now);
        
        var filter = new ProductSearchFilter(
            Search: null,
            CategoryId: 1,
            MinPrice: null,
            MaxPrice: null,
            SortBy: "createdAt",
            SortOrder: "desc",
            PageSize: 10,
            Cursor: null
        );
        
        // Act
        var result = await _repository.GetPagedAsync(filter);
        
        // Assert
        result.ProductIds.Should().HaveCount(2);
        result.ProductIds[0].Should().Be(id2);
        result.ProductIds[1].Should().Be(id1);
        result.NextCursor.Should().BeNull();
    }
    
    [Fact]
    public async Task GetPagedAsync_WhenHasNextPage_ShouldReturnCorrectLimitAndCursor()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        
        await SeedProductAsync(id1, 101, Guid.NewGuid(), "Product A", "Desc", 
            100m, "RUB", categoryId, "[]", now.AddMinutes(-5));
        await SeedProductAsync(id2, 102, Guid.NewGuid(), "Product B", "Desc",
            200m, "RUB", categoryId, "[]", now);
        
        var filter = new ProductSearchFilter(
            Search: null,
            CategoryId: categoryId,
            MinPrice: null,
            MaxPrice: null,
            SortBy: "createdAt",
            SortOrder: "desc",
            PageSize: 1,
            Cursor: null
        );

        // Act
        var result = await _repository.GetPagedAsync(filter);

        // Assert
        result.ProductIds.Should().ContainSingle();
        result.ProductIds[0].Should().Be(id2);
        result.NextCursor.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task GetPagedAsync_WithPriceRangeFilter_ShouldFilterCorrectly()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await SeedProductAsync(id1, 101, Guid.NewGuid(), "Cheap Phone", "Desc",
            150m, "RUB", categoryId, "[]", DateTimeOffset.UtcNow);
        await SeedProductAsync(id2, 102, Guid.NewGuid(), "Expensive Phone", "Desc",
            990m, "RUB", categoryId, "[]", DateTimeOffset.UtcNow);
        
        var filter = new ProductSearchFilter(
            Search: null,
            CategoryId: categoryId,
            MinPrice: new MoneyDto(100m, "RUB"),
            MaxPrice: new MoneyDto(500m, "RUB"),
            SortBy: "price",
            SortOrder: "asc",
            PageSize: 10,
            Cursor: null
        );

        // Act
        var result = await _repository.GetPagedAsync(filter);

        // Assert
        result.ProductIds.Should().ContainSingle();
        result.ProductIds[0].Should().Be(id1);
    }
    
    [Fact]
    public async Task GetPagedAsync_WithFullTextSearch_ShouldFindMatchingProducts()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await SeedProductAsync(id1, 101, Guid.NewGuid(), "Кожаные туфли", "Мужские туфли из кожи",
            5000m, "RUB", categoryId, "[]", DateTimeOffset.UtcNow);
        await SeedProductAsync(id2, 102, Guid.NewGuid(), "Красные кроссовки", "Спортивная обувь",
            4000m, "RUB", categoryId, "[]", DateTimeOffset.UtcNow);
        
        var filter = new ProductSearchFilter(
            Search: "туфли",
            CategoryId: categoryId,
            MinPrice: null,
            MaxPrice: null,
            SortBy: "name",
            SortOrder: "asc",
            PageSize: 10,
            Cursor: null
        );

        // Act
        var result = await _repository.GetPagedAsync(filter);

        // Assert
        result.ProductIds.Should().ContainSingle();
        result.ProductIds[0].Should().Be(id1);
    }
    
    [Fact]
    public async Task GetPagedAsync_WithInvalidCursorFormat_ShouldThrowFormatException()
    {
        // Arrange
        var corruptedCursor = CursorHelper.Encode("NOT_A_DECIMAL_PRICE", Guid.NewGuid());

        var filter = new ProductSearchFilter(
            Search: null,
            CategoryId: 1,
            MinPrice: null,
            MaxPrice: null,
            SortBy: "price",
            SortOrder: "asc",
            PageSize: 10,
            Cursor: corruptedCursor
        );

        // Act
        Func<Task> act = async () => await _repository.GetPagedAsync(filter);

        // Assert
        await act.Should().ThrowAsync<FormatException>();
    }
    
    [Fact]
    public async Task GetPagedAsync_WithCursor_ShouldPaginateCorrectlyAcrossPages()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        
        await SeedProductAsync(id1, 101, Guid.NewGuid(), "Product Oldest", "Desc",
            100m, "RUB", categoryId, "[]", now.AddMinutes(-10));
        await SeedProductAsync(id2, 102, Guid.NewGuid(), "Product Middle", "Desc",
            100m, "RUB", categoryId, "[]", now.AddMinutes(-5));
        await SeedProductAsync(id3, 103, Guid.NewGuid(), "Product Newest", "Desc",
            100m, "RUB", categoryId, "[]", now);

        // Страница 1
        var filterPage1 = new ProductSearchFilter(
            Search: null,
            CategoryId: categoryId,
            MinPrice: null,
            MaxPrice: null,
            SortBy: "createdAt",
            SortOrder: "desc", 
            PageSize: 1,  
            Cursor: null
        );

        // Act - Запрос страницы 1
        var resultPage1 = await _repository.GetPagedAsync(filterPage1);

        // Assert - Страница 1
        resultPage1.ProductIds.Should().ContainSingle();
        resultPage1.ProductIds[0].Should().Be(id3);
        resultPage1.NextCursor.Should().NotBeNullOrEmpty();

        // Страница 2
        var filterPage2 = new ProductSearchFilter(
            Search: null,
            CategoryId: categoryId,
            MinPrice: null,
            MaxPrice: null,
            SortBy: "createdAt",
            SortOrder: "desc",
            PageSize: 1,
            Cursor: resultPage1.NextCursor
        );

        // Act - Запрос страницы 2
        var resultPage2 = await _repository.GetPagedAsync(filterPage2);

        // Assert - Страница 2
        resultPage2.ProductIds.Should().ContainSingle();
        resultPage2.ProductIds[0].Should().Be(id2);
        resultPage2.NextCursor.Should().NotBeNullOrEmpty();

        // --- СТРАНИЦА 3 ---
        var filterPage3 = new ProductSearchFilter(
            Search: null,
            CategoryId: categoryId,
            MinPrice: null,
            MaxPrice: null,
            SortBy: "createdAt",
            SortOrder: "desc",
            PageSize: 1,
            Cursor: resultPage2.NextCursor
        );

        // Act - Запрос страницы 3
        var resultPage3 = await _repository.GetPagedAsync(filterPage3);

        // Assert - Страница 3
        resultPage3.ProductIds.Should().ContainSingle();
        resultPage3.ProductIds[0].Should().Be(id1);
        resultPage3.NextCursor.Should().BeNull();
    }
    
    [Fact]
    public async Task GetPagedAsync_WithDuplicateSortValues_ShouldUseIdAsTieBreakerCorrectly()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        
        var uuidA = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var uuidB = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var uuidC = Guid.Parse("00000000-0000-0000-0000-000000000003");
        
        var now = DateTimeOffset.UtcNow;
        await SeedProductAsync(uuidB, 102, Guid.NewGuid(), "Product B", "Desc",
            500m, "RUB", categoryId, "[]", now);
        await SeedProductAsync(uuidC, 103, Guid.NewGuid(), "Product C", "Desc",
            500m, "RUB", categoryId, "[]", now);
        await SeedProductAsync(uuidA, 101, Guid.NewGuid(), "Product A", "Desc",
            500m, "RUB", categoryId, "[]", now);

        // Сортируем по цене по возрастанию ('asc').
        // Ожидаемый порядок из-за одинаковой цены будет определяться строго по возрастанию UUID: A -> B -> C.
        
        // Страница 1
        var filterPage1 = new ProductSearchFilter(
            Search: null,
            CategoryId: categoryId,
            MinPrice: null,
            MaxPrice: null,
            SortBy: "price",
            SortOrder: "asc", 
            PageSize: 2,
            Cursor: null
        );

        // Act - Запрос страницы 1
        var resultPage1 = await _repository.GetPagedAsync(filterPage1);

        // Assert - Страница 1
        resultPage1.ProductIds.Should().HaveCount(2);
        resultPage1.ProductIds[0].Should().Be(uuidA);
        resultPage1.ProductIds[1].Should().Be(uuidB);
        resultPage1.NextCursor.Should().NotBeNullOrEmpty();

        // Страница 2
        var filterPage2 = new ProductSearchFilter(
            Search: null,
            CategoryId: categoryId,
            MinPrice: null,
            MaxPrice: null,
            SortBy: "price",
            SortOrder: "asc",
            PageSize: 2,
            Cursor: resultPage1.NextCursor
        );

        // Act - Запрос страницы 2
        var resultPage2 = await _repository.GetPagedAsync(filterPage2);

        // Assert - Страница 2
        resultPage2.ProductIds.Should().ContainSingle();
        resultPage2.ProductIds[0].Should().Be(uuidC);
        resultPage2.NextCursor.Should().BeNull();
    }
    
    #endregion
    
    #region GetCardsBySkuAsync Tests

    [Fact]
    public async Task GetCardsBySkuAsync_WhenMultipleProductsShareSameSku_ShouldReturnAllMatchingCards()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");

        const long targetSku = 999000;
        var seller1Id = Guid.NewGuid();
        var seller2Id = Guid.NewGuid();
        
        var idSeller1 = Guid.NewGuid();
        var idSeller2 = Guid.NewGuid();
        var idOtherProduct = Guid.NewGuid();
        
        await SeedProductAsync(idSeller1, targetSku, seller1Id, "iPhone 15 (Store 1)", "Desc", 999m, "USD", categoryId, "[{\"Url\": \"http://img1.png\"}]", DateTimeOffset.UtcNow);
        await SeedProductAsync(idSeller2, targetSku, seller2Id, "iPhone 15 (Store 2)", "Desc", 950m, "USD", categoryId, "[{\"Url\": \"http://img2.png\"}]", DateTimeOffset.UtcNow);
        
        await SeedProductAsync(idOtherProduct, 111111, Guid.NewGuid(), "Other Product", "Desc", 100m, "USD", categoryId, "[]", DateTimeOffset.UtcNow);

        // Act
        var result = await _repository.GetCardsBySkuAsync(targetSku);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        
        var card1 = result.Should().Contain(c => c.Id == idSeller1).Subject;
        card1.Name.Should().Be("iPhone 15 (Store 1)");
        card1.PriceAmount.Should().Be(999m);
        card1.MainImageUrl.Should().Be("http://img1.png");
        
        var card2 = result.Should().Contain(c => c.Id == idSeller2).Subject;
        card2.Name.Should().Be("iPhone 15 (Store 2)");
        card2.PriceAmount.Should().Be(950m);
        card2.MainImageUrl.Should().Be("http://img2.png");
    }

    [Fact]
    public async Task GetCardsBySkuAsync_WhenProductHasNoImages_ShouldReturnCardWithEmptyMainImageUrl()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        var productId = Guid.NewGuid();
        const long targetSku = 888888;
        
        await SeedProductAsync(productId, targetSku, Guid.NewGuid(), "No Image Phone", "Desc", 500m, "USD", categoryId, "[]", DateTimeOffset.UtcNow);

        // Act
        var result = await _repository.GetCardsBySkuAsync(targetSku);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(productId);
        result[0].MainImageUrl.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCardsBySkuAsync_WhenNoProductsMatchSku_ShouldReturnEmptyList()
    {
        // Arrange
        var categoryId = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        
        await SeedProductAsync(Guid.NewGuid(), 111111, Guid.NewGuid(), "Product", "Desc", 100m, "USD", categoryId, "[]", DateTimeOffset.UtcNow);

        // Act
        var result = await _repository.GetCardsBySkuAsync(999999);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion
}