namespace OrderService.UseCases.Queries.Models;

public record AuditLogItemModel(
    DateTime Timestamp,
    string Action,
    Guid? AdminId,
    string? Comment);