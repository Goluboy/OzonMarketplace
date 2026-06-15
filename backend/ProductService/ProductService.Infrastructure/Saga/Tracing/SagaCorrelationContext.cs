using System;
using System.Collections.Generic;
using System.Text;

namespace ProductService.Infrastructure.Saga.Tracing;

public static class SagaCorrelationContext
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    public static string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }
}
