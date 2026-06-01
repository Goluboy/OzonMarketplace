using System.Data;
using System.Text.Json;
using Dapper;

namespace ProductService.Infrastructure.Helpers.JsonbSerialization;

public class JsonbTypeHandler<T> : SqlMapper.TypeHandler<T> where T : class
{
    public override void SetValue(IDbDataParameter parameter, T? value)
    {
        parameter.Value = value == null 
            ? DBNull.Value 
            : JsonSerializer.Serialize(value, JsonbOptions.Options);
        parameter.DbType = DbType.String;
    }

    public override T? Parse(object value)
    {
        if (value is DBNull)
        {
            return null;
        }

        var json = value.ToString();
        return string.IsNullOrEmpty(json) 
            ? null 
            : JsonSerializer.Deserialize<T>(json, JsonbOptions.Options);
    }
}