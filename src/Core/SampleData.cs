using Core.Domain;

namespace Core;

// Зразкові дані для демо і для LINQ-звітів тижня 7.
// Усе створюється через фабрики й методи сутностей — ті самі інваріанти, що й завжди.
public static class SampleData
{
    private static readonly DateTimeOffset BaseDate = new(2026, 9, 1, 10, 0, 0, TimeSpan.FromHours(3));

    public static IReadOnlyList<Product> Products() =>
    [
        Product.Create("P-001", "Ноутбук 15.6\"", 32999.00m),
        Product.Create("P-002", "Миша бездротова", 649.50m),
        Product.Create("P-003", "Клавіатура механічна", 2450.00m),
        Product.Create("P-004", "Монітор 27\" IPS", 8999.99m),
        Product.Create("P-005", "Навушники бездротові", 1899.00m),
        Product.Create("P-006", "USB-хаб 4 порти", 399.90m),
        Product.Create("P-007", "Веб-камера Full HD", 1250.00m),
        Product.Create("P-008", "Килимок для миші XL", 299.00m),
        Product.Create("P-009", "Зарядний пристрій 65W", 1150.00m),
        Product.Create("P-010", "Кабель HDMI 2м", 189.50m),
    ];

    public static IReadOnlyList<Order> Orders()
    {
        IReadOnlyList<Product> p = Products();

        return
        [
            Make("ORD-S001", "C-001", 0,  OrderStatus.Confirmed, (p[0], 1), (p[1], 1)),
            Make("ORD-S002", "C-002", 1,  OrderStatus.Confirmed, (p[3], 2)),
            Make("ORD-S003", "C-003", 2,  OrderStatus.Confirmed, (p[4], 1), (p[7], 2)),
            Make("ORD-S004", "C-001", 3,  OrderStatus.Cancelled, (p[5], 3), (p[9], 4)),
            Make("ORD-S005", "C-005", 4,  OrderStatus.Confirmed, (p[2], 1)),
            Make("ORD-S006", "C-002", 6,  OrderStatus.Draft,     (p[6], 1), (p[8], 1)),
            Make("ORD-S007", "C-003", 7,  OrderStatus.Confirmed, (p[0], 2)),
            Make("ORD-S008", "C-006", 9,  OrderStatus.Confirmed, (p[1], 5), (p[7], 5)),
            Make("ORD-S009", "C-004", 10, OrderStatus.Draft,     (p[3], 1)),
            Make("ORD-S010", "C-004", 11, OrderStatus.Draft,     (p[4], 2)),
            Make("ORD-S011", "C-004", 12, OrderStatus.Draft),
            Make("ORD-S012", "C-005", 14, OrderStatus.Cancelled, (p[9], 10)),
            Make("ORD-S013", "C-006", 15, OrderStatus.Confirmed, (p[2], 1), (p[1], 1), (p[7], 1)),
            Make("ORD-S014", "C-001", 18, OrderStatus.Confirmed, (p[8], 2)),
            Make("ORD-S015", "C-003", 20, OrderStatus.Draft,     (p[5], 1)),
            Make("ORD-S016", "C-002", 22, OrderStatus.Confirmed, (p[0], 1), (p[3], 1), (p[4], 1)),
        ];
    }

    private static Order Make(string id, string customerId, int day, OrderStatus status,
        params (Product Product, int Quantity)[] lines)
    {
        Order order = Order.Create(id, customerId, BaseDate.AddDays(day));
        foreach ((Product product, int quantity) in lines)
            order.AddLine(product, quantity);

        switch (status)
        {
            case OrderStatus.Confirmed:
                order.Confirm();
                break;
            case OrderStatus.Cancelled:
                order.Cancel();
                break;
        }

        return order;
    }
}