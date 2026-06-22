using FluentValidation;
using OrderService.Http.Dtos.Shared;

namespace OrderService.Http.Validators.Shared;

public class MoneyValidator : AbstractValidator<MoneyDto>
{
    public MoneyValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Amount)
            .NotEmpty()
            .WithMessage("Amount is required")
            .Matches(@"^\d+\.\d{2}$")
            .WithMessage("Amount must be in format '123.45' (2 decimal places)")
            .Must(BeNonNegative)
            .WithMessage("Amount cannot be negative");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Matches(@"^[A-Z]{3}$")
            .WithMessage("Currency must be a 3-letter ISO 4217 code (e.g., RUB, USD, EUR)");
    }

    private static bool BeNonNegative(string amount)
    {
        if (decimal.TryParse(amount, out var value))
            return value >= 0;
        return false;
    }
}