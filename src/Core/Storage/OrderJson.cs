using System.Text.Encodings.Web;
using System.Text.Json;
using Core.Dto;

namespace Core.Storage;

// Одні й ті самі налаштування JSON для сховища, експорту й імпорту:
// що записали — те й прочитаємо.
public static class OrderJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,   // у файлі "customerId", у C# CustomerId
        PropertyNameCaseInsensitive = true,                  // старі файли з "CustomerId" теж читаються
        // Кирилиця в файлі як є, а не \u0426. Безпечно, бо цей JSON не вставляється в HTML.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Атомарний запис: спершу повністю пишемо тимчасовий файл, потім одним кроком підміняємо.
    // Якщо процес впаде посеред запису, зіпсується лише .tmp, а старий файл лишиться цілим.
    public static void WriteAtomic(string path, IReadOnlyList<OrderDto> dtos)
    {
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        string tmp = fullPath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(dtos, Options));
        File.Move(tmp, fullPath, overwrite: true);
    }
}