namespace OrderService.Http.Dtos;

public record MoneyDto(string Amount, string Currency = "RUB");
