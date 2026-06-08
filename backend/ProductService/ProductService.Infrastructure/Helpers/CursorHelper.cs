using System.Text;

namespace ProductService.Infrastructure.Helpers;

public static class CursorHelper
{
    public static string Encode(object sortValue, Guid id)
    {
        var rawCursor = $"{sortValue}|{id}";
        
        var bytes = Encoding.UTF8.GetBytes(rawCursor);
        return Convert.ToBase64String(bytes);
    }
    
    public static (string SortValue, Guid Id)? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var rawCursor = Encoding.UTF8.GetString(bytes);
            
            var delimiterIndex = rawCursor.LastIndexOf('|');
            if (delimiterIndex == -1)
            {
                return null;
            }

            var sortValue = rawCursor.Substring(0, delimiterIndex);
            var idStr = rawCursor.Substring(delimiterIndex + 1);

            if (Guid.TryParse(idStr, out var id))
            {
                return (sortValue, id);
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}