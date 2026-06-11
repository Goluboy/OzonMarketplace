using FluentValidation;
using ProductService.Presentation.Models;

namespace ProductService.Presentation.Validators;

public class MoneyDtoValidator : AbstractValidator<MoneyHttpDto>
{
    public MoneyDtoValidator()
    {
        RuleFor(x => x.Amount)
            .NotEmpty().WithMessage("Сумма обязательна для заполнения.")
            .Matches(@"^\d+\.\d{2}$")
            .WithMessage("Сумма должна быть строкой с ровно 2 знаками после запятой через точку (например, '1299.99').");


        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Код валюты обязателен для заполнения.")
            .Matches(@"^[A-Z]{3}$")
            .WithMessage("Код валюты должен состоять ровно из 3 заглавных латинских букв (например, RUB).");
    }
}