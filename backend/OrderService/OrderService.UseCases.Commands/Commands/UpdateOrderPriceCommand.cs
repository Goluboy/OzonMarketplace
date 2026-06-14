using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.UseCases.Commands.Commands;

public record UpdateOrderPriceCommand(
    Guid OrderId,
    decimal TotalAmount,
    string Currency,
    string? MessageId = null
) : ICommand<bool>;
