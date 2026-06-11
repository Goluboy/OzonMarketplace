using StackExchange.Redis;

namespace Redis.Provider;

internal interface IRedisConnectionFactory
{
    IDatabase GetDatabase();
}