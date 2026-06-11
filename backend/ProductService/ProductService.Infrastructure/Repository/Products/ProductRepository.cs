using System.Data;
using Dapper;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using ProductService.Infrastructure.DAO;
using ProductService.Infrastructure.Mappers;
using ProductService.Infrastructure.UnitOfWork;

namespace ProductService.Infrastructure.Repository.Products;

public class ProductRepository(IDbSession session) : IProductRepository
{
    public async Task<Product?> GetAsync(Guid id)
    {
        var connection = session.Connection;
        var transaction = session.Transaction;
        
        const string sql = """
                           SELECT id, sku, seller_id, name, description, 
                           price_amount, price_currency, category_id,
                           images, created_at, updated_at, version
                           FROM products 
                           WHERE id = @Id;
                           """;
        
        var productDao = await connection.QueryFirstOrDefaultAsync<ProductDao>(sql, new { Id = id }, transaction);
        
        return productDao?.ToDomain();
    }

    public async Task AddAsync(Product product)
    {
        var connection = session.Connection;
        var transaction = session.Transaction;

        var dao = product.ToDao();
        
        const string sql = """
                           INSERT INTO products (id, sku, seller_id, name, description, price_amount, price_currency, category_id, created_at, updated_at, version, images)
                           VALUES (@Id, @Sku, @SellerId, @Name, @Description, @PriceAmount, @PriceCurrency, @CategoryId, @CreatedAt, @UpdatedAt, @Version, @Images::jsonb);
                           """;
        
        await connection.ExecuteAsync(sql, dao, transaction);
    }

    public async Task UpdateAsync(Product product)
    {
        var connection = session.Connection;
        var transaction = session.Transaction;
        
        var dao = product.ToDao();

        const string sql = """
                           UPDATE products 
                           SET name = @Name, 
                               description = @Description, 
                               price_amount = @PriceAmount, 
                               price_currency = @PriceCurrency,
                               category_id = @CategoryId, 
                               updated_at = @UpdatedAt, 
                               version = @Version,
                               images = @Images::jsonb
                           WHERE id = @Id AND version = (@Version - 1);;
                           """;
        
        var affectedRows = await connection.ExecuteAsync(sql, dao, transaction);

        if (affectedRows == 0)
        {
            throw new DBConcurrencyException($"Concurrency conflict. Product with ID {product.Id} has been modified by another process.");
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var connection = session.Connection;
        var transaction = session.Transaction;

        const string sql = "DELETE FROM products WHERE id = @Id;";

        await connection.ExecuteAsync(sql, new { Id = id }, transaction);
    }
}