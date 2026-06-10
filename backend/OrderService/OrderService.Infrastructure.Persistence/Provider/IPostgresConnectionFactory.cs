using Npgsql;

namespace OrderService.Infrastructure.Persistence.Provider;

public interface IPostgresConnectionFactory
{
    NpgsqlConnection GetConnection();
}