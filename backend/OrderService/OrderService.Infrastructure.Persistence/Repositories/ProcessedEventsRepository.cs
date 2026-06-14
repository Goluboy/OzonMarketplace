using Dapper;
using OrderService.Domain.Interfaces.Persistence;
using OrderService.Infrastructure.Persistence.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Infrastructure.Persistence.Repositories;

public class ProcessedEventsRepository(IDbSession dbSession) : IProcessedEventsRepository
{
    public async Task<bool> IsProcessedAsync(string messageId, CancellationToken ct)
    {
        const string sql = """
            SELECT EXISTS(
                SELECT 1 FROM "ProcessedEvents" 
                WHERE "MessageId" = @MessageId
            )
            """;
        return await dbSession.Connection.ExecuteScalarAsync<bool>(
            sql, new { MessageId = messageId }, dbSession.Transaction);
    }

    public async Task MarkAsProcessedAsync(
        string messageId, string eventName, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "ProcessedEvents" ("MessageId", "EventName", "ProcessedAt")
            VALUES (@MessageId, @EventName, @ProcessedAt)
            ON CONFLICT ("MessageId") DO NOTHING
            """;
        await dbSession.Connection.ExecuteAsync(sql, new
        {
            MessageId = messageId,
            EventName = eventName,
            ProcessedAt = DateTime.UtcNow
        }, dbSession.Transaction);
    }

    public async Task CleanupOlderThanAsync(DateTime olderThan, CancellationToken ct)
    {
        const string sql = """
            DELETE FROM "ProcessedEvents" 
            WHERE "ProcessedAt" < @OlderThan
            """;
        await dbSession.Connection.ExecuteAsync(sql, new { OlderThan = olderThan });
    }
}
