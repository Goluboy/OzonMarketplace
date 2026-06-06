using System.Data;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ProductService.Application.DTO.Category;
using ProductService.Application.Exceptions;
using ProductService.Application.Services;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;
using Xunit;

namespace ProductService.UnitTests.Application.Services;

public class CategoryServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>();
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _service = new CategoryService(_uow, _repository);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenCategoriesExist_ShouldReturnMappedDtos()
    {
        // Arrange
        var ct = CancellationToken.None;
        var categories = new List<Category>
        {
            Category.Reconstruct(1, "Electronics", "electronics"),
            Category.Reconstruct(2, "Books", "books")
        };

        _repository.GetAllAsync().Returns(categories);

        // Act
        var result = await _service.GetAllAsync(ct);

        // Assert
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(1);
        result.First().Name.Should().Be("Electronics");
        result.Last().Id.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoCategoriesExist_ShouldReturnEmptyCollection()
    {
        // Arrange
        var ct = CancellationToken.None;
        _repository.GetAllAsync().Returns(new List<Category>());

        // Act
        var result = await _service.GetAllAsync(ct);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion


    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidInput_ShouldSaveCategoryAndCommit()
    {
        // Arrange
        var ct = CancellationToken.None;
        var input = new CreateCategoryDto("Automotive", "automotive");
        const int generatedId = 42;
        
        _repository.AddAsync(Arg.Any<Category>()).Returns(generatedId);

        // Act
        var result = await _service.CreateAsync(input, ct);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(generatedId);
        result.Name.Should().Be("Automotive");
        
        await _uow.Received(1).BeginTransactionAsync(ct);
        await _uow.Received(1).CommitAsync(ct);
        await _uow.DidNotReceive().RollbackAsync(ct);
        
        await _repository.Received(1).AddAsync(Arg.Is<Category>(c => c.Name == "Automotive"));
    }

    [Fact]
    public async Task CreateAsync_WhenDomainValidationFails_ShouldThrowExceptionBeforeTransaction()
    {
        // Arrange
        var ct = CancellationToken.None;
        var invalidInput = new CreateCategoryDto("", "invalid.path"); // Пустое имя

        // Act
        var act = async () => await _service.CreateAsync(invalidInput, ct);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();

        await _uow.DidNotReceive().BeginTransactionAsync(ct);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Category>());
    }

    [Fact]
    public async Task CreateAsync_WhenDatabaseThrowsException_ShouldRollbackAndThrow()
    {
        // Arrange
        var ct = CancellationToken.None;
        var input = new CreateCategoryDto("Books", "books");

        _repository.AddAsync(Arg.Any<Category>()).ThrowsAsync(new Exception("Database crash"));

        // Act
        var act = async () => await _service.CreateAsync(input, ct);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Database crash");
        
        await _uow.Received(1).BeginTransactionAsync(ct);
        await _uow.Received(1).RollbackAsync(ct);
        await _uow.DidNotReceive().CommitAsync(ct);
    }

    #endregion


    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenNoChangesMade_ShouldReturnImmediatelyWithoutDatabaseCall()
    {
        // Arrange
        var ct = CancellationToken.None;
        var existingCategory = Category.Reconstruct(1, "Electronics", "electronics");
        
        var input = new UpdateCategoryDto(1, "Electronics", "electronics");

        _repository.GetByIdAsync(1).Returns(existingCategory);

        // Act
        var result = await _service.UpdateAsync(input, ct);

        // Assert
        result.Id.Should().Be(1);
        result.Name.Should().Be("Electronics");
        
        await _uow.DidNotReceive().BeginTransactionAsync(ct);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Category>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNameChanges_ShouldUpdateAndCommit()
    {
        // Arrange
        var ct = CancellationToken.None;
        var existingCategory = Category.Reconstruct(1, "Old Name", "path");
        var input = new UpdateCategoryDto(1, "New Name", "path");

        _repository.GetByIdAsync(1).Returns(existingCategory);
        _repository.UpdateAsync(Arg.Any<Category>()).Returns(true);

        // Act
        var result = await _service.UpdateAsync(input, ct);

        // Assert
        result.Name.Should().Be("New Name");

        await _uow.Received(1).BeginTransactionAsync(ct);
        await _repository.Received(1).UpdateAsync(Arg.Is<Category>(c => c.Name == "New Name"));
        await _uow.Received(1).CommitAsync(ct);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var ct = CancellationToken.None;
        var input = new UpdateCategoryDto(999, "Name", "path");

        _repository.GetByIdAsync(999).Returns((Category?)null);

        // Act
        var act = async () => await _service.UpdateAsync(input, ct);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().BeginTransactionAsync(ct);
    }

    [Fact]
    public async Task UpdateAsync_WhenConcurrencyConflictExists_ShouldRollbackAndThrowException()
    {
        // Arrange
        var ct = CancellationToken.None;
        var existingCategory = Category.Reconstruct(1, "Old Name", "path");
        var input = new UpdateCategoryDto(1, "New Name", "path");

        _repository.GetByIdAsync(1).Returns(existingCategory);
        
        _repository.UpdateAsync(Arg.Any<Category>()).Returns(false);

        // Act
        var act = async () => await _service.UpdateAsync(input, ct);

        // Assert
        await act.Should().ThrowAsync<DBConcurrencyException>();
        await _uow.Received(1).RollbackAsync(ct);
        await _uow.DidNotReceive().CommitAsync(ct);
    }

    #endregion
    
    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenCategoryExists_ShouldDeleteAndCommit()
    {
        // Arrange
        var ct = CancellationToken.None;
        var existingCategory = Category.Reconstruct(10, "To Delete", "path");

        _repository.GetByIdAsync(10).Returns(existingCategory);

        // Act
        await _service.DeleteAsync(10, ct);

        // Assert
        await _uow.Received(1).BeginTransactionAsync(ct);
        await _repository.Received(1).DeleteAsync(10);
        await _uow.Received(1).CommitAsync(ct);
        await _uow.DidNotReceive().RollbackAsync(ct);
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var ct = CancellationToken.None;
        _repository.GetByIdAsync(999).Returns((Category?)null);

        // Act
        var act = async () => await _service.DeleteAsync(999, ct);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        
        await _uow.DidNotReceive().BeginTransactionAsync(ct);
        await _repository.DidNotReceive().DeleteAsync(999);
    }

    [Fact]
    public async Task DeleteAsync_WhenDatabaseExceptionOccurs_ShouldRollbackAndThrow()
    {
        // Arrange
        var ct = CancellationToken.None;
        var existingCategory = Category.Reconstruct(10, "To Delete", "path");

        _repository.GetByIdAsync(10).Returns(existingCategory);
        _repository.DeleteAsync(10).ThrowsAsync(new Exception("Database Error"));

        // Act
        var act = async () => await _service.DeleteAsync(10, ct);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Database Error");
        await _uow.Received(1).RollbackAsync(ct);
        await _uow.DidNotReceive().CommitAsync(ct);
    }

    #endregion
}