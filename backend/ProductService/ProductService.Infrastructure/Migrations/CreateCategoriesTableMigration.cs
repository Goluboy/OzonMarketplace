using FluentMigrator;
using ProductService.Infrastructure.DAO;
using ProductService.Infrastructure.Helpers;

namespace ProductService.Infrastructure.Migrations;

[Migration(202605301700000)]
public class CreateCategoriesTableMigration : Migration
{
    public override void Up()
    {
        Create.Table("categories")
            .WithColumn(nameof(CategoryDao.Id).ToSnakeCase()).AsInt32().PrimaryKey().Identity()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("path").AsString(120).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("categories");
    }
}