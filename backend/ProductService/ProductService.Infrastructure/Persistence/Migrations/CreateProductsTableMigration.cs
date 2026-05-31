using FluentMigrator;
using ProductService.Infrastructure.DAO;
using ProductService.Infrastructure.Helpers;

namespace ProductService.Infrastructure.Persistence.Migrations;

[Migration(202605301710000)]
public class CreateProductsTableMigration : Migration
{
    private const string TableName = "products";
    
    public override void Up()
    {
        var id = nameof(ProductDao.Id).ToSnakeCase();
        var sellerId = nameof(ProductDao.SellerId).ToSnakeCase();
        var sku = nameof(ProductDao.Sku).ToSnakeCase();
        var name = nameof(ProductDao.Name).ToSnakeCase();
        var description = nameof(ProductDao.Description).ToSnakeCase();
        var priceAmount = nameof(ProductDao.PriceAmount).ToSnakeCase();
        var categoryId = nameof(ProductDao.CategoryId).ToSnakeCase();
        var createdAt = nameof(ProductDao.CreatedAt).ToSnakeCase();
        var updatedAt = nameof(ProductDao.UpdatedAt).ToSnakeCase();
        var version = nameof(ProductDao.Version).ToSnakeCase();
        var images = nameof(ProductDao.Images).ToSnakeCase();
        
        Create.Table(TableName)
            .WithColumn(id).AsGuid().PrimaryKey()
            .WithColumn(sellerId).AsGuid().NotNullable()
            .WithColumn(sku).AsInt64().NotNullable()
            .WithColumn(name).AsString(255).NotNullable()
            .WithColumn(description).AsString(2000).NotNullable()
            .WithColumn(priceAmount).AsDecimal(18, 2).NotNullable()
            .WithColumn(categoryId).AsInt32().NotNullable()
            .ForeignKey("fk_products_categories", "categories", "id")
            .WithColumn(createdAt).AsDateTimeOffset().NotNullable()
            .WithColumn(updatedAt).AsDateTimeOffset().NotNullable()
            .WithColumn(version).AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn(images).AsCustom("jsonb").NotNullable().WithDefaultValue("[]");
            
        Create.UniqueConstraint("uq_products_seller_sku")
            .OnTable(TableName)
            .Columns(sellerId, sku);
        
        Create.Index("ix_products_category_created_at")
            .OnTable(TableName)
            .OnColumn(categoryId).Ascending()
            .OnColumn(createdAt).Descending();
        
        Create.Index("ix_products_category_price")
            .OnTable(TableName)
            .OnColumn(categoryId).Ascending()
            .OnColumn(priceAmount).Ascending();
        
        Create.Index("ix_products_category_name")
            .OnTable(TableName)
            .OnColumn(categoryId).Ascending()
            .OnColumn(name).Ascending();
        
        Alter.Table(TableName)
            .AddColumn("search_vector").AsCustom(
                "tsvector GENERATED ALWAYS AS (to_tsvector('russian', coalesce(name, '') || ' ' || coalesce(description, ''))) STORED"
            );
        
        Execute.Sql("CREATE INDEX ix_products_search_vector ON products USING gin(search_vector);");
    }

    public override void Down()
    {
        Delete.Index("ix_products_search_vector").OnTable(TableName);
        Delete.Index("ix_products_category_name").OnTable(TableName);
        Delete.Index("ix_products_category_price").OnTable(TableName);
        Delete.Index("ix_products_category_created_at").OnTable(TableName);
        
        Delete.UniqueConstraint("uq_products_seller_sku").FromTable(TableName);

        Delete.Table(TableName);
    }
}