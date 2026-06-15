using Amazon.Runtime.Internal.Util;
using DotNetCore.CAP.Messages;
using IntegrationEvents;
using IntegrationEvents.IntegrationEvents.Order;
using Microsoft.Extensions.Logging;
using ProductService.Application.Services.Products.Command;
using ProductService.Application.Services.Products.Query;
using ProductService.Infrastructure.Abstractions.EventHandlers.Abstractions;
using ProductService.Infrastructure.Abstractions.EventPublisher.Abstractions;
using ProductService.Infrastructure.Abstractions.UnitOfWork.Abstractions;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ProductService.Application.EventHandlers;

public class StockReservedEventHandler(
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher,
    IProductQueryService productQueryService) : IStockReservedEventHandler
{
    public async Task HandleAsync(StockReservedEvent @event)
    {
        decimal totalAmount = 0;

        foreach (var reservedItem in @event.ReservedItems)
        {
            var product = await productQueryService.GetProductAsync(
                reservedItem.ProductId);

            if (product is null)
            {
                throw new InvalidOperationException(
                    $"Product '{reservedItem.ProductId}' not found. Cannot calculate price.");
            }

            var itemSubtotal = product.PriceAmount * reservedItem.Quantity;
            totalAmount += itemSubtotal;
        }

        await unitOfWork.BeginOutboxTransactionAsync();
        try
        {
            var priceCalculatedEvent = new PriceCalculatedEvent
            {
                CorrelationId = @event.CorrelationId,
                OccurredOn = DateTime.UtcNow,
                TotalAmount = totalAmount,
                Currency = "RUB"
            };

            await eventPublisher.PublishAsync(
                Topics.Prices.PricesTopic,
                priceCalculatedEvent,
                CreateHeaders(@event.CorrelationId));

            await unitOfWork.CommitAsync();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    private static Dictionary<string, string?> CreateHeaders(Guid correlationId)
    {
        return new Dictionary<string, string?>
        {
            [Headers.CorrelationId] = correlationId.ToString(),
            ["sharding-key"] = correlationId.ToString()
        };
    }
}
