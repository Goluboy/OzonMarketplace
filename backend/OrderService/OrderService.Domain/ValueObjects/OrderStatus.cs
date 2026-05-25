namespace OrderService.Domain.ValueObjects;

public enum OrderStatus
{
    Created,
    Paid,
    Assembling,
    Shipping,
    Delivered,
    Cancelled
}