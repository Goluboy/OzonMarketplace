using System.ComponentModel.DataAnnotations;
using Core.Minio.Helpers;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ProductService.Application.DTO.Product;
using ProductService.Application.Exceptions;
using ProductService.Application.Services.Products.Command;
using ProductService.Domain.Entities;
using ProductService.Domain.ValueObjects;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;
using Xunit;

namespace ProductService.UnitTests.Application.Services;

public class ProductCommandServiceTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IS3UrlFormatter _s3UrlFormatter = Substitute.For<IS3UrlFormatter>();
    private readonly ProductCommandService _service;

    public ProductCommandServiceTests()
    {
        _service = new ProductCommandService(_unitOfWork, _productRepository, _categoryRepository, _s3UrlFormatter);
    }

    #region CreateProductAsync Tests

    [Fact]
    public async Task CreateProductAsync_WhenParametersAreValid_ShouldCreateProductAndCommitTransaction()
    {
        // Arrange
        var ct = CancellationToken.None;
        var dto = new CreateProductDto(
            Sku: 1001L,
            Name: "Игровой ноутбук",
            Description: "Мощное описание",
            CategoryId: 15,
            Price: new MoneyDto(120000m, "RUB"),
            ImagesUrl: new List<string> { "https://cdn.com/1.png" }
        );

        var dbCategory = Category.Reconstruct(15, "Ноутбуки", "electronics.laptops");
        _categoryRepository.GetAsync(15).Returns(dbCategory);

        // Act
        var result = await _service.CreateProductAsync(dto, ct);

        // Assert
        result.Should().NotBeNull();
        result.Sku.Should().Be(dto.Sku);
        result.Name.Should().Be(dto.Name);
        result.PriceAmount.Should().Be(dto.Price.Amount);

        await _categoryRepository.Received(1).GetAsync(15);
        await _unitOfWork.Received(1).BeginTransactionAsync(ct);
        await _productRepository.Received(1).AddAsync(Arg.Is<Product>(p => p.Sku == dto.Sku));
        await _unitOfWork.Received(1).CommitAsync();
        await _unitOfWork.DidNotReceive().RollbackAsync();
    }

    [Fact]
    public async Task CreateProductAsync_WhenCategoryDoesNotExist_ShouldThrowValidationExceptionAndNotStartTransaction()
    {
        // Arrange
        var ct = CancellationToken.None;
        var dto = new CreateProductDto(1001L, "Product", "Desc", new MoneyDto(100m, "USD"), 99, []);
        
        _categoryRepository.GetAsync(99).Returns((Category?)null);

        // Act
        var act = () => _service.CreateProductAsync(dto, ct);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Category with id 99 does not exist.*");

        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _productRepository.DidNotReceive().AddAsync(Arg.Any<Product>());
    }

    [Fact]
    public async Task CreateProductAsync_WhenDatabaseThrowsError_ShouldRollbackTransactionAndPropagateException()
    {
        // Arrange
        var ct = CancellationToken.None;
        var dto = new CreateProductDto(1001L, "Product", "Desc", new MoneyDto(100m, "USD"), 15, []);

        var dbCategory = Category.Reconstruct(15, "Category", "path");
        _categoryRepository.GetAsync(15).Returns(dbCategory);
        
        _productRepository.AddAsync(Arg.Any<Product>()).Throws(new Exception("Database connection lost."));

        // Act
        var act = () => _service.CreateProductAsync(dto, ct);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Database connection lost.");

        await _unitOfWork.Received(1).BeginTransactionAsync(ct);
        await _productRepository.Received(1).AddAsync(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().CommitAsync();
        await _unitOfWork.Received(1).RollbackAsync();
    }

    [Fact]
    public async Task CreateProductAsync_WhenCancellationIsRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var dto = new CreateProductDto(1001L, "Product", "Desc", new MoneyDto(100m, "USD"), 15, []);

        // Act
        var act = () => _service.CreateProductAsync(dto, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        await _productRepository.DidNotReceive().AddAsync(Arg.Any<Product>());
    }

    #endregion

    #region UpdateProductAsync Tests

    [Fact]
    public async Task UpdateProductAsync_WhenNoFieldsAreChanged_ShouldReturnDtoDirectlyWithoutDbUpdateOrTransaction()
    {
        // Arrange
        var ct = CancellationToken.None;
        var productId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        
        var dto = new UpdateProductDto(
            ProductId: productId,
            Name: "Same Name",
            Description: "Same Description",
            CategoryId: 15,
            Price: new MoneyDto(500m, "USD"),
            ImagesUrl: ["http://img1.png"]
        );

        var dbCategory = Category.Reconstruct(15, "Category", "path");
        _categoryRepository.GetAsync(15).Returns(dbCategory);

        var existingProduct = Product.Reconstruct(
            id: productId,
            sku: 1001,
            sellerId: sellerId,
            name: "Same Name",
            description: "Same Description",
            price: new Money(500m, "USD"),
            categoryId: 15,
            createdAt: DateTimeOffset.UtcNow.AddDays(-1),
            updatedAt: DateTimeOffset.UtcNow.AddDays(-1),
            version: 3,
            images: [new ProductImage("http://img1.png")]
        );
        _productRepository.GetAsync(productId).Returns(existingProduct);

        // Act
        var result = await _service.UpdateProductAsync(dto, ct);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productId);

        await _productRepository.Received(1).GetAsync(productId);
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _productRepository.DidNotReceive().UpdateAsync(Arg.Any<Product>());
    }

    [Fact]
    public async Task UpdateProductAsync_WhenFieldsAreChanged_ShouldApplyChangesAndUpdateInDbUnderTransaction()
    {
        // Arrange
        var ct = CancellationToken.None;
        var productId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        
        var dto = new UpdateProductDto(
            ProductId: productId,
            Name: "NEW Name",
            Description: "Same Description",
            CategoryId: 15,
            Price: new MoneyDto(600m, "USD"),
            ImagesUrl: ["http://img1.png"]
        );

        var dbCategory = Category.Reconstruct(15, "Category", "path");
        _categoryRepository.GetAsync(15).Returns(dbCategory);

        var existingProduct = Product.Reconstruct(
            productId, sellerId, 1001L, "Old Name", "Same Description", new Money(500m, "USD"), 15,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 1, [new ProductImage("http://img1.png")]
        );
        _productRepository.GetAsync(productId).Returns(existingProduct);

        // Act
        var result = await _service.UpdateProductAsync(dto, ct);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("NEW Name");
        result.PriceAmount.Should().Be(600m);

        await _unitOfWork.Received(1).BeginTransactionAsync(ct);
        await _productRepository.Received(1).UpdateAsync(Arg.Is<Product>(p => p.Name == "NEW Name" && p.Price.Amount == 600m));
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task UpdateProductAsync_WhenProductDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var ct = CancellationToken.None;
        var nonExistentId = Guid.NewGuid();
        var dto = new UpdateProductDto(nonExistentId, "Name", "Desc", new MoneyDto(100m, "USD"), 15, []);

        var dbCategory = Category.Reconstruct(15, "Category", "path");
        _categoryRepository.GetAsync(15).Returns(dbCategory);
        _productRepository.GetAsync(nonExistentId).Returns((Product?)null);

        // Act
        var act = () => _service.UpdateProductAsync(dto, ct);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteProductAsync Tests

    [Fact]
    public async Task DeleteProductAsync_WhenProductExists_ShouldDeleteProductAndCommitTransaction()
    {
        // Arrange
        var ct = CancellationToken.None;
        var productId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var existingProduct = Product.Reconstruct(
            productId, sellerId, 1011L,"Name", "Desc", new Money(100m, "USD"), 15,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 1, []
        );
        _productRepository.GetAsync(productId).Returns(existingProduct);

        // Act
        await _service.DeleteProductAsync(productId, ct);

        // Assert
        await _productRepository.Received(1).GetAsync(productId);
        await _unitOfWork.Received(1).BeginTransactionAsync(ct);
        await _productRepository.Received(1).DeleteAsync(productId);
        await _unitOfWork.Received(1).CommitAsync();
        await _unitOfWork.DidNotReceive().RollbackAsync();
    }

    [Fact]
    public async Task DeleteProductAsync_WhenProductDoesNotExist_ShouldThrowNotFoundExceptionAndNotStartTransaction()
    {
        // Arrange
        var ct = CancellationToken.None;
        var nonExistentId = Guid.NewGuid();
        _productRepository.GetAsync(nonExistentId).Returns((Product?)null);

        // Act
        var act = () => _service.DeleteProductAsync(nonExistentId, ct);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _productRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteProductAsync_WhenDatabaseErrorOccurs_ShouldRollbackTransaction()
    {
        // Arrange
        var ct = CancellationToken.None;
        var productId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var existingProduct = Product.Reconstruct(
            productId, sellerId, 1011L, "Name", "Desc", new Money(100m, "USD"), 15,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 1, []
        );
        _productRepository.GetAsync(productId).Returns(existingProduct);
        
        _productRepository.DeleteAsync(productId).Throws(new Exception("Database timeout."));

        // Act
        var act = () => _service.DeleteProductAsync(productId, ct);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Database timeout.");
        await _unitOfWork.Received(1).BeginTransactionAsync(ct);
        await _unitOfWork.DidNotReceive().CommitAsync();
        await _unitOfWork.Received(1).RollbackAsync();
    }

    #endregion
}