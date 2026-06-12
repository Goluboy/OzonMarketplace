using Microsoft.Extensions.DependencyInjection;
using Redis.Provider;
using Redis.Service;

namespace Redis;

public static class RedisExtensions
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services, Action<RedisOptions> options)
    {
        services.Configure(options);
        
        services.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();
        services.AddSingleton<ICacheService, RedisCacheService>();
        
        return services;
    }
}