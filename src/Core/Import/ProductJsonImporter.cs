using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static ImportResult<ProductDto> Load(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            List<ProductDto> items = JsonSerializer.Deserialize<List<ProductDto>>(json, Options) ?? [];
            return new ImportResult<ProductDto>(items, []);
        }
        catch (JsonException ex)
        {
            return new ImportResult<ProductDto>([], [$"рядок {ex.LineNumber + 1}: некоректний JSON"]);
        }
    }
}