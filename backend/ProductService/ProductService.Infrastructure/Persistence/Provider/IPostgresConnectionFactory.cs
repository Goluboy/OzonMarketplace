using Npgsql;

namespace ProductService.Infrastructure.Persistence.Provider;

public interface IPostgresConnectionFactory
{
    NpgsqlConnection GetConnection();
}