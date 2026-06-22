using FluentValidation;
using OrderService.Http.Dtos.Requests;

namespace OrderService.Http.Dtos.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerName)
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.CustomerEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.CustomerEmail));

        RuleFor(x => x.Items)
            .NotEmpty()
            .Must(items => items.Count >= 1 && items.Count <= 100)
            .WithMessage("Order must contain between 1 and 100 items.");
        RuleForEach(x => x.Items).SetValidator(new OrderItemCreateValidator());

        RuleFor(x => x.DeliveryAddress)
            .MaximumLength(500);
    }
}