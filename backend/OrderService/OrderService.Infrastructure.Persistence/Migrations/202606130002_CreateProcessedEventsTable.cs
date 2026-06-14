using FluentMigrator;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.Persistence.Migrations;

[Migration(202606130002)]
public class CreateProcessedEventsTable : Migration
{
    public override void Up()
    {
        Create.Table("ProcessedEvents")
            .WithColumn("MessageId").AsString(100).PrimaryKey()
            .WithColumn("EventName").AsString(200).NotNullable()
            .WithColumn("ProcessedAt").AsDateTime().NotNullable();

        Create.Index("idx_processed_events_processed_at")
            .OnTable("ProcessedEvents")
            .OnColumn("ProcessedAt").Ascending();
    }

    public override void Down() => Delete.Table("ProcessedEvents");
}
