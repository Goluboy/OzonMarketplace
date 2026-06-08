using FluentMigrator;
using System.Reflection;
using OrderService.Infrastructure.utils;

namespace OrderService.Infrastructure.Migrations;

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