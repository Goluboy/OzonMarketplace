using FluentValidation;
using OrderService.Http.Dtos;

namespace OrderService.Http.Dtos.Validators;

public class OrderItemCreateValidator : AbstractValidator<OrderItemCreate>
{
    public OrderItemCreateValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, 99);
    }
}