using FluentAssertions;
using ProductService.Domain.Entities;
using ProductService.Domain.Events;
using Xunit;

namespace ProductService.UnitTests.Domain;

public class CategoryTests
{
    [Theory]
    [InlineData("electronics")]
    [InlineData("electronics.phones")]
    [InlineData("electronics.phones.smartphones")]
    [InlineData("home-appliances.cooking-stoves")]  
    public void Create_WithValidPathFormat_ShouldInitializeSuccessfully(string validPath)
    {
        // Act
        var category = Category.Create("Valid Name", validPath);

        // Assert
        category.Should().NotBeNull();
        category.Path.Should().Be(validPath);
        
        category.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CategoryCreatedEvent>();;
    }

    [Fact]
    public void SetId_WithValidCategory_ShouldSetIdInDomainEvent()
    {
        // Assert
        var category = Category.Create("Valid Name", "valid.parent.path");
        var expectedId = 101;
        
        // Act
        category.SetId(expectedId);
        
        // Assert
        var domainEvent = category.DomainEvents
            .Should().ContainSingle()
            .Subject.Should().BeOfType<CategoryCreatedEvent>()
            .Subject;
        domainEvent.Category.Id.Should().Be(expectedId);
    }
    
    [Theory]
    [InlineData(".electronics")] 
    [InlineData("electronics.")]
    [InlineData("electronics..phones")]
    [InlineData("electronics. .phones")]
    [InlineData("electronics.phones.smart phones")]
    [InlineData("electronics.ph@nes")]
    public void Create_WithInvalidPathFormat_ShouldThrowArgumentException(string invalidPath)
    {
        // Act
        Action act = () => Category.Create("Valid Name", invalidPath);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("path");
    }
    
    [Theory]
    [InlineData(".newpath")]
    [InlineData("newpath.")]
    [InlineData("new..path")]
    [InlineData("new.pa@th")]
    public void MoveTo_WithInvalidPathFormat_ShouldThrowArgumentException(string invalidPath)
    {
        // Arrange
        var category = Category.Create("Valid Name", "valid.parent.path");

        // Act
        var act = () => category.MoveTo(invalidPath);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("path");
        
        category.Path.Should().Be("valid.parent.path");
    }

    [Fact]
    public void MoveTo_WithValidPathFormat_ShouldSuccessfully()
    {
        // Assert
        var category = Category.Create("Valid Name", "valid.parent.path");
        category.ClearDomainEvents();
        var newPath = "new.valid.path";
        
        // Act
        category.MoveTo(newPath);
        
        // Assert
        category.Should().NotBeNull();
        category.Path.Should().Be(newPath);
        
        category.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CategoryPathChangedEvent>();
    }
}