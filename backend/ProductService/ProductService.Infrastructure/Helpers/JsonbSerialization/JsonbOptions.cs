using System.Text.Json;

namespace ProductService.Infrastructure.Helpers.JsonbSerialization;

public static class JsonbOptions
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };
}