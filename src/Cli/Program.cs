using System.Text;
using Cli;
using Core.Abstractions;
using Core.Dto;
using Core.Import;
using Core.Services;

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

    ImportResult<ProductDto>? result = Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".csv" => ProductCsvImporter.Load(path),
        ".json" => ProductJsonImporter.Load(path),
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
    if (result.Errors.Count > 0)
    {
        Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
        foreach (string e in result.Errors)
            Console.WriteLine($"  ! {e}");
    }

    Console.WriteLine($"Усього {result.Total} | прийнято {result.Items.Count} | пропущено {result.Errors.Count} | помилок {result.ErrorRate:P0}");
    return 0;
}

// lab05: COMPOSITION ROOT
// Єдине місце, де створюються конкретні класи і зшиваються залежності.
// Прапорці: --file (сховище у файлі), --cache (обгорнути декоратором).
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");

IOrderStore store = StoreFactory.Create(args, dataPath);
var service = new OrderService(store);

try
{
    StoreDemo.Run(service, store);
}
catch (System.Text.Json.JsonException ex)
{
    Console.WriteLine($"Файл даних пошкоджено або має старий формат: {ex.Message}");
    Console.WriteLine($"Видаліть {dataPath} і запустіть знову.");
    return 1;
}

return 0;