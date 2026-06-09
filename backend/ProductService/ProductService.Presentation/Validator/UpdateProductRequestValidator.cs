using FluentValidation;
using ProductService.Presentation.Models;

namespace ProductService.Presentation.Validator;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public  UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название товара обязательно для заполнения.")
            .MaximumLength(255).WithMessage("Название товара не может превышать 200 символов.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Описание товара обязательно для заполнения.")
            .MaximumLength(2000).WithMessage("Описание товара не может превышать 2000 символов.");

        RuleFor(x => x.Price)
            .NotNull().WithMessage("Цена товара обязательна.")
            .SetValidator(new MoneyDtoValidator());

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Идентификатор категории должен быть положительным числом.");
        
        RuleFor(x => x.Images)
            .NotNull().WithMessage("Список изображений не может быть null.");
            
        RuleForEach(x => x.Images)
            .SetValidator(new ProductImageDtoValidator());
    }
}