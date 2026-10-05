namespace Core.Domain;

// Рядок замовлення. Створюється лише через Create, тому некоректного рядка не існує.
public sealed class OrderLine
{
    public string ProductId { get; }
    public string Name { get; }
    public decimal Price { get; }
    public int Quantity { get; }

    public decimal Sum => Price * Quantity;

    private OrderLine(string productId, string name, decimal price, int quantity)
    {
        ProductId = productId;
        Name = name;
        Price = price;
        Quantity = quantity;
    }

    public static OrderLine Create(string productId, string name, decimal price, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Ідентифікатор товару обов'язковий", nameof(productId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва товару обов'язкова", nameof(name));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), price,
                "Ціна в рядку не може бути від'ємною");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity,
                "Кількість у рядку має бути більшою за нуль");

        return new OrderLine(productId.Trim(), name.Trim(), price, quantity);
    }

    public override string ToString() => $"{Name} × {Quantity} = {Sum:F2}";
}