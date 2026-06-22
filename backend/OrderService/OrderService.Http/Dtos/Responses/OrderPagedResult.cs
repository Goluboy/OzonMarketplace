using OrderService.Http.Dtos.Requests;

namespace OrderService.Http.Dtos.Responses;

public record OrderPagedResult(
    List<OrderDto> Items,
    int TotalCount,
    int Page,
    int PageSize);