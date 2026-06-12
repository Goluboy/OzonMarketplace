using FluentValidation;
using OrderService.Domain.ValueObjects;
using OrderService.Http.Dtos.Shared;

namespace OrderService.Http.Dtos;

public record UpdateOrderStatusRequest(
    OrderStatus NewStatus,
    string? Comment);