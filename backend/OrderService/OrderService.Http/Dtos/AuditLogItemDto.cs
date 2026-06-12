using OrderService.Http.Dtos.Shared;

namespace OrderService.Http.Dtos;

public record AuditLogItemDto(
    DateTime Timestamp,
    string Action,
    Guid? AdminId,
    string? Comment);