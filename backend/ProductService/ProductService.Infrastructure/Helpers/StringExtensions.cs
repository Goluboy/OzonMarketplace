using System.Text.Json;

namespace ProductService.Infrastructure.Helpers;

public static class StringExtensions
{
    public static string ToSnakeCase(this string text)
    {
        return string.IsNullOrEmpty(text) 
            ? 
            text 
            : JsonNamingPolicy.SnakeCaseLower.ConvertName(text);
    }
}