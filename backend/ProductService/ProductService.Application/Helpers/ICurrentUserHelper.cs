namespace ProductService.Application.Helpers;

public interface ICurrentUserHelper
{
    Guid UserId { get; }
    bool IsAdmin { get;}
}