namespace ProductService.Domain.ValueObjects;

public record ProductImage
{
    public string Url { get; init; }

    public ProductImage(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL не может быть пустым.");
        }
        
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult) || 
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Некорректный формат URL.");
        }

        Url = url;
    }
}