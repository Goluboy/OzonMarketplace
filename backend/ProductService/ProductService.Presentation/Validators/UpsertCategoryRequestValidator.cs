using FluentValidation;
using ProductService.Presentation.Models;

namespace ProductService.Presentation.Validators;

public class UpsertCategoryRequestValidator : AbstractValidator<UpsertCategoryRequest>
{
    public UpsertCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название категории обязательно для заполнения.")
            .MaximumLength(120).WithMessage("Название категории не может превышать 120 символов.");

        RuleFor(x => x.Path)
            .NotEmpty().WithMessage("Путь категории обязателен для заполнения.")
            .MaximumLength(120).WithMessage("Путь категории не может превышать 120 символов.")
            .Matches(@"^([a-z0-9-_]+)(\.[a-z0-9-_]+)*$")
            .WithMessage("Путь должен состоять из сегментов в нижнем регистре, разделенных точкой (например: electronics.smartphones).");
    }
}