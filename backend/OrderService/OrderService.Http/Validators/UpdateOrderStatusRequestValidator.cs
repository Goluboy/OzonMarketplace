using FluentValidation;
using OrderService.Http.Dtos;

namespace OrderService.Http.Validators;

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .WithMessage("Status must be one of: Created, Paid, Assembling, Shipping, Delivered, Cancelled.");

        RuleFor(x => x.Comment)
            .MaximumLength(500)
            .When(x => x.Comment is not null);
    }
}
