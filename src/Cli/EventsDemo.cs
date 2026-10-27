using Cli.Notifications;
using Core;
using Core.Domain;
using Core.Events;
using Core.Services;

namespace Cli;

// Сценарій лабораторної 6: два підписники, відмова без події, відписка, Func-пошук.
internal static class EventsDemo
{
    public static void Run(OrderService service, string logPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        IReadOnlyList<Product> catalog = SampleData.Products();

        // Підписник 1: лямбда із замиканням над локальною змінною changes.
        int changes = 0;
        service.Changed += (_, _) => changes++;

        // Підписник 2: локальна функція. Має ім'я, тому її можна зняти через -=.
        // Номер #N береться з changes: підписник 1 стоїть раніше і вже його збільшив.
        void OnOrderChanged(object? sender, OrderChangedEventArgs e)
        {
            string line = $"#{changes,-2} {OrderLogFormat.Line(e)}";
            Console.WriteLine(line);
            try
            {
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch (IOException ex)
            {
                // Виняток у підписнику не повинен зламати бізнес-операцію.
                Console.WriteLine($"  (лог не записано: {ex.Message})");
            }
        }

        service.Changed += OnOrderChanged;

        Console.WriteLine("=== Два підписники ===");
        Order order = service.CreateOrder("C-010");
        service.AddLine(order.Id, catalog[0], 1);
        service.AddLine(order.Id, catalog[1], 2);
        Console.WriteLine($"Подій отримано: {changes}");

        Console.WriteLine();
        Console.WriteLine("=== Відмова: виняток є, події немає ===");
        TryDo("нульова кількість", () => service.AddLine(order.Id, catalog[2], 0));
        TryDo("неіснуючий id", () => service.Confirm("ORD-NOPE"));
        Console.WriteLine($"Подій отримано: {changes}");

        Console.WriteLine();
        service.Confirm(order.Id);
        Console.WriteLine($"Подій отримано: {changes}");

        Console.WriteLine();
        Console.WriteLine("=== Після -= : операції виконуються мовчки ===");
        service.Changed -= OnOrderChanged;

        Order second = service.CreateOrder("C-011");
        service.AddLine(second.Id, catalog[2], 1);
        service.Cancel(second.Id);
        Console.WriteLine($"  Стан у сховищі: {service.Find(second.Id)}");
        Console.WriteLine($"Подій отримано: {changes}  (+3 лише від лямбди-лічильника)");

        Console.WriteLine();
        Console.WriteLine("=== Func замість події: потрібне значення у відповідь ===");
        IReadOnlyList<Order> drafts = service.FindAll(o => o.Status == OrderStatus.Draft);
        Console.WriteLine($"  Незавершених замовлень: {drafts.Count}");
        foreach (Order o in drafts)
            Console.WriteLine($"    {o}");

        Console.WriteLine();
        Console.WriteLine($"Журнал: {logPath}");
    }

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