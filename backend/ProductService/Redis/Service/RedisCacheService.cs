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

    public RedisCacheService(IRedisConnectionFactory connectionFactory, IOptions<RedisOptions> options, JsonSerializerOptions? jsonOptions)
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
        
        var value = await Db.StringGetAsync(GetFullKey(key));
        if (value.IsNullOrEmpty)
        {
            return default;
        }
        
        ReadOnlyMemory<byte> bytes = value;
        return JsonSerializer.Deserialize<T>(bytes.Span, _jsonOptions);
    }

    public async Task<IReadOnlyList<T?>> GetManyAsync<T>(IReadOnlyList<string> keys, CancellationToken ct = default)
    {
        if (keys == null || keys.Count == 0)
        {
            return [];
        }

        var redisKeys = keys.Select(k => (RedisKey)GetFullKey(k)).ToArray();
        var values = await Db.StringGetAsync(redisKeys);
        
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

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Ключ не может быть пустым.", nameof(key));
        }
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value), "Запись null-значений в кэш не разрешена.");
        }
        
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
        var redisKey = GetFullKey(key);
        var expiration = expiry ?? TimeSpan.Zero;
        
        await Db.StringSetAsync(redisKey, bytes, expiration);
    }

    public async Task SetManyAsync<T>(IReadOnlyDictionary<string, T> values, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (values == null || values.Count == 0)
        {
            return;
        }

        var tasks = new List<Task>(values.Count);

        var expiration = expiry ?? TimeSpan.Zero;
        
        foreach (var kvp in values)
        {
            var redisKey = GetFullKey(kvp.Key);
            
            var bytes = JsonSerializer.SerializeToUtf8Bytes(kvp.Value, _jsonOptions);

            tasks.Add(Db.StringSetAsync(redisKey, bytes, expiration));
        }

        await Task.WhenAll(tasks);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await Db.KeyDeleteAsync(GetFullKey(key));
    }

    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        var redisKeys = keys.Select(k => (RedisKey)GetFullKey(k)).ToArray();
        await Db.KeyDeleteAsync(redisKeys);
    }
}