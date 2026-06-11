using Microsoft.Extensions.Configuration;
using Npgsql;

namespace OrderService.Infrastructure.Persistence.Provider;

public class PostgresConnectionFactory(IConfiguration configuration) : IPostgresConnectionFactory
{
    public NpgsqlConnection GetConnection()
    {
        var postgresConnString = configuration.GetConnectionString("DefaultConnection")
                                 ?? throw new NullReferenceException("Connection string 'DefaultConnection' not found.");
        
        return new NpgsqlConnection(postgresConnString);
    }
}