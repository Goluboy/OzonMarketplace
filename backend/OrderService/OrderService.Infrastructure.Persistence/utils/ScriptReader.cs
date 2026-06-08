using System.Reflection;

namespace OrderService.Infrastructure.Persistence.utils;

public static class ScriptReader
{
    public static string ReadEmbeddedScript(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"OrderService.Infrastructure.Persistence.Migrations.Scripts.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            throw new FileNotFoundException(
                $"Embedded resource '{resourceName}' not found. " +
                $"Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}