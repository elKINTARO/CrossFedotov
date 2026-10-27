using Core;
using Core.Domain;
using Core.Services;

namespace Cli;

// [дод 1] Той самий сервіс, але одержувачі передані через конструктор, а не підписані подією.
internal static class SinkDemo
{
    public static void Run(OrderService service)
    {
        Console.WriteLine("=== [дод 1] INotificationSink: ConsoleSink + FileSink через конструктор ===");
        IReadOnlyList<Product> catalog = SampleData.Products();

        Order order = service.CreateOrder("C-012");
        service.AddLine(order.Id, catalog[3], 1);
        service.Confirm(order.Id);

        Console.WriteLine();
        Console.WriteLine("Відписатися під час роботи неможливо: склад одержувачів зафіксовано при створенні сервісу.");
    }
}