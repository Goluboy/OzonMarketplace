using DotNetCore.CAP.Filter;
using DotNetCore.CAP.Messages;
using ProductService.Infrastructure.Saga.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ProductService.Infrastructure.Saga.Filters;

public class SagaCorrelationFilter : SubscribeFilter
{
    public override Task OnSubscribeExecutingAsync(ExecutingContext context)
    {
        if (context.DeliverMessage.Headers.TryGetValue(Headers.CorrelationId, out var correlationId)
            && correlationId != null)
        {
            SagaCorrelationContext.CorrelationId = correlationId.ToString();

            Activity.Current?.SetTag("saga.order_id", correlationId);

            //Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId);
        }

        return Task.CompletedTask;
    }

    public override Task OnSubscribeExecutedAsync(ExecutedContext context)
    {
        SagaCorrelationContext.CorrelationId = null;
        return Task.CompletedTask;
    }
}