using OrderService.Domain.Entities;

namespace OrderService.Domain.Events;

public static class OrderItemExtensions
{
    public static OrderItemSnapshot ToSnapshot(this OrderItem item) => new(
        item.Id,
        item.ProductId,
        item.ProductName,
        item.Quantity,
        item.PriceAtPurchase,
        item.Subtotal);
}