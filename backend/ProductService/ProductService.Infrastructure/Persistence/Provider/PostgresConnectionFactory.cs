using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ProductService.Infrastructure.Persistence.Provider;

public class PostgresConnectionFactory : IPostgresConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    static PostgresConnectionFactory()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public PostgresConnectionFactory(IConfiguration configuration)
    {
        var postgresConnString = configuration.GetConnectionString("PostgresConnectionString")
                                 ?? throw new NullReferenceException("Connection string 'PostgresConnectionString' not found.");
        
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(postgresConnString);

        dataSourceBuilder.EnableDynamicJson();
        
        _dataSource = dataSourceBuilder.Build();
    }
    
    
    public NpgsqlConnection GetConnection()
    {
        return _dataSource.CreateConnection();
    }
}