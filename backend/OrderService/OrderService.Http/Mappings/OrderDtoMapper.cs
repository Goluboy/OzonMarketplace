using System.Globalization;
using System.Text.RegularExpressions;
using OrderService.Http.Dtos;
using OrderService.UseCases.Queries.Models;

namespace OrderService.Http.Mappings;

public static class OrderDtoMapper
{
    private static readonly Regex AmountPattern = new(@"^\d+\.\d{2}$", RegexOptions.Compiled);
    private static readonly Regex CurrencyPattern = new(@"^[A-Z]{3}$", RegexOptions.Compiled);

    public static OrderDto ToDto(this OrderModel order) =>
        new(
            order.Id,
            order.Status,
            order.CreatedAt,
            order.UpdatedAt,
            order.CustomerName,
            order.CustomerEmail,
            order.DeliveryAddress,
            order.TotalAmount.ToMoneyDto(),
            order.Items.Select(i => new OrderItemDto(
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.Price.ToMoneyDto())).ToList());

    public static MoneyDto ToMoneyDto(this decimal amount, string currency = "RUB") =>
        new(FormatAmount(amount), currency);

    private static string FormatAmount(decimal amount) =>
        amount.ToString("F2", CultureInfo.InvariantCulture);

    public static bool IsValidMoneyFormat(MoneyDto money) =>
        AmountPattern.IsMatch(money.Amount) && CurrencyPattern.IsMatch(money.Currency);
}
