namespace Redis.Service;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task<IReadOnlyList<T?>> GetManyAsync<T>(IReadOnlyList<string> keys, CancellationToken ct = default);
    
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task SetManyAsync<T>(IReadOnlyDictionary<string, T> values, TimeSpan? expiry = null, CancellationToken ct = default);
    
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken ct = default);
}