using System;
using System.Collections.Generic;
using System.Text;

namespace ProductService.IntegrationEvents.IntegrationEvents;

public record StockReservationFailedEvent : IntegrationEvent
{
    public string Reason { get; init; } = string.Empty;  // "InsufficientStock", "ProductNotFound", "DatabaseError"
    public List<Guid> FailedProductIds { get; init; } = new();
}
