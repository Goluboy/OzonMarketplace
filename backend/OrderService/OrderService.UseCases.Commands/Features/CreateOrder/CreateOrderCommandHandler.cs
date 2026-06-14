using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.Shared;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;
using OrderCreatedEvent = IntegrationEvents.IntegrationEvents.OrderCreatedEvent;

namespace OrderService.UseCases.Commands.Features.CreateOrder;

public class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    ICapPublisher capPublisher)
    : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        var items = command.Items.Select(item =>
            OrderItem.Create(item.ProductId, item.ProductName, item.Quantity, item.Price)
        ).ToList();

        var order = Order.Create(
            command.CustomerId,
            command.CustomerName,
            command.CustomerEmail,
            command.DeliveryAddress,
            items);

        await orderRepository.SaveAsync(order, cancellationToken);

        var sagaHeaders = new Dictionary<string, string?>
        {
            [Headers.CorrelationId] = order.Id.Value.ToString()
        };

        await capPublisher.PublishAsync(
            Topics.Orders.Created,
            new OrderCreatedEvent
            {
                CorrelationId = order.Id,
                CustomerEmail = order.CustomerEmail.Value,
                DeliveryAddress = order.DeliveryAddress?.AddressLine ?? string.Empty,
                Items = order.Items.Select(i => new OrderItemDto(i.ProductId, i.Quantity)).ToList()
            },
            sagaHeaders,
            cancellationToken);

        await capPublisher.PublishDelayAsync(
                TimeSpan.FromSeconds(61),
                Topics.Orders.SagaTimeout,
                new OrderSagaTimeout() { CorrelationId = order.Id },
                sagaHeaders,
                cancellationToken);


        return order.Id.Value;
    }
}