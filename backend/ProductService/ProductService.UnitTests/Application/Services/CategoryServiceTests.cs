using System.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ProductService.Application.DTO.Category;
using ProductService.Application.Exceptions;
using ProductService.Application.Services;
using ProductService.Application.Services.Categories;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.Caching.Abstractions;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;
using Xunit;

namespace ProductService.UnitTests.Application.Services;

public class CategoryServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>();
    private readonly ICategoryVersionProvider _versionProvider = Substitute.For<ICategoryVersionProvider>();
    private readonly ILogger<CategoryService> _logger = Substitute.For<ILogger<CategoryService>>();
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _service = new CategoryService(_uow, _repository, _versionProvider, _logger);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenEtagMatches_ShouldReturnNotModifiedAndAvoidDatabaseCall()
    {
        // Arrange
        var ct = CancellationToken.None;
        const string matchingEtag = "etag-12345";

        // Настраиваем получение актуального E-Tag из Redis
        _versionProvider.GetVersionETagAsync(ct).Returns(matchingEtag);

        // Act
        // Передаем клиентом точно такой же E-Tag, какой лежит в кэше
        var result = await _service.GetAllAsync(matchingEtag, ct);

        // Assert
        result.Should().NotBeNull();
        result.Categories.Should().BeEmpty();
        result.ETag.Should().Be(matchingEtag);
        result.IsModified.Should().BeFalse();

        // Важнейшая проверка для High-Load: репозиторий НЕ должен вызываться за данными!
        await _repository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_WhenEtagMismatches_ShouldFetchFromRepositoryAndReturnModified()
    {
        // Arrange
        var ct = CancellationToken.None;
        const string actualEtag = "new-etag-999";
        const string clientOldEtag = "old-etag-111";

        var categories = new List<Category>
        {
            Category.Reconstruct(1, "Electronics", "electronics"),
            Category.Reconstruct(2, "Books", "books")
        };

        _versionProvider.GetVersionETagAsync(ct).Returns(actualEtag);
        _repository.GetAllAsync(ct).Returns(categories);

        // Act
        // Передаем устаревший E-Tag клиента
        var result = await _service.GetAllAsync(clientOldEtag, ct);

        // Assert
        result.Should().NotBeNull();
        result.IsModified.Should().BeTrue();
        result.ETag.Should().Be(actualEtag);
        
        result.Categories.Should().HaveCount(2);
        result.Categories.First().Id.Should().Be(1);
        result.Categories.First().Name.Should().Be("Electronics");
        result.Categories.Last().Id.Should().Be(2);

        // Проверяем, что к репозиторию был совершен вызов за данными
        await _repository.Received(1).GetAllAsync(ct);
    }

    [Fact]
    public async Task GetAllAsync_WhenClientHasNoEtag_ShouldFetchFromRepositoryAndReturnModified()
    {
        // Arrange
        var ct = CancellationToken.None;
        const string actualEtag = "some-current-etag";
        
        _versionProvider.GetVersionETagAsync(ct).Returns(actualEtag);
        _repository.GetAllAsync(ct).Returns(new List<Category>());

        // Act
        // Клиент делает первый запрос (If-None-Match отсутствует)
        var result = await _service.GetAllAsync(null, ct);

        // Assert
        result.Should().NotBeNull();
        result.Categories.Should().BeEmpty();
        result.ETag.Should().Be(actualEtag);
        result.IsModified.Should().BeTrue();

        await _repository.Received(1).GetAllAsync(ct);
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
        await _uow.Received(1).CommitAsync();
        await _uow.DidNotReceive().RollbackAsync();
        
        await _repository.Received(1).AddAsync(Arg.Is<Category>(c => c.Name == "Automotive"));
    }

    [Fact]
    public async Task CreateAsync_WhenDomainValidationFails_ShouldThrowExceptionBeforeTransaction()
    {
        // Arrange
        var ct = CancellationToken.None;
        var invalidInput = new CreateCategoryDto("", "invalid.path");

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
        await _uow.Received(1).RollbackAsync();
        await _uow.DidNotReceive().CommitAsync();
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

        _repository.GetAsync(1).Returns(existingCategory);

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

        _repository.GetAsync(1).Returns(existingCategory);
        _repository.UpdateAsync(Arg.Any<Category>()).Returns(true);

        // Act
        var result = await _service.UpdateAsync(input, ct);

        // Assert
        result.Name.Should().Be("New Name");

        await _uow.Received(1).BeginTransactionAsync(ct);
        await _repository.Received(1).UpdateAsync(Arg.Is<Category>(c => c.Name == "New Name"));
        await _uow.Received(1).CommitAsync();
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var ct = CancellationToken.None;
        var input = new UpdateCategoryDto(999, "Name", "path");

        _repository.GetAsync(999).Returns((Category?)null);

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

        _repository.GetAsync(1).Returns(existingCategory);
        
        _repository.UpdateAsync(Arg.Any<Category>()).Returns(false);

        // Act
        var act = async () => await _service.UpdateAsync(input, ct);

        // Assert
        await act.Should().ThrowAsync<DBConcurrencyException>();
        await _uow.Received(1).RollbackAsync();
        await _uow.DidNotReceive().CommitAsync();
    }

    #endregion
    
    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenCategoryExists_ShouldDeleteAndCommit()
    {
        // Arrange
        var ct = CancellationToken.None;
        var existingCategory = Category.Reconstruct(10, "To Delete", "path");

        _repository.GetAsync(10).Returns(existingCategory);

        // Act
        await _service.DeleteAsync(10, ct);

        // Assert
        await _uow.Received(1).BeginTransactionAsync(ct);
        await _repository.Received(1).DeleteAsync(10);
        await _uow.Received(1).CommitAsync();
        await _uow.DidNotReceive().RollbackAsync();
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var ct = CancellationToken.None;
        _repository.GetAsync(999).Returns((Category?)null);

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

        _repository.GetAsync(10).Returns(existingCategory);
        _repository.DeleteAsync(10).ThrowsAsync(new Exception("Database Error"));

        // Act
        var act = async () => await _service.DeleteAsync(10, ct);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Database Error");
        await _uow.Received(1).RollbackAsync();
        await _uow.DidNotReceive().CommitAsync();
    }

    #endregion
}