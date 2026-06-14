using FluentValidation;

namespace OrderService.Http.Dtos.Validators;

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus)
            .IsInEnum();

        RuleFor(x => x.Comment)
            .MaximumLength(500);
    }
}