using FluentMigrator;
using ProductService.Infrastructure.DAO;
using ProductService.Infrastructure.Helpers;

namespace ProductService.Infrastructure.Migrations;

[Migration(20260530181500)]
public class CreateOutboxMessagesMigration : Migration
{
    public override void Up()
    {
        Create.Table("outbox_messages")
            .WithColumn(nameof(OutboxMessageDao.Id).ToSnakeCase()).AsGuid().PrimaryKey()
            .WithColumn(nameof(OutboxMessageDao.Type).ToSnakeCase()).AsString(255).NotNullable()
            .WithColumn(nameof(OutboxMessageDao.Payload).ToSnakeCase()).AsCustom("TEXT").NotNullable()
            .WithColumn(nameof(OutboxMessageDao.CreatedAt).ToSnakeCase()).AsDateTimeOffset().NotNullable()
            .WithColumn(nameof(OutboxMessageDao.ProcessedAt).ToSnakeCase()).AsDateTimeOffset().Nullable();
        
        Execute.Sql("CREATE INDEX ix_outbox_unprocessed_partial ON outbox_messages(created_at) WHERE processed_at IS NULL;");
    }

    public override void Down()
    {
        Delete.Index("ix_outbox_unprocessed_partial").OnTable("outbox_messages");

        Delete.Table("outbox_messages");
    }
}