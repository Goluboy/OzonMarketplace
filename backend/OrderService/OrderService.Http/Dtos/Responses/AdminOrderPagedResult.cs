using OrderService.Http.Dtos.Requests;

namespace OrderService.Http.Dtos.Responses;

public record AdminOrderPagedResult(
    List<AdminOrderDto> Items,
    int TotalCount,
    int Page,
    int PageSize);