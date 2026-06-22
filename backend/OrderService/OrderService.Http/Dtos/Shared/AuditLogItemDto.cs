namespace OrderService.Http.Dtos.Shared;

public record AuditLogItemDto(
    DateTime Timestamp,
    string Action,
    Guid? AdminId,
    string? Comment);