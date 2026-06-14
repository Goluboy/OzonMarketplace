using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.EventBus.Tracing;

public class CorrelatedCapPublisher : ICapPublisher
{
    private readonly ICapPublisher _inner;

    public CorrelatedCapPublisher(ICapPublisher inner) => _inner = inner;

    public IServiceProvider ServiceProvider => _inner.ServiceProvider;

    public ICapTransaction? Transaction
    {
        get => _inner.Transaction;
        set => _inner.Transaction = value;
    }

    private IDictionary<string, string?> EnsureHeaders(IDictionary<string, string?>? headers, string? callbackName = null)
    {
        var dict = headers != null
            ? new Dictionary<string, string?>(headers)
            : new Dictionary<string, string?>();

        var correlationId = SagaCorrelationContext.CorrelationId;

        if (!string.IsNullOrEmpty(correlationId) && !dict.ContainsKey(Headers.CorrelationId))
        {
            dict[Headers.CorrelationId] = correlationId;
        }

        if (!string.IsNullOrEmpty(callbackName) && !dict.ContainsKey(Headers.CallbackName))
        {
            dict[Headers.CallbackName] = callbackName;
        }

        return dict;
    }

    public Task PublishAsync<T>(string name, T? contentObj, string? callbackName = null, CancellationToken cancellationToken = default)
    {
        var headers = EnsureHeaders(null, callbackName);
        return _inner.PublishAsync(name, contentObj, headers, cancellationToken);
    }

    public Task PublishAsync<T>(string name, T? contentObj, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
    {
        var finalHeaders = EnsureHeaders(headers);
        return _inner.PublishAsync(name, contentObj, finalHeaders, cancellationToken);
    }

    public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? contentObj, string? callbackName = null, CancellationToken cancellationToken = default)
    {
        var headers = EnsureHeaders(null, callbackName);
        return _inner.PublishDelayAsync(delayTime, name, contentObj, headers, cancellationToken);
    }

    public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? contentObj, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
    {
        var finalHeaders = EnsureHeaders(headers);
        return _inner.PublishDelayAsync(delayTime, name, contentObj, finalHeaders, cancellationToken);
    }

    public void Publish<T>(string name, T? contentObj, string? callbackName = null)
    {
        var headers = EnsureHeaders(null, callbackName);
        _inner.Publish(name, contentObj, headers);
    }

    public void Publish<T>(string name, T? contentObj, IDictionary<string, string?> headers)
    {
        var finalHeaders = EnsureHeaders(headers);
        _inner.Publish(name, contentObj, finalHeaders);
    }

    public void PublishDelay<T>(TimeSpan delayTime, string name, T? contentObj, string? callbackName = null)
    {
        var headers = EnsureHeaders(null, callbackName);
        _inner.PublishDelay(delayTime, name, contentObj, headers);
    }

    public void PublishDelay<T>(TimeSpan delayTime, string name, T? contentObj, IDictionary<string, string?> headers)
    {
        var finalHeaders = EnsureHeaders(headers);
        _inner.PublishDelay(delayTime, name, contentObj, finalHeaders);
    }
}