using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace OrderService.UseCases.Commands;

public static class CommandsDependencyInjection
{
    public static IServiceCollection AddCommands(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var handlerTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("CommandHandler") && !t.IsAbstract && !t.IsInterface);

        foreach (var type in handlerTypes)
        {
            // Find the ICommandHandler<TCommand, TResponse> interface
            var interfaceType = type.GetInterfaces()
                .FirstOrDefault(i => i.Name.Contains("ICommandHandler"));

            if (interfaceType != null)
            {
                services.AddScoped(interfaceType, type);
            }
        }

        return services;
    }
}
