using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace OrderService.UseCases.Queries;

public static class QueriesDependencyInjection
{
    public static IServiceCollection AddQueries(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var handlerTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Handler") && !t.IsAbstract && !t.IsInterface);

        foreach (var type in handlerTypes)
        {
            var interfaceType = type.GetInterfaces()
                .FirstOrDefault(i => i.Name.Contains("IQueryHandler"));

            if (interfaceType != null)
            {
                services.AddScoped(interfaceType, type);
            }
        }

        return services;
    }
}
