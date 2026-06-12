namespace ProductService.Infrastructure.Caching;

public interface ICategoryVersionUpdater
{
    Task UpdateVersionAsync(CancellationToken ct = default);
}