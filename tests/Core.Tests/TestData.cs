using Core.Domain;

namespace Core.Tests;

// Маленький фіксований набір, оголошений у тестах, а не взятий із SampleData:
// якщо хтось змінить SampleData для демо, тести від цього не зламаються.
internal static class TestData
{
    public static readonly DateTimeOffset At = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static Product Cable() => Product.Create("P-1", "Кабель", 100m);
    public static Product Mouse() => Product.Create("P-2", "Миша", 250m);
    public static Product Laptop() => Product.Create("P-3", "Ноутбук", 30_000m);

    // Суми: A 30 000, B 1 000, C 500, D 1 000, E 60 000 (скасоване), F 100 (чернетка).
    // C-2 і C-3 мають однакову виручку 1 000 — на цьому перевіряється другий ключ топ-N.
    public static IReadOnlyList<Order> Orders() =>
    [
        Make("ORD-A", "C-1", OrderStatus.Confirmed, (Laptop(), 1)),
        Make("ORD-B", "C-2", OrderStatus.Confirmed, (Mouse(), 4)),
        Make("ORD-C", "C-1", OrderStatus.Confirmed, (Cable(), 5)),
        Make("ORD-D", "C-3", OrderStatus.Confirmed, (Mouse(), 2), (Cable(), 5)),
        Make("ORD-E", "C-2", OrderStatus.Cancelled, (Laptop(), 2)),
        Make("ORD-F", "C-3", OrderStatus.Draft, (Cable(), 1)),
    ];

    private static Order Make(string id, string customerId, OrderStatus status,
        params (Product Product, int Quantity)[] lines)
    {
        Order order = Order.Create(id, customerId, At);
        foreach ((Product product, int quantity) in lines)
            order.AddLine(product, quantity);

        if (status == OrderStatus.Confirmed)
            order.Confirm();
        else if (status == OrderStatus.Cancelled)
            order.Cancel();

        return order;
    }
}