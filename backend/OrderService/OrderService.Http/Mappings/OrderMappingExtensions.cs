using OrderService.Http.Dtos.Requests;
using OrderService.Http.Dtos.Shared;
using OrderService.UseCases.Queries.Models;

namespace OrderService.Http.Mappings;

public static class OrderMappingExtensions
{
    public static OrderDto ToDto(this OrderModel model)
    {
        return new OrderDto(
            model.Id,
            model.Status,
            model.CreatedAt,
            model.UpdatedAt,
            model.CustomerName,
            model.CustomerEmail,
            model.DeliveryAddress,
            new MoneyDto(model.TotalAmount.ToString(), model.Currency),
            model.Items.Select(item => item.ToDto()).ToList());
    }

    public static OrderItemDto ToDto(this OrderItemModel model)
    {
        return new OrderItemDto(
            model.ProductId,
            model.ProductName,
            model.Quantity,
            new MoneyDto(model.Price.ToString(), model.Currency));
    }

    public static AdminOrderDto ToAdminDto(this OrderModel model)
    {
        return new AdminOrderDto(
            model.Id,
            model.Status,
            model.CreatedAt,
            model.UpdatedAt,
            model.CustomerName,
            model.CustomerEmail,
            model.DeliveryAddress,
            new MoneyDto(model.TotalAmount.ToString(), model.Currency),
            model.Items.Select(item => item.ToDto()).ToList(),
            model.CustomerId);
    }
}
