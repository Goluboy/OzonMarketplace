namespace OrderService.Domain.ValueObjects;

public sealed record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !IsValidFormat(value))
        {
            throw new ArgumentException("Invalid email");
        }
        Value = value.Trim().ToLowerInvariant();
    }

    private static bool IsValidFormat(string email)
    {
        return email.Contains('@') && email.IndexOf('@') > 0 && email.LastIndexOf('.') > email.IndexOf('@') + 1;
    }

    public override string ToString() => Value;
    public static implicit operator string(Email email) => email.Value;
    public static explicit operator Email(string value) => new(value);
}
