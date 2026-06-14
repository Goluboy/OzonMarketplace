namespace OrderService.Http.Dtos;

public record OrderPagedResult(
    List<OrderDto> Items,
    int TotalCount,
    int Page,
    int PageSize);