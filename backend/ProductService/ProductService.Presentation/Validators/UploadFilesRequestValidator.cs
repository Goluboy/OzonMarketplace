using FluentValidation;
using ProductService.Presentation.Models;

namespace ProductService.Presentation.Validators;

public class UploadFilesRequestValidator : AbstractValidator<UploadFilesRequest>
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public UploadFilesRequestValidator()
    {
        RuleFor(x => x.FileNames)
            .NotNull().WithMessage("File list cannot be null.")
            .NotEmpty().WithMessage("File list cannot be empty.")
            .Must(x => x is { Count: <= 10 }).WithMessage("You can request a maximum of 10 files per batch.");
        
        RuleForEach(x => x.FileNames)
            .NotEmpty().WithMessage("File name cannot be empty.")
            .MaximumLength(100).WithMessage("File name must not exceed 100 characters.")
            .Must(HasValidExtension).WithMessage($"Only image files are allowed. Allowed extensions: {string.Join(", ", AllowedExtensions)}")
            .Must(ContainNoPathTraversal).WithMessage("File name contains invalid directory path elements (e.g. '..', '/' or '\\').")
            .Must(ContainOnlyValidFileNameChars).WithMessage("File name contains invalid characters.");
    }

    private static bool HasValidExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        return AllowedExtensions.Contains(extension.ToLowerInvariant());
    }

    private static bool ContainNoPathTraversal(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }
        
        return !fileName.Contains("..") && !fileName.Contains('/') && !fileName.Contains('\\');
    }

    private static bool ContainOnlyValidFileNameChars(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        return !fileName.Any(c => invalidChars.Contains(c));
    }
}