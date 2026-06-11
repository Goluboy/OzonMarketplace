using FluentMigrator;
using OrderService.Infrastructure.Persistence.utils;

namespace OrderService.Infrastructure.Persistence.Migrations;

[Migration(202606080002)]
public class CreateQuartzSchema : Migration
{
    public override void Up()
    {
        var sql = ScriptReader.ReadEmbeddedScript("CreateQuartzTablePostgres.sql");
        Execute.Sql(sql);
    }

    public override void Down()
    {
        var sql = ScriptReader.ReadEmbeddedScript("DownQuartzTables.sql");
        Execute.Sql(sql);
    }
}