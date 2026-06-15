using IntegrationEvents.IntegrationEvents.Order;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProductService.Infrastructure.Abstractions.EventHandlers.Abstractions;

public interface IStockReservedEventHandler
{
    Task HandleAsync(StockReservedEvent @event);
}
