using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.EventBus.Tracing;

public static class SagaCorrelationContext
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    public static string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }
}
