using FluentAssertions;
using ProductService.Domain.Entities;
using ProductService.Domain.Events;
using ProductService.Domain.ValueObjects;
using Xunit;

namespace ProductService.UnitTests.Domain;

public class ProductTests(ProductFixture fixture) : IClassFixture<ProductFixture>
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateProduct()
    {
        // Arrange
        // Act
        var product = Product.Create(
            fixture.DefaultSku,
            fixture.DefaultName, 
            fixture.DefaultDescription,
            fixture.DefaultCategoryId,
            fixture.DefaultPrice,
            fixture.DefaultImages);
        
        // Assert
        product.Should().NotBeNull();
        product.Id.Should().NotBeEmpty();
        product.Name.Should().Be(fixture.DefaultName);
        product.Description.Should().Be(fixture.DefaultDescription);
        product.CategoryId.Should().Be(fixture.DefaultCategoryId);
        product.Price.Should().Be(fixture.DefaultPrice);
        product.Images.Should().HaveCount(1);
        product.Version.Should().Be(1);
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        product.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreatedEvent>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WhenSkuIsZeroOrNegative_ShouldThrowArgumentException(int invalidSku)
    {
        // Arrange
        // Act
        Action act = () => Product.Create(
            invalidSku,
            fixture.DefaultName,
            fixture.DefaultDescription,
            fixture.DefaultCategoryId,
            fixture.DefaultPrice,
            fixture.DefaultImages);
        
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("sku");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WhenNameIsNullOrEmpty_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange
        // Act
        Action act = () => Product.Create(
            fixture.DefaultSku,
            invalidName!,
            fixture.DefaultDescription,
            fixture.DefaultCategoryId,
            fixture.DefaultPrice,
            fixture.DefaultImages);
        
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void UpdateDetails_WhenValidParameters_ShouldUpdateProductDetails()
    {
        // Assert
        var product = Product.Create(
            fixture.DefaultSku,
            fixture.DefaultName, 
            fixture.DefaultDescription,
            fixture.DefaultCategoryId,
            fixture.DefaultPrice,
            fixture.DefaultImages);

        var newName = "Updated Product Name";
        var newDescription = "Updated Description";
        var newCategoryId = Guid.NewGuid();
        
        var initialVersion = product.Version;
        product.ClearDomainEvents();
        
        // Act
        product.UpdateDetails(newName, newDescription, newCategoryId);
        
        // Assert
        product.Name.Should().Be(newName);
        product.Description.Should().Be(newDescription);
        product.CategoryId.Should().Be(newCategoryId);
        
        product.Version.Should().Be(initialVersion + 1);
        product.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductDetailsUpdatedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                ProductId = product.Id,
                NewName = newName,
                NewDescription = newDescription,
                NewCategoryId = newCategoryId
            });
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void UpdateDetails_WhenNameIsNullOrEmpty_ShouldThrowArgumentException(string? invalidName)
    {
        var product = Product.Create(
            fixture.DefaultSku,
            fixture.DefaultName, 
            fixture.DefaultDescription,
            fixture.DefaultCategoryId,
            fixture.DefaultPrice,
            fixture.DefaultImages);
        
        var validDescription = "Some Description";
        var validCategoryId = Guid.NewGuid();

        var act = () => product.UpdateDetails(invalidName!, validDescription, validCategoryId);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void UpdateDetails_WhenCategoryIdIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var product = Product.Create(
            fixture.DefaultSku,
            fixture.DefaultName, 
            fixture.DefaultDescription,
            fixture.DefaultCategoryId,
            fixture.DefaultPrice,
            fixture.DefaultImages);
        var validName = "Valid Name";
        var validDescription = "Valid Description";
        var emptyCategoryId = Guid.Empty;

        // Act
        var act = () => product.UpdateDetails(validName, validDescription, emptyCategoryId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("categoryId");
    }
    
    [Fact]
    public void ChangePrice_WithValidPrice_ShouldUpdatePriceAndRaiseEvent()
    {
        // Assert
        var product = Product.Create(
            fixture.DefaultSku,
            fixture.DefaultName, 
            fixture.DefaultDescription,
            fixture.DefaultCategoryId,
            fixture.DefaultPrice,
            fixture.DefaultImages);
        
        var newPrice = new Money(999);
        var initialVersion = product.Version;
        product.ClearDomainEvents();
        
        // Act
        product.ChangePrice(newPrice);
        
        // Assert
        product.Price.Should().Be(newPrice);
        product.Version.Should().Be(initialVersion + 1);
        product.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductPriceChangedEvent>();
    }
    
    [Fact]
    public void ChangePrice_WhenPriceIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var product = Product.Create(
            fixture.DefaultSku,
            fixture.DefaultName, 
            fixture.DefaultDescription,
            fixture.DefaultCategoryId,
            fixture.DefaultPrice,
            fixture.DefaultImages);

        // Act
        var act = () => product.ChangePrice(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("newPrice");
    }
    
    [Fact]
    public void AddImage_WhenImageIsNew_ShouldAddToArrayAndRaiseEvent()
    {
        // Arrange
        var product = Product.Create(
            fixture.DefaultSku,
            fixture.DefaultName, 
            fixture.DefaultDescription,
            fixture.DefaultCategoryId,
            fixture.DefaultPrice,
            []);
        var image = new ProductImage("https://example.com/new.png");
        product.ClearDomainEvents();

        // Act
        product.AddImage(image);

        // Assert
        product.Images.Should().ContainSingle()
            .Which.Url.Should().Be("https://example.com/new.png");
        
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductImagesUpdatedEvent>();
    }
    
    [Fact]
    public void AddImage_WhenImageAlreadyExists_ShouldThrowArgumentException()
    {
        // Arrange
        var product = Product.Create(
            fixture.DefaultSku,
            fixture.DefaultName, 
            fixture.DefaultDescription,
            fixture.DefaultCategoryId,
            fixture.DefaultPrice,
            []);
        var image = new ProductImage("https://example.com/duplicate.png");
        product.AddImage(image);

        // Act
        var act = () => product.AddImage(new ProductImage("https://example.com/duplicate.png"));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("image");
    }
    
    [Fact]
    public void RemoveImage_WhenImageExists_ShouldRemoveAndRaiseEvent()
    {
        // Arrange
        var product = Product.Create(
            fixture.DefaultSku,
            fixture.DefaultName, 
            fixture.DefaultDescription,
            fixture.DefaultCategoryId,
            fixture.DefaultPrice,
            []);
        var imageUrl = "https://example.com/to-remove.png";
        product.AddImage(new ProductImage(imageUrl));
        product.ClearDomainEvents();

        // Act
        product.RemoveImage(imageUrl);

        // Assert
        product.Images.Should().BeEmpty();
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductImagesUpdatedEvent>();
    }
    
    [Fact]
    public void RemoveImage_WhenImageDoesNotExist_ShouldThrowArgumentException()
    {
        // Arrange
        var product = Product.Create(
            fixture.DefaultSku,
            fixture.DefaultName, 
            fixture.DefaultDescription,
            fixture.DefaultCategoryId,
            fixture.DefaultPrice,
            []);

        // Act
        var act = () => product.RemoveImage("https://example.com/non-existent.png");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("url");
    }
}