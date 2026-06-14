namespace OrderService.Http.Dtos;

public record AdminOrderPagedResult(
    List<AdminOrderDto> Items,
    int TotalCount,
    int Page,
    int PageSize);