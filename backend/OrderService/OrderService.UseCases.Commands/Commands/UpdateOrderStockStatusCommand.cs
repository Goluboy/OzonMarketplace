using IntegrationEvents.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.UseCases.Commands.Commands;

public record UpdateOrderStockStatusCommand(
    Guid OrderId,
    List<ReservedItemDto> ReservedItems,
    string? MessageId = null
) : ICommand<bool>;