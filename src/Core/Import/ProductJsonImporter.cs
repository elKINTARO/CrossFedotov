using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static ImportResult<ProductDto> Load(string path, Action<string>? onSkipped = null)
    {
        try
        {
            string json = File.ReadAllText(path);
            List<ProductDto> items = JsonSerializer.Deserialize<List<ProductDto>>(json, Options) ?? [];
            return new ImportResult<ProductDto>(items, []);
        }
        catch (JsonException ex)
        {
            string message = $"рядок {ex.LineNumber + 1}: некоректний JSON";
            onSkipped?.Invoke(message);
            return new ImportResult<ProductDto>([], [message]);
        }
    }
}