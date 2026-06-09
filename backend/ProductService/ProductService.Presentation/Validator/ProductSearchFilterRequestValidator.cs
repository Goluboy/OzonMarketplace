using System.Globalization;
using FluentValidation;
using ProductService.Presentation.Models;

namespace ProductService.Presentation.Validator;

public class ProductSearchFilterRequestValidator : AbstractValidator<ProductSearchFilterRequest>
{
    public ProductSearchFilterRequestValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(100).WithMessage("Поисковый запрос не может превышать 100 символов.")
            .When(x => !string.IsNullOrEmpty(x.Search));
        
        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Идентификатор категории должен быть положительным числом.")
            .When(x => x.CategoryId.HasValue);
        
        RuleFor(x => x.MinPrice)
            .SetValidator(new MoneyDtoValidator()!)
            .When(x => x.MinPrice != null);

        RuleFor(x => x.MaxPrice)
            .SetValidator(new MoneyDtoValidator()!)
            .When(x => x.MaxPrice != null);
        
        RuleFor(x => x)
            .Must(x => x.MinPrice!.Currency.Equals(x.MaxPrice!.Currency, StringComparison.InvariantCultureIgnoreCase))
            .When(x => x.MinPrice != null && x.MaxPrice != null)
            .WithMessage("Валюты минимальной и максимальной цены должны совпадать.")
            .WithName("Price.Currency");

        RuleFor(x => x)
            .Must(x =>
            {
                var isMinParsed = decimal.TryParse(x.MinPrice!.Amount, CultureInfo.InvariantCulture, out var min);
                var isMaxParsed = decimal.TryParse(x.MaxPrice!.Amount, CultureInfo.InvariantCulture, out var max);

                return isMinParsed && isMaxParsed && min <= max;
            })
            .When(x => x.MinPrice != null && x.MaxPrice != null && x.MinPrice.Currency.Equals(x.MaxPrice.Currency, StringComparison.InvariantCultureIgnoreCase))
            .WithMessage("Минимальная цена не может быть больше максимальной цены.")
            .WithName("Price.Amount");
        
        RuleFor(x => x.SortBy)
            .NotEmpty().WithMessage("Поле сортировки обязательно для заполнения.")
            .Must(sortBy => new[] { "name", "price", "createdAt" }.Contains(sortBy))
            .WithMessage("Сортировка возможна только по полям: 'name', 'price', 'createdAt'.");

        RuleFor(x => x.SortOrder)
            .NotEmpty().WithMessage("Порядок сортировки обязателен для заполнения.")
            .Must(order => new[] { "asc", "desc" }.Contains(order.ToLowerInvariant()))
            .WithMessage("Порядок сортировки должен быть 'asc' или 'desc'.");
        
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Количество элементов на странице должно быть в диапазоне от 1 до 100.");
    }
}