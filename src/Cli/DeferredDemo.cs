using Core;
using Core.Domain;

namespace Cli;

// Демонстрація відкладеного виконання LINQ.
internal static class DeferredDemo
{
    public static void Run(IReadOnlyList<Order> all)
    {
        Console.WriteLine("=== 1. Запит бачить зміни джерела до матеріалізації ===");

        // Локальний список: service.All() щоразу повертає новий знімок,
        // тому для демо потрібне джерело, яке ми самі змінюємо.
        List<Order> source = all.ToList();
        IEnumerable<Order> query = source.Where(o => o.Total > 10_000m); // побудовано, не виконано

        Console.WriteLine($"  Дорожчих за 10 000 до змін:    {query.Count()}");

        Order big = Order.Create("ORD-TMP01", "C-999", DateTimeOffset.Now);
        big.AddLine(SampleData.Products()[0], 1);
        source.Add(big);
        Console.WriteLine($"  Дорожчих за 10 000 після змін: {query.Count()}");

        List<Order> snapshot = query.ToList();                           // результат зафіксовано
        source.Clear();
        Console.WriteLine($"  Запит після Clear():           {query.Count()}");
        Console.WriteLine($"  У знімку лишилось:             {snapshot.Count}");

        Console.WriteLine();
        Console.WriteLine("=== 2. Коли саме виконується предикат ===");

        int calls = 0;
        IEnumerable<Order> counted = all.Where(o => { calls++; return o.Total > 10_000m; });
        Console.WriteLine($"  Після оголошення запиту викликів: {calls}");

        _ = counted.Count();
        Console.WriteLine($"  Після першого Count():            {calls}");

        _ = counted.Count();
        Console.WriteLine($"  Після другого Count():            {calls}   ← запит виконано вдруге");

        List<Order> once = counted.ToList();
        int before = calls;
        _ = once.Count;
        Console.WriteLine($"  Властивість Count у списку:       +{calls - before} викликів");
    }
}