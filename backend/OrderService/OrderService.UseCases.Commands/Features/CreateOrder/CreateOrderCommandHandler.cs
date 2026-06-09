using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.Shared;
using MassTransit;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;

namespace OrderService.UseCases.Commands.Features.CreateOrder;

public class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint) : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken ct = default)
    {
        await unitOfWork.BeginTransactionAsync(ct);

        try
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

            await orderRepository.SaveAsync(order, ct);

            await unitOfWork.CommitAsync(ct);

            var integrationEvent = new OrderCreatedEvent
            {
                CorrelationId = order.Id.Value,
                CustomerEmail = order.CustomerEmail.Value,
                DeliveryAddress = order.DeliveryAddress?.AddressLine ?? string.Empty,
                Items = order.Items.Select(i => new OrderItemDto(i.ProductId, i.Quantity)).ToList()
            };

            await publishEndpoint.Publish(integrationEvent, ct);

            return order.Id.Value;
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
