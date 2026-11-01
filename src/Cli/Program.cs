using System.Text;
using Cli;
using Core.Abstractions;
using Core.Dto;
using Core.Import;
using Core.Services;
using Cli.Notifications;
using Core;
using Core.Domain;
using Core.Reports;

Console.OutputEncoding = Encoding.UTF8;

// lab01-02: інформація про середовище
if (args is ["--env", ..])
{
    EnvironmentView.Print(asJson: args.Contains("--json"));
    return 0;
}

// lab04: доменна модель
if (args is ["--domain", ..])
{
    string catalogPath = args.Length > 1 ? args[1] : Path.Combine("data", "sample.csv");
    if (!File.Exists(catalogPath))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(catalogPath)}");
        return 1;
    }

    DomainDemo.Run(catalogPath);
    return 0;
}

// lab03 [дод 2]: товари і клієнти в одному файлі
if (args is ["--mixed", var mixedPath])
{
    if (!File.Exists(mixedPath))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(mixedPath)}");
        return 1;
    }

    MixedImportResult mixed = MixedCsvImporter.Load(mixedPath);
    Console.WriteLine($"Товарів: {mixed.Products.Count}, клієнтів: {mixed.Customers.Count}");
    foreach (ProductDto p in mixed.Products)
        Console.WriteLine($"  {p.Id,-6} {p.Name,-26} {p.Price,10:F2} грн");
    foreach (CustomerDto c in mixed.Customers)
        Console.WriteLine($"  {c.Id,-6} {c.FullName,-20} {c.Email,-22} {c.Phone ?? "—"}");
    foreach (string e in mixed.Errors)
        Console.WriteLine($"  ! {e}");
    return 0;
}

// lab03: імпорт товарів з CSV/JSON
if (args is ["--import", ..])
{
    string path = args.Length > 1 ? args[1] : Path.Combine("data", "sample.csv");
    if (!File.Exists(path))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
        return 1;
    }

    // lab06 [дод 2]: Action<string> — імпортер повідомляє про пропущені рядки в момент читання.
    Action<string> reportSkipped = message => Console.WriteLine($"  ! пропущено {message}");

    Console.WriteLine($"Читання {path}...");
    ImportResult<ProductDto>? result = Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".csv" => ProductCsvImporter.Load(path, reportSkipped),
        ".json" => ProductJsonImporter.Load(path, reportSkipped),
        _ => null
    };

    if (result is null)
    {
        Console.WriteLine($"Непідтримуваний формат: {Path.GetExtension(path)}");
        return 2;
    }

    Console.WriteLine($"Завантажено записів: {result.Items.Count}");
    foreach (ProductDto p in result.Items.Take(10))
        Console.WriteLine($"  {p.Id,-6} {p.Name,-26} {p.Price,10:F2} грн");

    // Помилки вже надрукував reportSkipped, тут лише підсумок.
    Console.WriteLine($"Усього {result.Total} | прийнято {result.Items.Count} | пропущено {result.Errors.Count} | помилок {result.ErrorRate:P0}");
    return 0;
}

// ===== COMPOSITION ROOT (lab05-07) =====
// Єдине місце, де створюються конкретні класи і зшиваються залежності.
// Сховище: --file, --cache. Сценарій: --events, --sink (lab06), report-* (lab07), без аргументів — lab05.
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");
string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");

IOrderStore store = StoreFactory.Create(args, dataPath);

// lab07: перший аргумент без "--" — назва звіту. Прапорці (--file тощо) назвою не вважаються.
string? command = args.FirstOrDefault(a => !a.StartsWith("--"));

try
{
    if (command is not null)
        return RunReport(command, new OrderService(store), empty: args.Contains("--empty"));

    if (args.Contains("--events"))
    {
        EventsDemo.Run(new OrderService(store), logPath);
    }
    else if (args.Contains("--sink"))
    {
        INotificationSink[] sinks = [new ConsoleSink(), new FileSink(logPath)];
        SinkDemo.Run(new OrderService(store, sinks: sinks));
    }
    else
    {
        StoreDemo.Run(new OrderService(store), store);
    }
}
catch (System.Text.Json.JsonException ex)
{
    Console.WriteLine($"Файл даних пошкоджено або має старий формат: {ex.Message}");
    Console.WriteLine($"Видаліть {dataPath} і запустіть знову.");
    return 1;
}

return 0;

// lab07: підкоманди звітів. Тут лише виклики ReportService і вивід — жодного відбору даних.
static int RunReport(string command, OrderService service, bool empty)
{
    // --empty: перевірка, що звіти не падають на порожньому джерелі.
    IReadOnlyList<Order> items = empty ? [] : service.All();
    Console.WriteLine($"Джерело: {items.Count} замовлень");
    Console.WriteLine();

    switch (command)
    {
        case "report-top":
            ReportPrinter.Print(ReportService.TopCustomers(items, 5));
            return 0;

        case "report-expensive":
            ReportPrinter.Print(ReportService.ExpensiveOrders(items, 10_000m));
            return 0;

        case "report-groups":
            ReportPrinter.Print("За статусом", ReportService.ByStatus(items));
            Console.WriteLine();
            ReportPrinter.Print("За місяцем", ReportService.ByMonth(items));
            return 0;

        case "report-sum":
            ReportPrinter.Print(ReportService.Totals(items));
            return 0;

        case "report-products":
            ReportPrinter.Print(ReportService.TopProducts(items, 5));
            return 0;

        case "report-customers":                                   // [дод 1]
            ReportPrinter.Print(ReportService.CustomersWithOrders(SampleData.Customers(), items));
            return 0;

        case "report-groups-query":                                // [дод 3]
        {
            IReadOnlyList<GroupSummary> viaQuery = ReportService.ByStatusQuery(items);
            ReportPrinter.Print("За статусом (синтаксис запитів)", viaQuery);
            Console.WriteLine();
            Console.WriteLine($"Збігається з методним синтаксисом: {viaQuery.SequenceEqual(ReportService.ByStatus(items))}");
            return 0;
        }

        case "demo-deferred":
            DeferredDemo.Run(items);
            return 0;

        case "bench":                                              // [дод 4]
            BenchDemo.Run(100_000);
            return 0;

        default:
            Console.WriteLine("Невідома команда. Доступні:");
            Console.WriteLine("  report-top | report-expensive | report-groups | report-sum | report-products");
            Console.WriteLine("  report-customers | report-groups-query | demo-deferred | bench");
            return 2;
    }
}