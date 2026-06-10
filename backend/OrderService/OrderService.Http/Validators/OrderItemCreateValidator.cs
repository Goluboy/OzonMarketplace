using FluentValidation;
using OrderService.Http.Dtos;

namespace OrderService.Http.Validators;

public class OrderItemCreateValidator : AbstractValidator<OrderItemCreate>
{
    public OrderItemCreateValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ProductId must be a valid UUID.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 99);
    }
}
