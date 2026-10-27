using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Cli;

// Сценарій лабораторної 5. Отримує вже зібрані об'єкти — сам нічого не створює.
internal static class StoreDemo
{
    public static void Run(OrderService service, IOrderStore store)
    {
        Console.WriteLine($"Сховище: {Describe(store)}");
        Console.WriteLine($"Замовлень на старті: {service.All().Count}");

        Console.WriteLine();
        Console.WriteLine("=== Додати, змінити, знайти ===");

        IReadOnlyList<Product> catalog = SampleData.Products();
        Order created = service.CreateOrder("C-001");
        service.AddLine(created.Id, catalog[2], 1);
        service.AddLine(created.Id, catalog[4], 2);
        service.Confirm(created.Id);

        Order? found = service.Find(created.Id);
        Console.WriteLine($"  Створено і знайдено за id: {found}");

        Console.WriteLine();
        Console.WriteLine("=== Список ===");
        foreach (Order o in service.All().OrderBy(o => o.CreatedAt))
            Console.WriteLine($"  {o.Id,-12} {o.CustomerId,-6} {o.Status,-10} {o.Lines.Count,2} рядк. {o.Total,12:F2}");

        Console.WriteLine();
        Console.WriteLine("=== [дод 2 lab05] Пошук через Func<Order, bool> ===");
        IReadOnlyList<Order> big = service.FindAll(o => o.Status == OrderStatus.Confirmed && o.Total > 10_000m);
        Console.WriteLine($"  Підтверджених на суму > 10 000: {big.Count}");
        foreach (Order o in big)
            Console.WriteLine($"    {o.Id} {o.Total:F2}");

        Console.WriteLine();
        Console.WriteLine("=== Сценарії відмови ===");
        TryDo("неіснуючий id", () => service.Confirm("ORD-NOPE"));
        TryDo("дубль id", () => store.Add(created));
        TryDo("рядок у підтверджене", () => service.AddLine(created.Id, catalog[0], 1));
        // Відкриваємо чернетки, доки не спрацює правило з лабораторної 4,
        // яке тепер викликає сервіс (лише він бачить усі замовлення клієнта).
        TryDo("ліміт чернеток клієнта", () =>
        {
            for (int i = 0; i <= CustomerOrderPolicy.MaxDraftOrders; i++)
                service.CreateOrder("C-900");
        });

        if (store is CachingOrderStore caching)
        {
            Console.WriteLine();
            Console.WriteLine("=== [дод 1] Декоратор ===");
            service.All();
            service.All();
            service.All();
            Console.WriteLine($"  List() запитано {caching.Reads} разів, до {caching.Inner.GetType().Name} дійшло {caching.InnerReads}");
        }

        IOrderStore real = store is CachingOrderStore wrapper ? wrapper.Inner : store;
        if (real is FileOrderStore file)
            Console.WriteLine($"\nФайл даних: {file.FilePath}");
    }

    private static string Describe(IOrderStore store) => store switch
    {
        CachingOrderStore c => $"CachingOrderStore → {c.Inner.GetType().Name}",
        _ => store.GetType().Name
    };

    private static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  {title}: виняток НЕ спрацював!");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Console.WriteLine($"  {title}: {ex.Message}");
        }
    }
}