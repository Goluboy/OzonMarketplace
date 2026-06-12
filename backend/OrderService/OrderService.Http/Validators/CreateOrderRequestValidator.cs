using FluentValidation;
using OrderService.Http.Dtos;
using OrderService.Http.Dtos.Requests;

namespace OrderService.Http.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator(IValidator<OrderItemCreate> itemValidator)
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.CustomerEmail)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.DeliveryAddress)
            .MaximumLength(500)
            .When(x => x.DeliveryAddress is not null);

        RuleFor(x => x.Items)
            .NotNull()
            .NotEmpty()
            .Must(items => items.Count is >= 1 and <= 100)
            .WithMessage("Items must contain between 1 and 100 elements.");

        RuleForEach(x => x.Items).SetValidator(itemValidator);
    }
}
