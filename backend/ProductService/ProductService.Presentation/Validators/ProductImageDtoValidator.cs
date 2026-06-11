using FluentValidation;
using ProductService.Presentation.Models;

namespace ProductService.Presentation.Validators;

public class ProductImageDtoValidator : AbstractValidator<ProductImageHttpDto>
{
    public ProductImageDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Ссылка на изображение не может быть пустой.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out var outUri) 
                         && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Ссылка на изображение '{PropertyValue}' имеет неверный формат URL.");
    }
}