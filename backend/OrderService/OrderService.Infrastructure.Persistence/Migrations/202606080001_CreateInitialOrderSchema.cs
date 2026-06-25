using FluentMigrator;
using System.Data;

namespace OrderService.Infrastructure.Persistence.Migrations;

[Migration(202606080001)]
public class CreateInitialOrderSchema : Migration
{
    public override void Up()
    {
        Create.Table("Orders")
            .WithColumn("Id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("CustomerId").AsGuid().NotNullable()
            .WithColumn("CustomerName").AsString(255).NotNullable()
            .WithColumn("CustomerEmail").AsString(255).NotNullable()
            .WithColumn("DeliveryAddress").AsString(500).Nullable()
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("TotalAmount").AsDecimal(18, 2).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime().Nullable()
            .WithColumn("CancelledAt").AsDateTime().Nullable()
            .WithColumn("PaidAt").AsDateTime().Nullable()
            .WithColumn("Version").AsInt32().NotNullable();

        Create.Table("OrderItems")
            .WithColumn("Id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("OrderId").AsGuid().NotNullable()
            .WithColumn("ProductId").AsGuid().NotNullable()
            .WithColumn("ProductName").AsString(255).NotNullable()
            .WithColumn("Quantity").AsInt32().NotNullable()
            .WithColumn("PriceAtPurchase").AsDecimal(18, 2).NotNullable()
            .WithColumn("Subtotal").AsDecimal(18, 2).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("IsReserved").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("ReservedQuantity").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("UpdatedAt").AsDateTime().Nullable();

        Create.Table("OrderStatusHistories")
            .WithColumn("Id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("OrderId").AsGuid().NotNullable()
            .WithColumn("OldStatus").AsInt32().Nullable()
            .WithColumn("NewStatus").AsInt32().NotNullable()
            .WithColumn("ChangedAt").AsDateTime().NotNullable()
            .WithColumn("ChangedBy").AsGuid().Nullable()
            .WithColumn("Comment").AsString(1000).Nullable();

        Create.ForeignKey("FK_OrderItems_Orders")
            .FromTable("OrderItems").ForeignColumn("OrderId")
            .ToTable("Orders").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_OrderStatusHistories_Orders")
            .FromTable("OrderStatusHistories").ForeignColumn("OrderId")
            .ToTable("Orders").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.Index("IX_Orders_CustomerId").OnTable("Orders").OnColumn("CustomerId");
        Create.Index("IX_OrderItems_OrderId").OnTable("OrderItems").OnColumn("OrderId");

        Create.Index("IX_OrderStatusHistories_OrderId").OnTable("OrderStatusHistories").OnColumn("OrderId");

        Create.Index("IX_Orders_Status").OnTable("Orders").OnColumn("Status");
        Create.Index("IX_Orders_PaidAt").OnTable("Orders").OnColumn("PaidAt").Ascending();
    }

    public override void Down()
    {
        Delete.Table("OrderStatusHistories");
        Delete.Table("OrderItems");
        Delete.Table("Orders");
    }
}
