using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Domain.Interfaces.Persistence;

public interface IProcessedEventsRepository
{
    Task<bool> IsProcessedAsync(string messageId, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(
        string messageId,
        string eventName,
        CancellationToken cancellationToken = default);
    Task CleanupOlderThanAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
