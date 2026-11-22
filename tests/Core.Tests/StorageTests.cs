using Core.Configuration;
using Core.Storage;

namespace Core.Tests;

// lab09: шляхи і файлове сховище. Кожен тест працює у власному тимчасовому каталозі.
public class StorageTests
{
    private static string NewTempDir() =>
        Path.Combine(Path.GetTempPath(), $"crossapp-lab09-{Guid.NewGuid():N}");

    [Fact] // відносний DataPath рахується від переданого базового каталогу, а не від cwd
    public void ResolveFullPath_RelativeDataPath_IsUnderBaseDir()
    {
        string baseDir = NewTempDir();
        try
        {
            var storage = new StorageOptions { DataPath = "data", FileName = "orders.json" };

            string path = storage.ResolveFullPath(baseDir);

            Assert.Equal(Path.Combine(baseDir, "data", "orders.json"), path);
            Assert.True(Directory.Exists(Path.Combine(baseDir, "data"))); // каталог створено
        }
        finally
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }

    [Fact] // абсолютний DataPath (наприклад, з CROSSAPP_DATA_PATH) використовується як є
    public void ResolveFullPath_RootedDataPath_IgnoresBaseDir()
    {
        string dataDir = NewTempDir();
        try
        {
            var storage = new StorageOptions { DataPath = dataDir, FileName = "x.json" };

            string path = storage.ResolveFullPath(Path.GetTempPath());

            Assert.Equal(Path.Combine(dataDir, "x.json"), path);
        }
        finally
        {
            Directory.Delete(dataDir, recursive: true);
        }
    }

    [Theory] // рівні логування: Debug найдетальніший
    [InlineData("Debug", "Debug", true)]
    [InlineData("Warning", "Debug", false)]
    [InlineData("Warning", "Error", true)]
    [InlineData("щось", "Information", true)] // невідомий рівень = Information
    public void Logging_IsEnabled_RespectsMinimumLevel(string minimum, string level, bool expected) =>
        Assert.Equal(expected, new LoggingOptions { MinimumLevel = minimum }.IsEnabled(level));

    [Fact] // запис і повторне читання новим екземпляром + формат файлу
    public void FileOrderStore_RoundTrip_CamelCaseAndReadableCyrillic()
    {
        string dir = NewTempDir();
        string path = Path.Combine(dir, "orders.json");
        try
        {
            var first = new FileOrderStore(path);
            first.Add(TestData.Orders()[0]);                  // ORD-A, ноутбук

            var second = new FileOrderStore(path);            // новий екземпляр читає з диска
            string json = File.ReadAllText(path);

            Assert.Equal(30_000m, second.GetById("ORD-A")!.Total);
            Assert.Contains("\"customerId\"", json);          // camelCase
            Assert.Contains("\"productName\"", json);         // варіант «Замовлення»
            Assert.Contains("Ноутбук", json);                 // кирилиця не як \u041D
            Assert.False(File.Exists(path + ".tmp"));         // тимчасовий файл не лишився
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact] // зіпсований файл — StorageException з шляхом, а не JsonException
    public void FileOrderStore_CorruptFile_ThrowsStorageException()
    {
        string dir = NewTempDir();
        string path = Path.Combine(dir, "orders.json");
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, "[{\"id\": oops");
            var store = new FileOrderStore(path);

            var ex = Assert.Throws<StorageException>(() => store.List());

            Assert.Contains(path, ex.Message);
            Assert.NotNull(ex.InnerException);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact] // файлу ще немає — порожнє сховище; видалений каталог створюється при записі
    public void FileOrderStore_MissingFileAndDirectory_WorksAndRecreates()
    {
        string dir = NewTempDir();
        string path = Path.Combine(dir, "nested", "orders.json");
        try
        {
            var store = new FileOrderStore(path);

            Assert.Empty(store.List());
            store.Add(TestData.Orders()[1]);
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact] // експорт → імпорт: те саме число записів і ті самі суми
    public async Task Export_ThenImport_RestoresSameOrders()
    {
        string dir = NewTempDir();
        string path = Path.Combine(dir, "export.json");
        try
        {
            IReadOnlyList<Domain.Order> source = TestData.Orders();

            int exported = JsonOrderExporter.Export(source, path);
            IReadOnlyList<Domain.Order> loaded = await new JsonOrderImporter().LoadAsync(path);

            Assert.Equal(source.Count, exported);
            Assert.Equal(source.Sum(o => o.Total), loaded.Sum(o => o.Total));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}