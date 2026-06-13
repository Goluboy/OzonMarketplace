using FluentMigrator;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.Persistence.Migrations;

[Migration(202606130001, "Create CAP tables for outbox and monitoring")]
public class CreateCapTables : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS cap;");

        Create.Table("published").WithDescription("CAP published events (outbox)")
            .InSchema("cap")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("Version").AsString(20).NotNullable()
            .WithColumn("Name").AsString(400).NotNullable()
            .WithColumn("Content").AsCustom("jsonb").Nullable()
            .WithColumn("Retries").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("Added").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("ExpiresAt").AsDateTime().Nullable()
            .WithColumn("StatusName").AsString(50).NotNullable();

        Create.Table("received").WithDescription("CAP received events")
            .InSchema("cap")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("Version").AsString(20).NotNullable()
            .WithColumn("Name").AsString(400).NotNullable()
            .WithColumn("Group").AsString(200).Nullable()
            .WithColumn("Content").AsCustom("jsonb").Nullable()
            .WithColumn("Retries").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("Added").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("ExpiresAt").AsDateTime().Nullable()
            .WithColumn("StatusName").AsString(50).NotNullable();

        Create.Table("lock").WithDescription("CAP distributed lock")
            .InSchema("cap")
            .WithColumn("Key").AsString(128).PrimaryKey()
            .WithColumn("Instance").AsString(256).NotNullable()
            .WithColumn("ExpiresAt").AsDateTime().NotNullable();

        Create.Index("idx_cap_published_status")
            .OnTable("published").InSchema("cap")
            .OnColumn("StatusName").Ascending();

        Create.Index("idx_cap_published_expires")
            .OnTable("published").InSchema("cap")
            .OnColumn("ExpiresAt").Ascending();

        Create.Index("idx_cap_received_status")
            .OnTable("received").InSchema("cap")
            .OnColumn("StatusName").Ascending();

        Create.Index("idx_cap_received_expires")
            .OnTable("received").InSchema("cap")
            .OnColumn("ExpiresAt").Ascending();
    }

    public override void Down()
    {
        Delete.Table("lock").InSchema("cap");
        Delete.Table("received").InSchema("cap");
        Delete.Table("published").InSchema("cap");
        Execute.Sql("DROP SCHEMA IF EXISTS cap CASCADE;");
    }
}