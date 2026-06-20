using IntegrationEvents;
using IntegrationEvents.IntegrationEvents.Order;
using IntegrationEvents.Shared;
using ProductService.Infrastructure.Abstractions.EventHandlers.Abstractions;
using ProductService.Infrastructure.Abstractions.EventPublisher.Abstractions;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;

namespace ProductService.Application.EventHandlers;

public class OrderCreatedEventHandler(IProductRepository productRepository, IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher) : IOrderCreatedEventHandler
{
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        await unitOfWork.BeginOutboxTransactionAsync();
        try
        {
            var isReservationSuccessful = true;
            var failedProductIds = new List<Guid>();
            var reservedItems = new List<ReservedItemDto>();
            decimal totalAmount = 0;
            var currency = "RUB";
            
            foreach (var item in @event.Items)
            {
                var product = await productRepository.GetAsync(item.ProductId);
                if (product == null)
                {
                    isReservationSuccessful = false;
                    failedProductIds.Add(item.ProductId);
                    continue;
                }

                currency = product.Price.Currency;
                reservedItems.Add(new ReservedItemDto(item.ProductId, item.Quantity));
                totalAmount += product.Price.Amount * item.Quantity;
            }

            var headers = new Dictionary<string, string?>
            {
                { "sharding-key", @event.OrderId.ToString() },
                { "correlation-id", @event.CorrelationId.ToString() }
            };

            var priceCalculatedEvent = new PriceCalculatedEvent
            {
                CorrelationId = @event.CorrelationId,
                Currency = currency,
                TotalAmount = totalAmount,
            };
            
            await eventPublisher.PublishAsync(Topics.Products.ProductsTopic, priceCalculatedEvent, headers);
            
            if (isReservationSuccessful)
            {
                var stockReservedEvent = new StockReservedEvent
                {
                    CorrelationId = @event.CorrelationId,
                    OccurredOn = DateTime.UtcNow,
                    ReservedItems = reservedItems
                };

                await eventPublisher.PublishAsync(Topics.Products.ProductsTopic, stockReservedEvent, headers);
            }
            else
            {
                var stockFailedEvent = new StockReservationFailedEvent
                {
                    EventId = Guid.NewGuid(),
                    CorrelationId = @event.CorrelationId,
                    OccurredOn = DateTime.UtcNow,
                    Reason = "Out of stock. One or more products are unavailable.",
                    FailedProductIds = failedProductIds
                };
                
                await eventPublisher.PublishAsync(Topics.Products.ProductsTopic, stockFailedEvent, headers);
            }

            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}