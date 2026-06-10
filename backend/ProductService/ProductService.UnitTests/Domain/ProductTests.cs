using FluentAssertions;
using ProductService.Domain.Entities;
using ProductService.Domain.Events;
using ProductService.Domain.ValueObjects;
using Xunit;

namespace ProductService.UnitTests.Domain;

public class ProductTests(ProductFixture fixture) : IClassFixture<ProductFixture>
{
    private static Product CreateProductWithImages(List<string> imageUrls)
    {
        var images = imageUrls.Select(url => new ProductImage(url)).ToList();
        
        return Product.Reconstruct(
            id: Guid.NewGuid(),
            sellerId: Guid.NewGuid(),
            sku: 123456,
            name: "Test Product",
            description: "Description",
            price: new Money(100m, "RUB"),
            categoryId: 1,
            createdAt: DateTimeOffset.UtcNow.AddHours(-1),
            updatedAt: DateTimeOffset.UtcNow.AddHours(-1),
            version: 1,
            images: images
        );
    }
    
    [Fact]
    public void Create_WithValidParameters_ShouldCreateProduct()
    {
        // Arrange
        var sellerId = fixture.SellerId;
        // Act
        var product = Product.Create(
            fixture.DefaultSku,
            fixture.DefaultName, 
            fixture.DefaultDescription,
            fixture.DefaultCategoryId,
            sellerId,
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
        product.SellerId.Should().Be(sellerId);
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
            fixture.SellerId,
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
            fixture.SellerId,
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
            fixture.SellerId,
            fixture.DefaultPrice,
            fixture.DefaultImages);

        var newName = "Updated Product Name";
        var newDescription = "Updated Description";
        var newCategoryId = 101;
        
        var initialVersion = product.Version;
        product.ClearDomainEvents();
        
        // Act
        product.UpdateDetails(newName, newDescription, newCategoryId);
        product.IncrementVersion();
        
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
            fixture.SellerId,
            fixture.DefaultPrice,
            fixture.DefaultImages);
        
