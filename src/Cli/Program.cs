using System.Text;
using Cli;
using Core.Dto;
using Core.Import;

Console.OutputEncoding = Encoding.UTF8;

// Режим lab01-02: інформація про середовище
if (args is ["--env", ..])
{
    EnvironmentView.Print(asJson: args.Contains("--json"));
    return 0;
}

// [дод 2] Товари і клієнти в одному файлі
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

// Основний режим: імпорт товарів
string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

// [дод 1] Вибір імпортера за розширенням
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
foreach (ProductDto p in result.Items.Take(5))
    Console.WriteLine($"  {p.Id,-6} {p.Name,-26} {p.Price,10:F2} грн");

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
        Console.WriteLine($"  ! {e}");
}

// [дод 3] Статистика
Console.WriteLine($"Усього {result.Total} | прийнято {result.Items.Count} | пропущено {result.Errors.Count} | помилок {result.ErrorRate:P0}");
return 0;