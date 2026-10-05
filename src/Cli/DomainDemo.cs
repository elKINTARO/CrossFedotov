using Core.Domain;
using Core.Dto;
using Core.Import;

namespace Cli;

// Демонстрація доменної моделі: успішний сценарій і порушення інваріантів.
internal static class DomainDemo
{
    public static void Run(string catalogPath)
    {
        // [дод 1] Каталог будується з імпорту тижня 3 через доменні інваріанти.
        ImportResult<Product> catalog = CatalogBuilder.FromImport(ProductCsvImporter.Load(catalogPath));
        Console.WriteLine($"Каталог: товарів {catalog.Items.Count}, відхилено рядків {catalog.Errors.Count}");

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 1: успіх ===");

        var createdAt = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.FromHours(3));
        Order order = Order.Create("ord-001", "c-001", createdAt);
        Console.WriteLine(order);

        order.AddLine(catalog.Items[0], 2);
        order.AddLine(catalog.Items[1], 1);
        Console.WriteLine(order);
        foreach (OrderLine line in order.Lines)
            Console.WriteLine($"    {line}");

        order.Confirm();
        Console.WriteLine(order);

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");

        TryDo("рядок у підтверджене замовлення", () => order.AddLine("P-001", "Ноутбук", 100m, 1));
        TryDo("повторне підтвердження", () => order.Confirm());
        TryDo("замовлення без клієнта", () => Order.Create("ORD-002", "   ", createdAt));
        TryDo("нульова кількість", () => Order.Create("ORD-003", "C-002", createdAt).AddLine("P-001", "Ноутбук", 100m, 0));
        TryDo("підтвердження порожнього", () => Order.Create("ORD-004", "C-002", createdAt).Confirm());
        TryDo("від'ємна ціна товару", () => Product.Create("P-900", "Дивний товар", -5m));

        Console.WriteLine();
        Console.WriteLine($"Стан після всіх відмов не змінився: {order}");

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 3: [дод 3] переходи статусів ===");

        Order cancelled = Order.Create("ORD-005", "C-003", createdAt);
        cancelled.AddLine(catalog.Items[0], 1);
        cancelled.Cancel();
        Console.WriteLine($"  {cancelled}");
        TryDo("підтвердити скасоване", () => cancelled.Confirm());
        TryDo("скасувати скасоване", () => cancelled.Cancel());

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 4: [дод 2] правило двох сутностей ===");

        var customerOrders = new List<Order>();
        for (int i = 1; i <= CustomerOrderPolicy.MaxDraftOrders; i++)
        {
            CustomerOrderPolicy.EnsureCanOpenNewOrder("C-100", customerOrders);
            customerOrders.Add(Order.Create($"ORD-10{i}", "C-100", createdAt));
        }
        Console.WriteLine($"  Відкрито чернеток: {customerOrders.Count}");
        TryDo("четверта чернетка", () => CustomerOrderPolicy.EnsureCanOpenNewOrder("C-100", customerOrders));

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 5: ToDto / FromDto ===");

        OrderDto dto = order.ToDto();
        Order restored = Order.FromDto(dto);
        Console.WriteLine($"  DTO     : {dto.Id}, статус {dto.Status}, рядків {dto.Lines.Count}");
        Console.WriteLine($"  Відновлено: {restored}");
        Console.WriteLine($"  Суми збігаються: {order.Total == restored.Total}");

        TryDo("DTO з нульовою кількістю", () => Order.FromDto(dto with
        {
            Lines = [new OrderLineDto("P-001", "Ноутбук", 100m, 0)]
        }));
    }

    private static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
        }
    }
}