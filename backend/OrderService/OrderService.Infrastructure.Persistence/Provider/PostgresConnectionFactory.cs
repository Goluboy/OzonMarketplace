using Microsoft.Extensions.Configuration;
using Npgsql;

namespace OrderService.Infrastructure.Persistence.Provider;

public class PostgresConnectionFactory(IConfiguration configuration) : IPostgresConnectionFactory
{
    public NpgsqlConnection GetConnection()
    {
        var postgresConnString = configuration.GetConnectionString("PostgresConnectionString")
                                 ?? throw new NullReferenceException("Connection string 'PostgresConnectionString' not found.");
        
        return new NpgsqlConnection(postgresConnString);
    }
}