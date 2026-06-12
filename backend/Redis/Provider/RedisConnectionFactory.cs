using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Redis.Provider;

internal sealed class RedisConnectionFactory : IRedisConnectionFactory, IDisposable
{
    private readonly ConnectionMultiplexer[] _pool;
    private readonly int _poolSize;
    
    public RedisConnectionFactory(IOptions<RedisOptions> options)
    {
        var opt = options.Value;
        _poolSize = opt.ConnectionPoolSize > 0 ? opt.ConnectionPoolSize : 1;
        _pool = new ConnectionMultiplexer[_poolSize];

        var config = ConfigurationOptions.Parse(opt.ConnectionString);
        
        config.AsyncTimeout = opt.AsyncTimeout;
        config.SyncTimeout = opt.SyncTimeout;
        config.KeepAlive = opt.KeepAlive;
        config.AbortOnConnectFail = opt.AbortOnConnectFail;

        for (var i = 0; i < _poolSize; i++)
        {
            _pool[i] = ConnectionMultiplexer.Connect(config);
        }
    }
    
    public IDatabase GetDatabase()
    {
        if (_poolSize == 1)
        {
            return _pool[0].GetDatabase();
        }

        var leastLoaded = _pool[0];
        var minOutstanding = leastLoaded.GetCounters().TotalOutstanding;

        for (var i = 1; i < _poolSize; i++)
        {
            var outstanding = _pool[i].GetCounters().TotalOutstanding;
            if (outstanding < minOutstanding)
            {
                minOutstanding = outstanding;
                leastLoaded = _pool[i];
            }
        }

        return leastLoaded.GetDatabase();
    }

    public void Dispose()
    {
        foreach (var multiplexer in _pool)
        {
            multiplexer.Dispose();
        }
    }
}