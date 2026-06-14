using FluentValidation;
using OrderService.Http.Dtos.Requests;

namespace OrderService.Http.Dtos.Validators;

public class ForceCancelOrderRequestValidator : AbstractValidator<ForceCancelOrderRequest>
{
    public ForceCancelOrderRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500);
    }
}