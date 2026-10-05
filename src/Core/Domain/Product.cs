using Core.Dto;

namespace Core.Domain;

// Товар каталогу: та сама назва, що й у ProductDto, але різні задачі.
// DTO переносить дані, сутність захищає правила.
public sealed class Product
{
    public string Id { get; }
    public string Name { get; }
    public decimal Price { get; private set; }

    private Product(string id, string name, decimal price)
    {
        Id = id;
        Name = name;
        Price = price;
    }

    public static Product Create(string id, string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва не може бути порожньою", nameof(name));
        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), price,
                "Ціна товару має бути більшою за нуль");

        return new Product(id.Trim().ToUpperInvariant(), name.Trim(), price);
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), newPrice,
                "Ціна товару має бути більшою за нуль");

        Price = newPrice;
    }

    public ProductDto ToDto() => new(Id, Name, Price);

    // Через Create, а не через приватний конструктор: пошкоджений файл
    // не повинен створити некоректну сутність.
    public static Product FromDto(ProductDto dto) => Create(dto.Id, dto.Name, dto.Price);

    public override string ToString() => $"{Id} {Name} — {Price:F2}";
}