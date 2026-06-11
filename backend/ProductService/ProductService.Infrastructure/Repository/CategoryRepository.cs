using Dapper;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions;
using ProductService.Infrastructure.DAO;
using ProductService.Infrastructure.Mappers;
using ProductService.Infrastructure.UnitOfWork;

namespace ProductService.Infrastructure.Repository;

public class CategoryRepository(IDbSession session) : ICategoryRepository
{
    public async Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken ct)
    {
        var connection = session.Connection;
        var transaction = session.Transaction;
        
        const string sql = "SELECT id, name, path FROM categories;";
        
        var daos = await connection.QueryAsync<CategoryDao>(sql, transaction: transaction);
        
        return daos.Select(dao => dao.ToDomain()).ToList();
    }

    public async Task<Category?> GetAsync(int id)
    {
        var connection = session.Connection;
        var transaction = session.Transaction;
        
        const string sql = """
                           SELECT id, name, path 
                           FROM categories 
                           WHERE id = @Id;
                           """;
        
        var dao = await connection.QueryFirstOrDefaultAsync<CategoryDao>(sql, new { Id = id }, transaction: transaction);

        return dao?.ToDomain();
    }

    public async Task<int> AddAsync(Category category)
    {
        var connection = session.Connection;
        var transaction = session.Transaction;
        
        var dao = category.ToDao();

        const string sql = """
                           INSERT INTO categories (name, path) 
                           VALUES (@Name, @Path) 
                           RETURNING id;
                           """;
        
        var id = await connection.ExecuteScalarAsync<int>(sql, dao, transaction: transaction);

        return id;
    }

    public async Task<bool> UpdateAsync(Category category)
    {
        var connection = session.Connection;
        var transaction = session.Transaction;
        
        var dao = category.ToDao();
        
        const string sql = """
                           UPDATE categories 
                           SET name = @Name, 
                               path = @Path 
                           WHERE id = @Id;
                           """;

        var updatedRows = await connection.ExecuteAsync(sql, dao, transaction: transaction);
        
        return updatedRows > 0;
    }

    public async Task DeleteAsync(int id)
    {
        var connection = session.Connection;
        var transaction = session.Transaction;

        const string sql = "DELETE FROM categories WHERE id = @Id;";

        await connection.ExecuteAsync(sql, new { Id = id }, transaction: transaction);
    }
}