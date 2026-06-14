using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Redis.Provider;
using StackExchange.Redis;

namespace Redis.Service;

[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
internal class RedisCacheService : ICacheService
{
    private readonly IRedisConnectionFactory _connectionFactory;
    private readonly string _prefix;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly CacheCircuitBreaker _breaker = new();

    public RedisCacheService(IRedisConnectionFactory connectionFactory, IOptions<RedisOptions> options,
        JsonSerializerOptions? jsonOptions = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        
        _prefix = options.Value.InstancePrefix ??  throw new ArgumentException("Prefix cannot be null or empty", nameof(options));

        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
    
    private IDatabase Db => _connectionFactory.GetDatabase();

    private string GetFullKey(string key) => $"{_prefix}{key}";
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }
        
        if (!_breaker.IsAllowed())
        {
            return default;
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(200));

            var value = await Db.StringGetAsync(GetFullKey(key)).WaitAsync(timeoutCts.Token);
            
            if (value.IsNullOrEmpty)
            {
                _breaker.RecordSuccess();
                return default;
            }
            
            _breaker.RecordSuccess();
            
            ReadOnlyMemory<byte> bytes = value;
            return JsonSerializer.Deserialize<T>(bytes.Span, _jsonOptions);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or OperationCanceledException)
        {
            _breaker.RecordFailure();
            
            return default;
        }
    }

    public async Task<IReadOnlyList<T?>> GetManyAsync<T>(IReadOnlyList<string> keys, CancellationToken ct = default)
    {
        if (keys == null || keys.Count == 0)
        {
            return [];
        }

        if (!_breaker.IsAllowed())
        {
            return new T?[keys.Count];
        }
        
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(300));

            var redisKeys = keys.Select(k => (RedisKey)GetFullKey(k)).ToArray();
            var values = await Db.StringGetAsync(redisKeys).WaitAsync(timeoutCts.Token);
            
            _breaker.RecordSuccess();

            var results = new List<T?>(keys.Count);
            foreach (var value in values)
            {
                if (value.IsNullOrEmpty)
                {
                    results.Add(default);
                }
                else
                {
                    ReadOnlyMemory<byte> bytes = value;
                    var deserialized = JsonSerializer.Deserialize<T>(bytes.Span, _jsonOptions);
                    results.Add(deserialized);
                }
            }

            return results;
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or OperationCanceledException)
        {
            _breaker.RecordFailure();
            return new T?[keys.Count];
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }
        if (value == null)
        {
            throw new ArgumentNullException( nameof(value), "Value cannot be null.");
        }
        
        if (!_breaker.IsAllowed())
        {
            return;
        }
        
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(300));

            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
            var redisKey = GetFullKey(key);
            var expiration = expiry ?? TimeSpan.Zero;
            
            await Db.StringSetAsync(redisKey, bytes, expiration).WaitAsync(timeoutCts.Token);
            _breaker.RecordSuccess();
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or OperationCanceledException)
        {
            _breaker.RecordFailure();
        }
    }

    public async Task SetManyAsync<T>(IReadOnlyDictionary<string, T> values, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (values == null || values.Count == 0)
        {
            return;
        }

        if (!_breaker.IsAllowed())
        {
            return;
        }
        
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(500));

            var tasks = new List<Task>(values.Count);
            var expiration = expiry ?? TimeSpan.Zero;
            
            foreach (var kvp in values)
            {
                var redisKey = GetFullKey(kvp.Key);
                var bytes = JsonSerializer.SerializeToUtf8Bytes(kvp.Value, _jsonOptions);
                tasks.Add(Db.StringSetAsync(redisKey, bytes, expiration));
            }

            await Task.WhenAll(tasks).WaitAsync(timeoutCts.Token);
            _breaker.RecordSuccess();
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or OperationCanceledException)
        {
            _breaker.RecordFailure();
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        if (!_breaker.IsAllowed())
        {
            return;
        }
        
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(200));

            await Db.KeyDeleteAsync(GetFullKey(key)).WaitAsync(timeoutCts.Token);
            _breaker.RecordSuccess();
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or OperationCanceledException)
        {
            _breaker.RecordFailure();
        }
    }

    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        if (!_breaker.IsAllowed())
        {
            return;
        }
        
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(300));

            var redisKeys = keys.Select(k => (RedisKey)GetFullKey(k)).ToArray();
            await Db.KeyDeleteAsync(redisKeys).WaitAsync(timeoutCts.Token);
            _breaker.RecordSuccess();
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or OperationCanceledException)
        {
            _breaker.RecordFailure();
        }
    }
}