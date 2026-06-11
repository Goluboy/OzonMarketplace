using FluentAssertions;
using NSubstitute;
using ProductService.Application.Exceptions;
using ProductService.Application.Helpers;
using ProductService.Application.Services.Products;
using ProductService.Application.Services.Products.Query;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using Xunit;

namespace ProductService.UnitTests.Application.Services;

public class ProductQueryServiceTests
{
    private readonly IProductQueryRepository _repository = Substitute.For<IProductQueryRepository>();
    private readonly IProductImageUrlHelper _productImageUrlHelper = Substitute.For<IProductImageUrlHelper>();
    private readonly ProductQueryService _service;
    
    public ProductQueryServiceTests()
    {
        _service = new ProductQueryService(_repository, _productImageUrlHelper);
    }
    
    #region GetCatalogAsync Tests

    [Fact]
    public async Task GetCatalogAsync_WhenSearchIsSku_ShouldReturnCardsBySkuDirectly()
    {
        var ct = CancellationToken.None;
        var filter = new ProductSearchFilter(
            Search: " 123456 ",
            CategoryId: null,
            MinPrice: null,
            MaxPrice: null,
            SortBy: "price",
            SortOrder: "asc",
            Cursor: null,
            PageSize: 10
        );

        var skuCards = new List<ProductCardDto>
        {
            new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), CategoryId = 1, Name = "Phone Store 1", PriceAmount = 1000m, PriceCurrency = "RUB", MainImageUrl = "img1.png"},
            new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), CategoryId = 1, Name = "Phone Store 2", PriceAmount = 950m, PriceCurrency = "RUB", MainImageUrl = "img2.png"},
        };

        _repository.GetCardsAsync(123456).Returns(skuCards);
        _productImageUrlHelper.ToAbsoluteUrl("img1.png").Returns("http://img1.png");
        _productImageUrlHelper.ToAbsoluteUrl("img2.png").Returns("http://img2.png");

        var result = await _service.GetCatalogAsync(filter, ct);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.NextCursor.Should().BeNull();
        result.Items.First().MainImageUrl.Should().Be("http://img1.png");
        result.Items.Last().MainImageUrl.Should().Be("http://img2.png");

        await _repository.DidNotReceive().GetPagedAsync(Arg.Any<ProductSearchFilter>());
        await _repository.DidNotReceive().GetCardsAsync(Arg.Any<IReadOnlyList<Guid>>());
    }

    [Fact]
    public async Task GetCatalogAsync_WhenNoProductsFoundByFilter_ShouldReturnEmptyPage()
    {
        var ct = CancellationToken.None;
        var filter = new ProductSearchFilter(
            Search: "Шоколад",
            CategoryId: null,
            MinPrice: null,
            MaxPrice: null,
            SortBy: "name",
            SortOrder: "asc",
            Cursor: null,
            PageSize: 10
        );

        var emptyPagedResult = new ProductPagedIdsDto(new List<Guid>(), null);
        _repository.GetPagedAsync(filter).Returns(emptyPagedResult);

        var result = await _service.GetCatalogAsync(filter, ct);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.NextCursor.Should().BeNull();

        await _repository.DidNotReceive().GetCardsAsync(Arg.Any<IReadOnlyList<Guid>>());
    }

    [Fact]
    public async Task GetCatalogAsync_WhenProductsExist_ShouldReturnCardsSortedInOriginalDbOrder()
    {
        var ct = CancellationToken.None;
        var filter = new ProductSearchFilter(
            Search: "Ноутбук",
            CategoryId: null,
            MinPrice: null,
            MaxPrice: null,
            SortBy: "price",
            SortOrder: "desc",
            Cursor: null,
            PageSize: 10
        );

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var pagedIds = new List<Guid> { id3, id1, id2 };
        var dbPagedResult = new ProductPagedIdsDto(pagedIds, "next_page_cursor_token");
        _repository.GetPagedAsync(filter).Returns(dbPagedResult);

        var dbCards = new List<ProductCardDto>
        {
            new() { Id = id1, SellerId = sellerId, CategoryId = 1, Name = "Notebook Middle", PriceAmount = 1000m, PriceCurrency = "RUB", MainImageUrl = "img1.png"},
            new() { Id = id2, SellerId = sellerId, CategoryId = 1, Name = "Notebook Cheap", PriceAmount = 500m, PriceCurrency = "RUB", MainImageUrl = "img2.png"},
            new() { Id = id3, SellerId = sellerId, CategoryId = 1, Name = "Notebook Expensive", PriceAmount = 2000m, PriceCurrency = "RUB", MainImageUrl = "img3.png"},
        };
        _repository.GetCardsAsync(pagedIds).Returns(dbCards);
        _productImageUrlHelper.ToAbsoluteUrl("img1.png").Returns("http://img1.png");
        _productImageUrlHelper.ToAbsoluteUrl("img2.png").Returns("http://img2.png");
        _productImageUrlHelper.ToAbsoluteUrl("img3.png").Returns("http://img3.png");

        var result = await _service.GetCatalogAsync(filter, ct);

        result.Should().NotBeNull();
        result.NextCursor.Should().Be("next_page_cursor_token");
        result.Items.Should().HaveCount(3);

        var itemsList = result.Items.ToList();
        itemsList[0].Id.Should().Be(id3);
        itemsList[0].Name.Should().Be("Notebook Expensive");
        itemsList[0].MainImageUrl.Should().Be("http://img3.png");

        itemsList[1].Id.Should().Be(id1);
        itemsList[1].Name.Should().Be("Notebook Middle");
        itemsList[1].MainImageUrl.Should().Be("http://img1.png");

        itemsList[2].Id.Should().Be(id2);
        itemsList[2].Name.Should().Be("Notebook Cheap");
        itemsList[2].MainImageUrl.Should().Be("http://img2.png");
    }

    #endregion

    #region GetProductAsync Tests

    [Fact]
    public async Task GetProductAsync_WhenProductExists_ShouldReturnCorrectDetails()
    {
        var ct = CancellationToken.None;
        var productId = Guid.NewGuid();
        var detailsDto = new ProductDetailsDto
        {
            Id = productId,
            Sku = 12345,
            SellerId = Guid.NewGuid(),
            Name = "Smartphone",
            Description = "Full description",
            PriceAmount = 999m,
            PriceCurrency = "USD",
            CategoryId = 1,
            CategoryName = "Electronics",
            CategoryPath = "electronics",
            Images = [new ProductImageDto("img1.png")],
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _repository.GetDetailsAsync(productId).Returns(detailsDto);
        _productImageUrlHelper.ToAbsoluteImageDtos(Arg.Any<IEnumerable<ProductImageDto>>()).Returns([new ProductImageDto("http://img1.png")]);

        var result = await _service.GetProductAsync(productId, ct);

        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
        result.Name.Should().Be("Smartphone");
        result.Description.Should().Be("Full description");
        result.CategoryName.Should().Be("Electronics");
        result.Images.Should().ContainSingle().Which.Should().Be(new ProductImageDto("http://img1.png"));
    }

    [Fact]
    public async Task GetProductAsync_WhenProductDoesNotExist_ShouldThrowNotFoundException()
    {
        var ct = CancellationToken.None;
        var nonExistentId = Guid.NewGuid();

        _repository.GetDetailsAsync(nonExistentId).Returns((ProductDetailsDto?)null);

        var act = async () => await _service.GetProductAsync(nonExistentId, ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion
}