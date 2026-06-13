namespace ProductService.Infrastructure.Abstractions.Helpers.Abstractions;

public interface ICurrentUserHelper
{
    Guid UserId { get; }
    bool IsAdmin { get;}
}