        var validDescription = "Some Description";
        var validCategoryId = 101;

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
            fixture.SellerId,
            fixture.DefaultPrice,
            fixture.DefaultImages);
        var validName = "Valid Name";
        var validDescription = "Valid Description";
        var emptyCategoryId = 0;

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
            fixture.SellerId,
            fixture.DefaultPrice,
            fixture.DefaultImages);
        
        var newPrice = new Money(999);
        var initialVersion = product.Version;
        product.ClearDomainEvents();
        
        // Act
        product.ChangePrice(newPrice);
        product.IncrementVersion();
        
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
            fixture.SellerId,
            fixture.DefaultPrice,
            fixture.DefaultImages);

        // Act
        var act = () => product.ChangePrice(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("newPrice");
    }
    
    [Fact]
    public void UpdateImages_WhenNoInitialImages_ShouldAddNewImagesAndRaiseEvent()
    {
        // Arrange
        var product = CreateProductWithImages([]);
        var newUrls = new List<string> { "http://img1.png", "http://img2.png" };

        // Act
        product.UpdateImages(newUrls);
        product.IncrementVersion();

        // Assert
        product.Images.Should().HaveCount(2);
        product.Images.Select(i => i.Url).Should().ContainInOrder(newUrls);
        product.Version.Should().Be(2);
        
        var ev = product.DomainEvents
            .Should().ContainSingle()
            .Which.Should().BeOfType<ProductImagesUpdatedEvent>()
            .Subject;

        ev.ProductId.Should().Be(product.Id);
        ev.ImageUrls.Should().ContainInOrder(newUrls);
        ev.RemovedUrls.Should().BeEmpty();
    }

    [Fact]
    public void UpdateImages_WhenHasInitialImages_ShouldDeleteAllAndRaiseEvent()
    {
        // Arrange
        var initialUrls = new List<string> { "http://img1.png", "http://img2.png" };
        var product = CreateProductWithImages(initialUrls);
        var emptyUrls = new List<string>();

        // Act
        product.UpdateImages(emptyUrls);
        product.IncrementVersion();

        // Assert
        product.Images.Should().BeEmpty();
        product.Version.Should().Be(2);

        var ev = product.DomainEvents
            .Should().ContainSingle()
            .Which.Should().BeOfType<ProductImagesUpdatedEvent>()
            .Subject;

        ev.ImageUrls.Should().BeEmpty();
        ev.RemovedUrls.Should().HaveCount(2).And.ContainInOrder(initialUrls);
    }

    [Fact]
    public void UpdateImages_WhenHasInitialImages_ShouldDeletePartAndRaiseEvent()
    {
        // Arrange
        var initialUrls = new List<string> { "http://img1.png", "http://img2.png", "http://img3.png" };
        var product = CreateProductWithImages(initialUrls);
        var remainingUrls = new List<string> { "http://img1.png" };

        // Act
        product.UpdateImages(remainingUrls);
        product.IncrementVersion();

        // Assert
        product.Images.Should().ContainSingle();
        product.Images.First().Url.Should().Be("http://img1.png");
        product.Version.Should().Be(2);

        var ev = product.DomainEvents
            .Should().ContainSingle()
            .Which.Should().BeOfType<ProductImagesUpdatedEvent>()
            .Subject;

        ev.ImageUrls.Should().ContainSingle().Which.Should().Be("http://img1.png");
        ev.RemovedUrls.Should().HaveCount(2).And.Contain("http://img2.png", "http://img3.png");
    }

    [Fact]
    public void UpdateImages_WhenHasInitialImages_ShouldDeletePartAndAddPart()
    {
        // Arrange
        var initialUrls = new List<string> { "http://img1.png", "http://img2.png" };
        var product = CreateProductWithImages(initialUrls);
        var updatedUrls = new List<string> { "http://img1.png", "http://img3.png" };

        // Act
        product.UpdateImages(updatedUrls);
        product.IncrementVersion();

        // Assert
        product.Images.Should().HaveCount(2);
        product.Images.Select(i => i.Url).Should().ContainInOrder(updatedUrls);
        product.Version.Should().Be(2);

        var ev = product.DomainEvents
            .Should().ContainSingle()
            .Which.Should().BeOfType<ProductImagesUpdatedEvent>()
            .Subject;

        ev.ImageUrls.Should().ContainInOrder(updatedUrls);
        ev.RemovedUrls.Should().ContainSingle().Which.Should().Be("http://img2.png");
    }

    [Fact]
    public void UpdateImages_WhenHasInitialImages_ShouldDeleteAllAndAddCompletelyNew()
    {
        // Arrange
        var initialUrls = new List<string> { "http://img1.png", "http://img2.png" };
        var product = CreateProductWithImages(initialUrls);
        var brandNewUrls = new List<string> { "http://img3.png", "http://img4.png" };

        // Act
        product.UpdateImages(brandNewUrls);
        product.IncrementVersion();

        // Assert
        product.Images.Should().HaveCount(2);
        product.Images.Select(i => i.Url).Should().ContainInOrder(brandNewUrls);
        product.Version.Should().Be(2);

        var ev = product.DomainEvents
            .Should().ContainSingle()
            .Which.Should().BeOfType<ProductImagesUpdatedEvent>()
            .Subject;

        ev.ImageUrls.Should().ContainInOrder(brandNewUrls);
        ev.RemovedUrls.Should().HaveCount(2).And.ContainInOrder(initialUrls);
    }

    [Fact]
    public void UpdateImages_WhenCollectionIsEmptyAndNoChangesMade_ShouldNotModifyStateOrRaiseEvents()
    {
        // Arrange
        var product = CreateProductWithImages([]);

        // Act
        product.UpdateImages([]);

        // Assert
        product.Images.Should().BeEmpty();
        product.Version.Should().Be(1);
        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateImages_WhenImagesOnlyChangeCase_ShouldTreatAsNoChangeAndNotModifyState()
    {
        // Arrange
        var initialUrls = new List<string> { "http://img1.png", "http://IMG2.PNG" };
        var product = CreateProductWithImages(initialUrls);
        
        var sameUrlsWithDifferentCase = new List<string> { "http://IMG1.PNG", "http://img2.png" };

        // Act
        product.UpdateImages(sameUrlsWithDifferentCase);

        // Assert 
        product.Images.Should().HaveCount(2);
        product.Version.Should().Be(1);
        product.DomainEvents.Should().BeEmpty();
    }
}