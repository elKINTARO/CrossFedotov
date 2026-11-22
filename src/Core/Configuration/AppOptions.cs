namespace Core.Configuration;

// Налаштування застосунку. Звичайні класи без жодної залежності від пакетів конфігурації:
// Core не знає, звідки прийшли значення (JSON, змінні середовища, командний рядок).
// Cli заповнює їх через config.Get<AppOptions>().
public sealed class AppOptions
{
    public string EnvironmentName { get; init; } = "Production";
    public StorageOptions Storage { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public OrdersOptions Orders { get; init; } = new();
}

public sealed class StorageOptions
{
    // Звідки відраховувати відносний DataPath:
    //   AppBase      — каталог застосунку (AppContext.BaseDirectory), зручно для розробки;
    //   LocalAppData — системний каталог даних користувача, для «встановленого» застосунку.
    public string Root { get; init; } = "AppBase";
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "orders.json";

    // Повний шлях до файлу даних. Ніяких "\" чи "/" вручну: лише Path.Combine.
    // Абсолютний DataPath (наприклад, зі змінної середовища) використовується як є.
    public string ResolveFullPath(string baseDir)
    {
        string root = Path.IsPathRooted(DataPath)
            ? DataPath
            : Path.Combine(ResolveRoot(baseDir), DataPath);

        Directory.CreateDirectory(root); // видалений каталог створюється заново
        return Path.GetFullPath(Path.Combine(root, FileName));
    }

    private string ResolveRoot(string baseDir) =>
        string.Equals(Root, "LocalAppData", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrossApp")
            : baseDir;
}

public sealed class LoggingOptions
{
    private static readonly string[] Levels = ["Debug", "Information", "Warning", "Error"];

    public string MinimumLevel { get; init; } = "Information";

    // Чи друкувати повідомлення рівня level за поточного MinimumLevel.
    public bool IsEnabled(string level) => Rank(level) >= Rank(MinimumLevel);

    private static int Rank(string level)
    {
        int index = Array.FindIndex(Levels, l => string.Equals(l, level, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? 1 : index; // невідомий рівень = Information
    }
}

// Варіант «Замовлення»: ключ Orders:Currency.
public sealed class OrdersOptions
{
    public string Currency { get; init; } = "UAH";
}