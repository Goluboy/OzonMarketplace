using System.Data.Common;
using Dapper;
using FluentAssertions;
using Npgsql;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.DAO;

namespace ProductService.IntegrationTests.Infrastructure.Repository;

public class CategoryRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    
    public async Task DisposeAsync()
    {
        await fixture.ResetAsync("categories");
    }
    
    private async Task<int> InsertCategoryDirectlyAsync(string name, string path)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        
        const string sql = "INSERT INTO categories (name, path) VALUES (@Name, @Path) RETURNING id;";
        return await connection.ExecuteScalarAsync<int>(sql, new { Name = name, Path = path });
    }
    
    [Fact]
    public async Task GetAllAsync_WhenDatabaseIsEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        var (uow, session) = fixture.CreateUnitOfWorkContext();
        var repository = fixture.CreateCategoryRepository(session);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }
    
    [Fact]
    public async Task GetAllAsync_WhenCategoriesExist_ShouldReturnAllCategories()
    {
        // Arrange
        var id1 = await InsertCategoryDirectlyAsync("Electronics", "electronics");
        var id2 = await InsertCategoryDirectlyAsync("Books", "books");

        var (uow, session) = fixture.CreateUnitOfWorkContext();
        var repository = fixture.CreateCategoryRepository(session);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Id == id1 && c.Name == "Electronics" && c.Path == "electronics");
        result.Should().Contain(c => c.Id == id2 && c.Name == "Books" && c.Path == "books");
    }
    
    [Fact]
    public async Task GetByIdAsync_WhenCategoryExists_ShouldReturnCorrectCategory()
    {
        // Arrange
        var id = await InsertCategoryDirectlyAsync("Home Decor", "home.decor");

        var (uow, session) = fixture.CreateUnitOfWorkContext();
        var repository = fixture.CreateCategoryRepository(session);

        // Act
        var result = await repository.GetAsync(id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Name.Should().Be("Home Decor");
        result.Path.Should().Be("home.decor");
    }
    
    [Fact]
    public async Task GetByIdAsync_WhenCategoryDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var (uow, session) = fixture.CreateUnitOfWorkContext();
        var repository = fixture.CreateCategoryRepository(session);
        const int nonExistentId = 9999;

        // Act
        var result = await repository.GetAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithValidData_ShouldInsertSuccessfullyAndReturnId()
    {
        // Arrange
        var (uow, session) = fixture.CreateUnitOfWorkContext();
        var repository = fixture.CreateCategoryRepository(session);
        var category = Category.Create("Automotive", "automotive");

        // Act
        await uow.BeginTransactionAsync();
        var id = await repository.AddAsync(category);
        await uow.CommitAsync();

        // Assert
        id.Should().BeGreaterThan(0);
        
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        var dbRow = await connection.QueryFirstOrDefaultAsync<CategoryDao>(
            "SELECT id, name, path FROM categories WHERE id = @Id;", new { Id = id });

        dbRow.Should().NotBeNull();
        dbRow!.Name.Should().Be("Automotive");
        dbRow.Path.Should().Be("automotive");
    }
    
    [Fact]
    public async Task AddAsync_WithDatabaseConstraintViolation_ShouldThrowDbException()
    {
        // Arrange
        var (uow, session) = fixture.CreateUnitOfWorkContext();
        var repository = fixture.CreateCategoryRepository(session);
        var category = Category.Create("Valid Name", "valid.path");
        
        typeof(Category)
            .GetProperty(nameof(Category.Name))?
            .SetValue(category, null);

        await uow.BeginTransactionAsync();

        // Act
        Func<Task> act = async () => await repository.AddAsync(category);

        // Assert
        await act.Should().ThrowAsync<DbException>();
        await uow.RollbackAsync();
    }
    
    [Fact]
    public async Task UpdateAsync_WhenCategoryExists_ShouldUpdateAndReturnTrue()
    {
        // Arrange
        var id = await InsertCategoryDirectlyAsync("Initial Name", "initial.path");

        var (uow, session) = fixture.CreateUnitOfWorkContext();
        var repository = fixture.CreateCategoryRepository(session);

        var category = Category.Create("Updated Name", "updated.path");
        category.SetId(id);

        // Act
        await uow.BeginTransactionAsync();
        var isUpdated = await repository.UpdateAsync(category);
        await uow.CommitAsync();

        // Assert
        isUpdated.Should().BeTrue();

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        var dbRow = await connection.QueryFirstOrDefaultAsync<CategoryDao>(
            "SELECT name, path FROM categories WHERE id = @Id;", new { Id = id });

        dbRow.Should().NotBeNull();
        dbRow!.Name.Should().Be("Updated Name");
        dbRow.Path.Should().Be("updated.path");
    }
    
    [Fact]
    public async Task UpdateAsync_WhenCategoryDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var (uow, session) = fixture.CreateUnitOfWorkContext();
        var repository = fixture.CreateCategoryRepository(session);
        
        var nonExistentCategory = Category.Create("Ghost", "ghost");
        nonExistentCategory.SetId(9999);

        // Act
        await uow.BeginTransactionAsync();
        var isUpdated = await repository.UpdateAsync(nonExistentCategory);
        await uow.CommitAsync();

        // Assert
        isUpdated.Should().BeFalse();
    }
    
    [Fact]
    public async Task DeleteAsync_WhenCategoryExists_ShouldRemoveRecord()
    {
        // Arrange
        var id = await InsertCategoryDirectlyAsync("To Be Deleted", "delete.me");

        var (uow, session) = fixture.CreateUnitOfWorkContext();
        var repository = fixture.CreateCategoryRepository(session);

        // Act
        await uow.BeginTransactionAsync();
        await repository.DeleteAsync(id);
        await uow.CommitAsync();

        // Assert
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        var exists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM categories WHERE id = @Id);", new { Id = id });

        exists.Should().BeFalse();
    }
    
    [Fact]
    public async Task DeleteAsync_WhenCategoryDoesNotExist_ShouldNotThrowException()
    {
        // Arrange
        var (uow, session) = fixture.CreateUnitOfWorkContext();
        var repository = fixture.CreateCategoryRepository(session);
        const int nonExistentId = 9999;

        // Act
        await uow.BeginTransactionAsync();
        var act = async () => await repository.DeleteAsync(nonExistentId);

        // Assert
        await act.Should().NotThrowAsync();
        await uow.CommitAsync();
    }
}