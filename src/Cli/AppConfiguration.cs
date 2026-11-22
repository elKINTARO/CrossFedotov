using System.Reflection;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cli;

// Готова конфігурація: усе, що потрібно composition root.
internal sealed record LoadedConfig(
    AppOptions Options,
    StorageOptions Storage,
    string DataFile,
    string? ApiKey,
    string DataPathSource);

// lab09: збирання конфігурації. Порядок джерел = порядок перекриття:
// пізніше додане джерело перекриває раніше додане за тим самим ключем.
internal static class AppConfiguration
{
    public const string EnvPrefix = "CROSSAPP_";

    public static LoadedConfig Load(string[] args)
    {
        string envName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT") ?? "Development";

        // У командний рядок конфігурації пускаємо лише пари --Ключ=значення.
        // Інакше "--file report-top" прочиталося б як ключ "file" зі значенням "report-top".
        string[] overrides = args.Where(a => a.StartsWith("--") && a.Contains('=')).ToArray();

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)                     // не від поточного каталогу!
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: false);

        // user-secrets — інструмент розробника, тому лише в Development (як робить і ASP.NET Core).
        if (string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase))
            builder.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

        IConfiguration config = builder
            .AddEnvironmentVariables(prefix: EnvPrefix)                // CROSSAPP_Storage__DataPath
            .AddCommandLine(overrides)                                 // --Storage:DataPath=tmp
            .Build();

        AppOptions options = config.Get<AppOptions>() ?? new AppOptions();

        // Проста змінна CROSSAPP_DATA_PATH перекриває розділ Storage повністю.
        string? legacy = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
        StorageOptions storage = string.IsNullOrWhiteSpace(legacy)
            ? options.Storage
            : new StorageOptions { Root = options.Storage.Root, DataPath = legacy, FileName = options.Storage.FileName };

        string source = string.IsNullOrWhiteSpace(legacy) ? "конфігурація (Storage:DataPath)" : "змінна CROSSAPP_DATA_PATH";

        return new LoadedConfig(
            options,
            storage,
            storage.ResolveFullPath(AppContext.BaseDirectory),
            config["Integration:ApiKey"],                              // секрет: читаємо, але не друкуємо
            source);
    }
}