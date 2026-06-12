namespace ProductService.Infrastructure.Abstractions.Caching.Abstractions;

public interface ICategoryVersionProvider
{
    Task<string> GetVersionETagAsync(CancellationToken ct = default);
}