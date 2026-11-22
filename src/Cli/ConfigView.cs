using Core.Configuration;

namespace Cli;

// Вивід конфігурації. Секрет не друкується ніколи — лише факт наявності й довжина.
internal static class ConfigView
{
    public static void PrintSummary(LoadedConfig cfg)
    {
        Console.WriteLine($"Профіль    : {cfg.Options.EnvironmentName}");
        Console.WriteLine($"Файл даних : {cfg.DataFile}");
        Console.WriteLine(cfg.ApiKey is null ? "ApiKey     : не заданий" : $"ApiKey     : присутній, довжина {cfg.ApiKey.Length}");
    }

    public static void PrintDetails(LoadedConfig cfg)
    {
        PrintSummary(cfg);
        Console.WriteLine($"Валюта     : {cfg.Options.Orders.Currency}");
        Console.WriteLine($"Лог        : мінімальний рівень {cfg.Options.Logging.MinimumLevel}");

        // Подробиці — лише якщо дозволяє рівень логування (Debug у Development).
        LoggingOptions log = cfg.Options.Logging;
        if (log.IsEnabled("Debug"))
        {
            Console.WriteLine();
            Console.WriteLine("[debug] Джерела: appsettings.json → appsettings.{Environment}.json → user-secrets → CROSSAPP_* → --Ключ=значення");
            Console.WriteLine($"[debug] Шлях даних узято з: {cfg.DataPathSource}");
            Console.WriteLine($"[debug] Storage: Root={cfg.Storage.Root}, DataPath={cfg.Storage.DataPath}, FileName={cfg.Storage.FileName}");
            Console.WriteLine($"[debug] AppContext.BaseDirectory   : {AppContext.BaseDirectory}");
            Console.WriteLine($"[debug] Environment.CurrentDirectory: {Environment.CurrentDirectory}");
        }
    }
}