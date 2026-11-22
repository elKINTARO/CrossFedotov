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
using Core.Storage;

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

// ===== COMPOSITION ROOT (lab05-09) =====
// Єдине місце, де створюються конкретні класи і зшиваються залежності.
// Сховище: --file, --cache. Сценарій: --events, --sink (lab06), report-* (lab07),
// import / import-stream / gen-import (lab08), config / export (lab09), без аргументів — lab05.

// lab09: шлях до даних і секрет беруться з конфігурації, а не з коду.
LoadedConfig cfg = AppConfiguration.Load(args);
string dataPath = cfg.DataFile;
string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");

IOrderStore store = StoreFactory.Create(args, dataPath);

// lab07: перший аргумент без "--" — назва звіту. Прапорці (--file тощо) назвою не вважаються.
string? command = args.FirstOrDefault(a => !a.StartsWith("--"));

try
{
    // lab09: конфігурація і експорт колекції в JSON.
    if (command is "config")
    {
        ConfigView.PrintDetails(cfg);
        return 0;
    }

    if (command is "export")
        return RunExport(args, new OrderService(store), cfg);

    // lab08: асинхронний імпорт. await на верхньому рівні: Main стає async Task<int> автоматично.
    if (command is "import" or "import-stream" or "gen-import")
        return await RunImportAsync(command, args, store);

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
catch (StorageException ex)
{
    // lab09: зіпсований файл даних — зрозуміле повідомлення замість stack trace.
    Console.WriteLine(ex.Message);
    Console.WriteLine($"Виправте або видаліть {dataPath} і запустіть знову.");
    return 1;
}

return 0;

// lab09: експорт усієї колекції в JSON. Без шляху — у файл даних з конфігурації.
static int RunExport(string[] args, OrderService service, LoadedConfig cfg)
{
    string path = args is [_, var target, ..] && !target.StartsWith("--")
        ? Path.GetFullPath(target)
        : cfg.DataFile;

    IReadOnlyList<Order> orders = service.All();
    int count = JsonOrderExporter.Export(orders, path);

    ConfigView.PrintSummary(cfg);
    Console.WriteLine($"Експортовано записів: {count}  →  {path}");
    Console.WriteLine($"Виручка підтверджених: {ReportService.Totals(orders).Revenue:N2} {cfg.Options.Orders.Currency}");
    Console.WriteLine("Початок файлу:");
    foreach (string line in File.ReadLines(path).Take(14))
        Console.WriteLine($"  {line}");

    return 0;
}

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
            Console.WriteLine("  import <файл.json> [--timeout мс] | import-stream <файл.json> | gen-import <файл.json> <кількість>");
            Console.WriteLine("  config | export [файл.json]");
            return 2;
    }
}

// lab08: імпорт замовлень з Ctrl+C, тайм-аутом і роздільною обробкою помилок.
// Команда йде першою: import <файл> [--timeout мс] [--file], import-stream <файл>, gen-import <файл> <кількість>.
static async Task<int> RunImportAsync(string command, string[] args, IOrderStore store)
{
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;   // не вбивати процес одразу: імпорт сам зупиниться на найближчій перевірці токена
        cts.Cancel();
    };

    if (args is ["gen-import", var genPath, var countText, ..])
    {
        int count = int.TryParse(countText, out int parsed) ? parsed : 200_000;
        await ImportFileGenerator.WriteAsync(genPath, count, cts.Token);
        Console.WriteLine($"Згенеровано {count:N0} замовлень у {Path.GetFullPath(genPath)}");
        return 0;
    }

    if (args is not [_, var path, ..] || path.StartsWith("--"))
    {
        Console.WriteLine("Використання:");
        Console.WriteLine("  import <файл.json> [--timeout мс]");
        Console.WriteLine("  import-stream <файл.json>");
        Console.WriteLine("  gen-import <файл.json> <кількість>");
        return 2;
    }

    var service = new OrderService(store);
    int events = 0;
    service.Changed += (_, _) => events++;           // подія тижня 6 рахує додані записи

    var importer = new JsonOrderImporter();
    var progress = new ConsoleProgress("обробка");    // [дод 3]
    int? timeoutMs = ReadInt(args, "--timeout");      // [дод 2]
    int before = service.All().Count;

    Console.WriteLine($"Імпорт {Path.GetFullPath(path)}  (Ctrl+C — скасувати)");

    try
    {
        int added = command switch
        {
            "import-stream" => await service.ImportStreamAsync(importer, path, progress, cts.Token),
            _ when timeoutMs is int ms => await service.ImportWithTimeoutAsync(
                importer, path, TimeSpan.FromMilliseconds(ms), progress, cts.Token),
            _ => await service.ImportAsync(importer, path, progress, cts.Token)
        };

        progress.Finish();
        Console.WriteLine($"Імпортовано записів: {added} (подій OrderImported: {events})");
        return 0;
    }
    // Порядок важливий: скасування і тайм-аут — очікувані ситуації, вони йдуть першими.
    catch (TimeoutException ex)
    {
        progress.Finish();
        Console.WriteLine($"Імпорт перервано: {ex.Message}");
        return 3;
    }
    catch (OperationCanceledException)
    {
        progress.Finish();
        Console.WriteLine("Імпорт скасовано.");
        return 3;
    }
    catch (DomainException ex)
    {
        progress.Finish();
        Console.WriteLine($"Дані відхилено: {ex.Message}");
        return 4;
    }
    catch (FileNotFoundException)
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
        return 1;
    }
    catch (System.Text.Json.JsonException ex)
    {
        progress.Finish();
        Console.WriteLine($"Файл не є коректним JSON: {ex.Message}");
        return 4;
    }
    finally
    {
        Console.WriteLine($"Замовлень у сховищі: було {before}, стало {service.All().Count}");
    }
}

static int? ReadInt(string[] args, string option)
{
    int i = Array.IndexOf(args, option);
    return i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out int value) ? value : null;
}