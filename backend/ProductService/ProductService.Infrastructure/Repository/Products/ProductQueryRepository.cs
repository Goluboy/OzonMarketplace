using System.Globalization;
using Dapper;
using ProductService.Infrastructure.Abstractions.DTO.Product.Query;
using ProductService.Infrastructure.Abstractions.Repository.Abstractions.Products;
using ProductService.Infrastructure.DAO;
using ProductService.Infrastructure.Helpers;
using ProductService.Infrastructure.Persistence.Provider;

namespace ProductService.Infrastructure.Repository.Products;

public class ProductQueryRepository(IPostgresConnectionFactory connectionFactory) : IProductQueryRepository
{
    public async Task<IReadOnlyList<ProductCardDto>> GetCardsAsync(long sku)
    {
        await using var connection = connectionFactory.GetConnection();
        
        const string sql = """
                           SELECT id, seller_id, category_id, name, price_amount, price_currency, COALESCE(images->0->>'url', '') as main_image_url 
                           FROM products 
                           WHERE sku = @Sku;
                           """;
        
        var productCards = await connection.QueryAsync<ProductCardDto>(sql, new { Sku = sku });
        
        return productCards.ToList();
    }
    
    public async Task<IReadOnlyList<ProductCardDto>> GetCardsAsync(IReadOnlyList<Guid> ids)
    {
        await using var connection = connectionFactory.GetConnection();
        
        const string sql = """
                           SELECT id, seller_id, category_id, name, price_amount, price_currency, COALESCE(images->0->>'url', '') as main_image_url 
                           FROM products 
                           WHERE id = ANY(@Ids);
                           """;
        
        var productCards = await connection.QueryAsync<ProductCardDto>(sql, new { Ids = ids });
        
        return productCards.ToList();
    }

    public async Task<ProductCardDto?> GetCardAsync(Guid id)
    {
        await using var connection = connectionFactory.GetConnection();
        
        const string sql = """
                           SELECT id, seller_id, category_id, name, price_amount, price_currency, COALESCE(images->0->>'url', '') as main_image_url 
                           FROM products 
                           WHERE id = @Id;
                           """;
        
        var productCard = await connection.QueryFirstOrDefaultAsync<ProductCardDto>(sql, new { Id = id });
        
        return productCard;
    }

    public async Task<ProductDetailsDto?> GetDetailsAsync(Guid id)
    {
        await using var connection = connectionFactory.GetConnection();
        
        const string sql = """
                           SELECT p.id, p.sku, p.seller_id, p.name, p.description, 
                                  p.price_amount, p.price_currency, 
                                  p.category_id, p.images, p.created_at, p.updated_at,
                                  c.name as CategoryName, c.path as CategoryPath
                           FROM products p
                           LEFT JOIN categories c ON p.category_id = c.id
                           WHERE p.id = @Id;
                           """;
        
        var productDto = await connection.QueryFirstOrDefaultAsync<ProductDetailsDto>(sql, new { Id = id });

        return productDto;
    }

    public async Task<ProductPagedIdsDto> GetPagedAsync(ProductSearchFilter filter)
    {
        var sortColumn = filter.SortBy.ToLowerInvariant() switch
        {
            "price" => "price_amount",
            "name" => "name",
            _ => "created_at"
        };
        
        var direction = filter.SortOrder.Equals("asc", StringComparison.InvariantCultureIgnoreCase) ? "ASC" : "DESC";
        var compOperator = direction == "ASC" ? ">" : "<";

        var builder = new SqlBuilder();
        
        var template = builder.AddTemplate("""
                                            SELECT id, /**select**/
                                            FROM products
                                            /**where**/
                                            /**orderby**/
                                            LIMIT @Limit;
                                            """);

        if (sortColumn == "created_at")
        {
            builder.Select("to_char(created_at, 'YYYY-MM-DD\"T\"HH24:MI:SS.US\"Z\"') as SortValue");
        }
        else
        {
            builder.Select($"{sortColumn}::text as SortValue");
        }

        if (filter.CategoryId.HasValue)
        {
            builder.Where("category_id = @CategoryId", new { CategoryId = filter.CategoryId.Value });
        }

        var targetCurrency = filter.MinPrice?.Currency ?? filter.MaxPrice?.Currency;

        if (!string.IsNullOrEmpty(targetCurrency))
        {
            builder.Where("price_currency = @PriceCurrency", new { PriceCurrency = targetCurrency.ToUpperInvariant() });
        }
        
        if (filter.MinPrice != null)
        {
            builder.Where("price_amount >= @MinPriceAmount", new { MinPriceAmount = filter.MinPrice.Amount });
        }

        if (filter.MaxPrice != null)
        {
            builder.Where("price_amount <= @MaxPriceAmount", new { MaxPriceAmount = filter.MaxPrice.Amount });
        }

        if (!string.IsNullOrEmpty(filter.Search))
        {
            builder.Where("search_vector @@ to_tsquery('russian', @Search)",
                new { Search = $"{filter.Search.Trim()}:*"});
        }
        
        var decodedCursor = CursorHelper.Decode(filter.Cursor);
        if (decodedCursor != null)
        {
            var cursorSql = $"""
                             ({sortColumn} {compOperator} @LastSortValue
                             OR ({sortColumn} = @LastSortValue AND id {compOperator} @LastId))
                             """;
            
            object lastSortValue = sortColumn switch
            {
                "price_amount" => decimal.TryParse(decodedCursor.Value.SortValue, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var priceAmount)
                    ? priceAmount
                    : throw new FormatException("Incorrect sort value in the pagination cursor."),

                "created_at" => DateTimeOffset.TryParse(decodedCursor.Value.SortValue, out var date)
                    ? date.ToUniversalTime()
                    : throw new FormatException("Incorrect sort value in the pagination cursor."),

                _ => decodedCursor.Value.SortValue
            };
                
            builder.Where(cursorSql, new { LastSortValue = lastSortValue, LastId = decodedCursor.Value.Id });
        }

        builder.OrderBy($"{sortColumn} {direction}");
        builder.OrderBy($"id {direction}");

        var parameters = new DynamicParameters(template.Parameters);
        parameters.Add("Limit", filter.PageSize + 1);

        await using var connection = connectionFactory.GetConnection();

        var response = (await connection.QueryAsync<ProductPagedRowModel>(template.RawSql, parameters)).ToList();

        var hasNextPage = response.Count > filter.PageSize;
        if (hasNextPage)
        {
            response.RemoveAt(response.Count - 1);
        }

        var productIds = response.Select(r => r.Id).ToList();

        string? nextCursor = null;
        if (hasNextPage && response.Count != 0)
        {
            var lastRow = response.Last();
            
            nextCursor = CursorHelper.Encode(lastRow.SortValue, lastRow.Id);
        }
        
        return new ProductPagedIdsDto(productIds, nextCursor);
    }
}